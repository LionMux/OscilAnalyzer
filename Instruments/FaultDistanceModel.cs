using Microsoft.ML.OnnxRuntime;
using System.IO;
using System.Numerics;

namespace OscilAnalyzer
{
    /// <summary>
    /// Класс для расчёта расстояния до места повреждения через ONNX-модель CNN1D.
    /// На вход подаётся сырая осциллограмма (6 каналов: Ia, Ib, Ic, Ua, Ub, Uc),
    /// на выходе — расстояние в км.
    /// Препроцессинг: DC removal -> детекция t0 -> окно -> симметричные составляющие
    /// -> нормализация -> ONNX инференс -> денормализация.
    /// </summary>
    public class FaultDistanceModel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly ScalersData _scalers;
        private readonly int _numChannels;
        private readonly int _seqLength;
        private readonly double _mainsFreq;
        private readonly double _preMs;
        private readonly double _postMs;

        private const double DefaultMainsFreq = 50.0;
        private const double DefaultPreMs = 50.0;
        private const double DefaultPostMs = 70.0;

        public FaultDistanceModel(string modelDir)
        {
            var dir = modelDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var onnxPath = Path.Combine(dir, "best_model.onnx");
            var scalersPath = Path.Combine(dir, "scalers.json");

            if (!File.Exists(onnxPath))
                throw new FileNotFoundException($"ONNX модель не найдена: {onnxPath}");
            if (!File.Exists(scalersPath))
                throw new FileNotFoundException($"Файлы нормализации не найдены: {scalersPath}");

            _session = new InferenceSession(onnxPath);
            _scalers = ScalersData.Load(scalersPath);

            _numChannels = _scalers.NumChannels;
            _seqLength = _scalers.SeqLength;
            _mainsFreq = DefaultMainsFreq;
            _preMs = DefaultPreMs;
            _postMs = DefaultPostMs;
        }

        /// <summary>
        /// Рассчитывает расстояние до места повреждения по осциллограмме.
        /// </summary>
        /// <param name="ia, ib, ic">Токи фаз A, B, C</param>
        /// <param name="ua, ub, uc">Напряжения фаз A, B, C</param>
        /// <param name="fsHz">Частота дискретизации, Гц</param>
        /// <returns>Расстояние в км</returns>
        public double Predict(
            double[] ia, double[] ib, double[] ic,
            double[] ua, double[] ub, double[] uc,
            double fsHz)
        {
            var channels = new[] { ia, ib, ic, ua, ub, uc };
            ValidateSignals(channels);

            var channelsArr = channels.Select(ch => ch.ToArray()).ToArray();

            // Шаг 1: DC removal
            channelsArr = RemoveDC(channelsArr, fsHz);

            // Шаг 2: Детекция t0
            int t0 = DetectFaultInception(channelsArr, fsHz);

            // Шаг 3: Окно вокруг t0
            channelsArr = CropWindow(channelsArr, t0, fsHz);

            // Шаг 4: Симметричные составляющие (скользящий ДПФ)
            double[][] symseqChannels = ComputeSymmetricalComponents(channelsArr, fsHz);

            // Шаг 5: Сборка 12 каналов и нормализация
            double[][] allChannels = new double[12][];
            for (int i = 0; i < 6; i++)
                allChannels[i] = channelsArr[i];
            for (int i = 0; i < 6; i++)
                allChannels[6 + i] = symseqChannels[i];

            double[][] normalized = Normalize(allChannels);

            // Шаг 6: Инференс ONNX
            double distNorm = RunInference(normalized);

            // Шаг 7: Денормализация
            return distNorm * (_scalers.DistMax - _scalers.DistMin) + _scalers.DistMin;
        }

        private void ValidateSignals(Array[] channels)
        {
            int len = channels[0].Length;
            for (int i = 1; i < channels.Length; i++)
            {
                if (channels[i].Length != len)
                    throw new ArgumentException("Все сигналы должны иметь одинаковую длину");
            }
        }

        /// <summary>
        /// Шаг 1: Удаление постоянной составляющей.
        /// Центрирование по предаварийному участку (первые 20ms).
        /// Вычитание скользящего среднего за период.
        /// </summary>
        private double[][] RemoveDC(double[][] channels, double fsHz)
        {
            int windowLen = Math.Max(1, (int)(fsHz / DefaultMainsFreq));
            int preWindowLen = Math.Max(1, (int)(0.020 * fsHz));

            var result = new double[channels.Length][];

            for (int ch = 0; ch < channels.Length; ch++)
            {
                var signal = channels[ch];
                int n = signal.Length;
                var outSignal = new double[n];

                double preMean = 0.0;
                if (preWindowLen < n)
                {
                    for (int i = 0; i < preWindowLen; i++)
                        preMean += signal[i];
                    preMean /= preWindowLen;
                }

                for (int i = 0; i < n; i++)
                    outSignal[i] = signal[i] - preMean;

                if (windowLen > 1 && n > windowLen)
                {
                    var rollingMean = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        int start = Math.Max(0, i - windowLen / 2);
                        int end = Math.Min(n, i + windowLen / 2 + 1);
                        double sum = 0.0;
                        for (int j = start; j < end; j++)
                            sum += outSignal[j];
                        rollingMean[i] = sum / (end - start);
                    }
                    for (int i = 0; i < n; i++)
                        outSignal[i] -= rollingMean[i];
                }

                result[ch] = outSignal;
            }

            return result;
        }

        /// <summary>
        /// Шаг 2: Детекция начала КЗ (t0) по критерию:
        /// Ток: RMS вырос более чем на T0_ETA_I (50%)
        /// Напряжение: RMS упал ниже T0_ETA_U (85%)
        /// </summary>
        private int DetectFaultInception(double[][] channels, double fsHz)
        {
            int halfPeriod = Math.Max(1, (int)(fsHz / DefaultMainsFreq / 2));
            int n = channels[0].Length;

            double[] rmsCurr = ComputeRmsSliding(channels[0], halfPeriod);
            double[] rmsCurrB = ComputeRmsSliding(channels[1], halfPeriod);
            double[] rmsCurrC = ComputeRmsSliding(channels[2], halfPeriod);
            double[] rmsVoltA = ComputeRmsSliding(channels[3], halfPeriod);
            double[] rmsVoltB = ComputeRmsSliding(channels[4], halfPeriod);
            double[] rmsVoltC = ComputeRmsSliding(channels[5], halfPeriod);

            double preWindow = (int)(0.100 * fsHz);
            double preCurrA = 0.0, preCurrB = 0.0, preCurrC = 0.0;
            double preVoltA = 0.0, preVoltB = 0.0, preVoltC = 0.0;
            int preCount = 0;

            for (int i = 0; i < Math.Min(preWindow, n - halfPeriod); i++)
            {
                preCurrA += rmsCurr[i];
                preCurrB += rmsCurrB[i];
                preCurrC += rmsCurrC[i];
                preVoltA += rmsVoltA[i];
                preVoltB += rmsVoltB[i];
                preVoltC += rmsVoltC[i];
                preCount++;
            }

            if (preCount > 0)
            {
                preCurrA /= preCount; preCurrB /= preCount; preCurrC /= preCount;
                preVoltA /= preCount; preVoltB /= preCount; preVoltC /= preCount;
            }

            double etaI = 0.5;
            double etaU = 0.85;

            for (int i = halfPeriod; i < n - halfPeriod; i++)
            {
                double currNow = Math.Max(Math.Max(rmsCurr[i], rmsCurrB[i]), rmsCurrC[i]);
                double currPre = Math.Max(Math.Max(
                    i - halfPeriod >= 0 ? rmsCurr[i - halfPeriod] : 0,
                    i - halfPeriod >= 0 ? rmsCurrB[i - halfPeriod] : 0),
                    i - halfPeriod >= 0 ? rmsCurrC[i - halfPeriod] : 0);

                double voltNow = Math.Min(Math.Min(rmsVoltA[i], rmsVoltB[i]), rmsVoltC[i]);
                double voltPre = Math.Min(Math.Min(
                    i - halfPeriod >= 0 ? rmsVoltA[i - halfPeriod] : double.MaxValue,
                    i - halfPeriod >= 0 ? rmsVoltB[i - halfPeriod] : double.MaxValue),
                    i - halfPeriod >= 0 ? rmsVoltC[i - halfPeriod] : double.MaxValue);

                if (preCurrA > 0 && preVoltA > 0)
                {
                    bool currRising = currNow > preCurrA * (1 + etaI);
                    bool voltDropping = voltNow < preVoltA * etaU;
                    if (currRising && voltDropping)
                        return i;
                }
            }

            return n / 2;
        }

        private double[] ComputeRmsSliding(double[] signal, int windowLen)
        {
            int n = signal.Length;
            var rms = new double[n];

            for (int i = 0; i < n; i++)
            {
                int start = Math.Max(0, i - windowLen / 2);
                int end = Math.Min(n, i + windowLen / 2 + 1);
                double sumSq = 0.0;
                for (int j = start; j < end; j++)
                    sumSq += signal[j] * signal[j];
                rms[i] = Math.Sqrt(sumSq / (end - start));
            }

            return rms;
        }

        /// <summary>
        /// Шаг 3: Обрезка сигнала к окну pre_ms + post_ms вокруг t0.
        /// Дополнение нулями если короче SEQ_LENGTH, обрезка если длиннее.
        /// </summary>
        private double[][] CropWindow(double[][] channels, int t0, double fsHz)
        {
            int preSamp = (int)(_preMs * fsHz / 1000.0);
            int postSamp = (int)(_postMs * fsHz / 1000.0);
            int windowSamp = preSamp + postSamp;

            var result = new double[channels.Length][];

            for (int ch = 0; ch < channels.Length; ch++)
            {
                int n = channels[ch].Length;
                int startIdx = Math.Max(0, t0 - preSamp);
                int endIdx = Math.Min(n, t0 + postSamp);

                if (endIdx - startIdx >= windowSamp)
                {
                    startIdx = Math.Max(0, endIdx - windowSamp);
                }

                int srcLen = endIdx - startIdx;
                var window = new double[_seqLength];

                for (int i = 0; i < _seqLength; i++)
                {
                    int srcIdx = startIdx + i;
                    if (srcIdx >= 0 && srcIdx < n)
                        window[i] = channels[ch][srcIdx];
                    else
                        window[i] = 0.0;
                }

                result[ch] = window;
            }

            return result;
        }

        /// <summary>
        /// Шаг 4: Вычисление симметричных составляющих скользящим ДПФ (Hann-окно, 1 период).
        /// Возвращает |I1|, |I2|, |I0|, |U1|, |U2|, |U0| для каждого отсчёта.
        /// </summary>
        private double[][] ComputeSymmetricalComponents(double[][] channels, double fsHz)
        {
            int periodLen = Math.Max(1, (int)(fsHz / _mainsFreq));
            int n = _seqLength;

            double[] hannWindow = new double[periodLen];
            for (int i = 0; i < periodLen; i++)
            {
                hannWindow[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / periodLen));
            }

            double twoPiN = 2.0 * Math.PI / periodLen;
            double cosCache = Math.Cos(twoPiN);
            double sinCache = Math.Sin(twoPiN);
            double cos2Cache = Math.Cos(2.0 * twoPiN);
            double sin2Cache = Math.Sin(2.0 * twoPiN);
            double cos3Cache = Math.Cos(3.0 * twoPiN);
            double sin3Cache = Math.Sin(3.0 * twoPiN);

            double invN = 1.0 / periodLen;

            double[] magI1 = new double[n];
            double[] magI2 = new double[n];
            double[] magI0 = new double[n];
            double[] magU1 = new double[n];
            double[] magU2 = new double[n];
            double[] magU0 = new double[n];

            for (int ch = 0; ch < 3; ch++)
            {
                int chIdx = ch;
                double[] signal = channels[chIdx];

                for (int t = 0; t < n; t++)
                {
                    int windowStart = t;
                    int windowEnd = Math.Min(n, t + periodLen);
                    int actualLen = windowEnd - windowStart;

                    double re1 = 0.0, im1 = 0.0;
                    double re2 = 0.0, im2 = 0.0;
                    double re0 = 0.0, im0 = 0.0;

                    for (int i = 0; i < actualLen; i++)
                    {
                        double s = signal[windowStart + i];
                        double w = (i < hannWindow.Length) ? hannWindow[i] : 0.0;
                        double sw = s * w;

                        double a = i * twoPiN;
                        re0 += sw;
                        re1 += sw * Math.Cos(a);
                        im1 += sw * Math.Sin(a);
                        re2 += sw * Math.Cos(2 * a);
                        im2 += sw * Math.Sin(2 * a);
                    }

                    re0 *= invN;
                    re1 *= 2.0 * invN;
                    im1 *= 2.0 * invN;
                    re2 *= 2.0 * invN;
                    im2 *= 2.0 * invN;

                    double i1Mag = Math.Sqrt(re1 * re1 + im1 * im1);
                    double i2Mag = Math.Sqrt(re2 * re2 + im2 * im2);
                    double i0Mag = Math.Abs(re0) * invN;

                    if (ch == 0) { magI1[t] = i1Mag; magI2[t] = i2Mag; magI0[t] = i0Mag; }
                    else if (ch == 1)
                    {
                        double angle = 2.0 * Math.PI / 3.0;
                        double ca = Math.Cos(angle), sa = Math.Sin(angle);
                        double re1b = re1, im1b = im1, re2b = re2, im2b = im2;
                        re1 = ca * re1b - sa * im1b; im1 = sa * re1b + ca * im1b;
                        re2 = ca * re2b + sa * im2b; im2 = -sa * re2b + ca * im2b;
                        magI1[t] = Math.Sqrt(re1 * re1 + im1 * im1);
                        magI2[t] = Math.Sqrt(re2 * re2 + im2 * im2);
                        magI0[t] = i0Mag;
                    }
                    else
                    {
                        double angle = 4.0 * Math.PI / 3.0;
                        double ca = Math.Cos(angle), sa = Math.Sin(angle);
                        double re1b = re1, im1b = im1, re2b = re2, im2b = im2;
                        re1 = ca * re1b - sa * im1b; im1 = sa * re1b + ca * im1b;
                        re2 = ca * re2b + sa * im2b; im2 = -sa * re2b + ca * im2b;
                        magI1[t] = Math.Sqrt(re1 * re1 + im1 * im1);
                        magI2[t] = Math.Sqrt(re2 * re2 + im2 * im2);
                        magI0[t] = i0Mag;
                    }
                }
            }

            for (int ch = 0; ch < 3; ch++)
            {
                int chIdx = 3 + ch;
                double[] signal = channels[chIdx];

                for (int t = 0; t < n; t++)
                {
                    int windowStart = t;
                    int windowEnd = Math.Min(n, t + periodLen);
                    int actualLen = windowEnd - windowStart;

                    double re1 = 0.0, im1 = 0.0;
                    double re2 = 0.0, im2 = 0.0;
                    double re0 = 0.0;

                    for (int i = 0; i < actualLen; i++)
                    {
                        double s = signal[windowStart + i];
                        double w = (i < hannWindow.Length) ? hannWindow[i] : 0.0;
                        double sw = s * w;

                        double a = i * twoPiN;
                        re0 += sw;
                        re1 += sw * Math.Cos(a);
                        im1 += sw * Math.Sin(a);
                        re2 += sw * Math.Cos(2 * a);
                        im2 += sw * Math.Sin(2 * a);
                    }

                    re0 *= invN;
                    re1 *= 2.0 * invN;
                    im1 *= 2.0 * invN;
                    re2 *= 2.0 * invN;
                    im2 *= 2.0 * invN;

                    double u1Mag = Math.Sqrt(re1 * re1 + im1 * im1);
                    double u2Mag = Math.Sqrt(re2 * re2 + im2 * im2);
                    double u0Mag = Math.Abs(re0) * invN;

                    if (ch == 0) { magU1[t] = u1Mag; magU2[t] = u2Mag; magU0[t] = u0Mag; }
                    else if (ch == 1)
                    {
                        double angle = 2.0 * Math.PI / 3.0;
                        double ca = Math.Cos(angle), sa = Math.Sin(angle);
                        double re1b = re1, im1b = im1, re2b = re2, im2b = im2;
                        re1 = ca * re1b - sa * im1b; im1 = sa * re1b + ca * im1b;
                        re2 = ca * re2b + sa * im2b; im2 = -sa * re2b + ca * im2b;
                        magU1[t] = Math.Sqrt(re1 * re1 + im1 * im1);
                        magU2[t] = Math.Sqrt(re2 * re2 + im2 * im2);
                        magU0[t] = u0Mag;
                    }
                    else
                    {
                        double angle = 4.0 * Math.PI / 3.0;
                        double ca = Math.Cos(angle), sa = Math.Sin(angle);
                        double re1b = re1, im1b = im1, re2b = re2, im2b = im2;
                        re1 = ca * re1b - sa * im1b; im1 = sa * re1b + ca * im1b;
                        re2 = ca * re2b + sa * im2b; im2 = -sa * re2b + ca * im2b;
                        magU1[t] = Math.Sqrt(re1 * re1 + im1 * im1);
                        magU2[t] = Math.Sqrt(re2 * re2 + im2 * im2);
                        magU0[t] = u0Mag;
                    }
                }
            }

            return new double[][] { magI1, magI2, magI0, magU1, magU2, magU0 };
        }

        /// <summary>
        /// Шаг 5: Нормализация по каналам через StandardScaler: (x - mean) / std.
        /// </summary>
        private double[][] Normalize(double[][] channels)
        {
            var result = new double[_numChannels][];

            for (int ch = 0; ch < Math.Min(_numChannels, channels.Length); ch++)
            {
                int chIdx = ch < 12 ? ch : 0;
                double mean = ch < _scalers.Means.Length ? _scalers.Means[ch] : 0.0;
                double std = ch < _scalers.Stds.Length ? _scalers.Stds[ch] : 1.0;
                if (std < 1e-10) std = 1.0;

                var outCh = new double[_seqLength];
                for (int i = 0; i < _seqLength; i++)
                {
                    outCh[i] = (channels[chIdx][i] - mean) / std;
                }
                result[ch] = outCh;
            }

            return result;
        }

        /// <summary>
        /// Шаг 6: Запуск ONNX инференса. Вход: (1, 12, 240).
        /// </summary>
        private double RunInference(double[][] normalizedChannels)
        {
            var inputTensor = new float[1, _numChannels, _seqLength];
            for (int ch = 0; ch < _numChannels; ch++)
            {
                for (int t = 0; t < _seqLength; t++)
                {
                    inputTensor[0, ch, t] = (float)normalizedChannels[ch][t];
                }
            }

            var inputName = _session.InputMetadata.Keys.FirstOrDefault() ?? "input";
            var flatInput = new float[1 * _numChannels * _seqLength];
            Buffer.BlockCopy(inputTensor, 0, flatInput, 0, flatInput.Length * sizeof(float));
            var denseTensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(flatInput, new[] { 1, _numChannels, _seqLength });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, denseTensor)
            };

            using var outputs = _session.Run(inputs);
            var outputArray = outputs.First().AsEnumerable<float>().ToArray();

            return outputArray.Length > 0 ? outputArray[0] : 0.0;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }

    internal class ScalersData
    {
        public double[] Means { get; private set; } = Array.Empty<double>();
        public double[] Stds { get; private set; } = Array.Empty<double>();
        public double DistMin { get; private set; }
        public double DistMax { get; private set; }
        public int NumChannels { get; private set; } = 12;
        public int SeqLength { get; private set; } = 240;

        public static ScalersData Load(string path)
        {
            var json = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var data = new ScalersData
            {
                Means = LoadDoubleArray(root, "signal_means"),
                Stds = LoadDoubleArray(root, "signal_stds"),
                DistMin = root.TryGetProperty("dist_min", out var dmin) ? dmin.GetDouble() : 0.0,
                DistMax = root.TryGetProperty("dist_max", out var dmax) ? dmax.GetDouble() : 50.0,
                NumChannels = root.TryGetProperty("num_channels", out var nc) ? nc.GetInt32() : 12,
                SeqLength = root.TryGetProperty("seq_length", out var sl) ? sl.GetInt32() : 240,
            };

            return data;
        }

        private static double[] LoadDoubleArray(System.Text.Json.JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var arr) || arr.ValueKind != System.Text.Json.JsonValueKind.Array)
                return Array.Empty<double>();

            return arr.EnumerateArray().Select(x => x.GetDouble()).ToArray();
        }
    }
}