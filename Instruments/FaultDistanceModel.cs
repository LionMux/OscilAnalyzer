using Microsoft.ML.OnnxRuntime;
using OscilAnalyzer.Preprocessing;
using System.IO;

namespace OscilAnalyzer
{
    /// <summary>
    /// Класс для расчёта расстояния до места повреждения через ONNX-модель CNN1D.
    /// На вход подаётся сырая осциллограмма (6 фазных каналов).
    /// Имплементирует конвейер предобработки, идентичный Python-пайплайну обучения (p.u. -> DC -> t0 -> Crop -> SymSeq).
    /// </summary>
    public class FaultDistanceModel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly ModelConfig _config;
        private readonly ISignalPreprocessingStep[] _pipeline;

        public FaultDistanceModel(string modelDir, ModelConfig? config = null)
        {
            var dir = modelDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var onnxPath = Path.Combine(dir, "best_model.onnx");

            if (!File.Exists(onnxPath))
                throw new FileNotFoundException($"ONNX модель не найдена: {onnxPath}");

            _session = new InferenceSession(onnxPath);
            _config = config ?? new ModelConfig();
            
            // Сборка конвейера предобработки в строгом порядке (сначала удаляем DC, затем ищем t0)
            _pipeline = new ISignalPreprocessingStep[]
            {
                new SignalResampler(_config),
                new PerUnitNormalizer(_config),
                new DcRemover(),                     // Сначала удаляем DC!
                new FaultInceptionDetector(_config), // Теперь t0 ищется на сигналах после удаления DC (как в Python!)
                new SlidingSymSeqCalculator(), 
                new SignalCropper(_config),    
                new TensorAssembler(_config)
            };
        }

        /// <summary>
        /// Рассчитывает расстояние до места повреждения по сырой осциллограмме.
        /// </summary>
        /// <param name="ia, ib, ic">Токи фаз A, B, C (в Амперах)</param>
        /// <param name="ua, ub, uc">Напряжения фаз A, B, C (в Вольтах)</param>
        /// <param name="fsHz">Частота дискретизации, Гц</param>
        /// <returns>Расстояние в км</returns>
        public double Predict(
            double[] ia, double[] ib, double[] ic,
            double[] ua, double[] ub, double[] uc,
            double fsHz)
        {
            // 1. Формирование контекста для конвейера
            var context = new PreprocessingContext
            {
                PhaseChannels = new[] 
                { 
                    (double[])ia.Clone(), 
                    (double[])ib.Clone(), 
                    (double[])ic.Clone(), 
                    (double[])ua.Clone(), 
                    (double[])ub.Clone(), 
                    (double[])uc.Clone() 
                },
                SamplingFrequencyHz = fsHz,
                MainsFrequencyHz = _config.MainsFreqHz,
                SeqLength = _config.SeqLength,
                NumChannels = _config.NumChannels
            };

            ValidateSignals(context.PhaseChannels);

            // 2. Прогон через все шаги предобработки
            foreach (var step in _pipeline)
            {
                step.Process(context);
            }
            Console.WriteLine($"[DEBUG C#] FaultInceptionIndex = {context.FaultInceptionIndex}");

            // 3. ONNX инференс (получаем предсказание в p.u.)
            if (context.Tensor == null)
                throw new InvalidOperationException("Тензор не был собран шагом TensorAssembler.");

            double distNorm = RunInference(context.Tensor);

            // 4. Денормализация (p.u. -> километры)
            // В модели с p.u. нормализацией дистанция предсказывается как доля от длины линии [0..1]
            return distNorm * _config.LineLengthKm;
        }

        private static void ValidateSignals(double[][] channels)
        {
            int len = channels[0].Length;
            for (int i = 1; i < channels.Length; i++)
            {
                if (channels[i].Length != len)
                    throw new ArgumentException("Все сигналы должны иметь одинаковую длину");
            }
        }

        /// <summary>
        /// Запуск ONNX инференса. Входной тензор имеет форму [1, NumChannels, SeqLength].
        /// </summary>
        private double RunInference(float[,] inputTensor)
        {
            int numChannels = _config.NumChannels;
            int seqLength = _config.SeqLength;

            var inputName = _session.InputMetadata.Keys.FirstOrDefault() ?? "input";
            
            // Flatten 2D тензора в 1D массив
            var flatInput = new float[numChannels * seqLength];
            int idx = 0;
            for (int c = 0; c < numChannels; c++)
            {
                for (int t = 0; t < seqLength; t++)
                {
                    flatInput[idx++] = inputTensor[c, t];
                }
            }

            var tensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(flatInput, new[] { 1, _config.NumChannels, _config.SeqLength });

            Console.WriteLine("DEBUG TENSOR HEAD (First 5 values per channel):");
            for(int c=0; c<_config.NumChannels; c++) {
                Console.Write($"Ch {c}: ");
                for(int t=0; t<5; t++) {
                    Console.Write($"{tensor[0, c, t]:F4} ");
                }
                Console.WriteLine();
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, tensor)
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
}