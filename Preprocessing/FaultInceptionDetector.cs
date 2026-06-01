namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Определение момента возникновения аварии (t0) методом скользящего RMS.
    /// Воспроизводит detect_t0_rms из fault_inception.py.
    /// 
    /// Алгоритм:
    /// 1. Длина окна RMS: k = fs / (4 · f_net) — четверть периода.
    /// 2. Для каждого отсчёта i вычисляются:
    ///    - I_ratio = max(RMS_post(IA,IB,IC)) / max(RMS_pre(IA,IB,IC))
    ///    - U_ratio = min(RMS_post(UA,UB,UC)) / min(RMS_pre(UA,UB,UC))
    /// 3. Авария фиксируется, когда I_ratio > 1 + η_I И U_ratio &lt; η_U.
    /// 4. Первые 2 окна (2·k отсчётов) пропускаются (защита от ложных срабатываний).
    /// 5. Если t0 не найден, используется середина сигнала.
    /// </summary>
    public class FaultInceptionDetector : ISignalPreprocessingStep
    {
        private readonly ModelConfig _config;

        public FaultInceptionDetector(ModelConfig config)
        {
            _config = config;
        }

        public void Process(PreprocessingContext context)
        {
            var channels = context.PhaseChannels;
            double fs = context.SamplingFrequencyHz;
            double fNet = context.MainsFrequencyHz;

            // Токи: каналы 0, 1, 2; Напряжения: каналы 3, 4, 5
            int n = channels[0].Length;
            int k = (int)(fs / (4.0 * fNet));
            if (k < 1) k = 1;

            if (n < 2 * k)
            {
                context.FaultInceptionIndex = n / 2;
                return;
            }

            var iRatio = new double[n];
            var uRatio = new double[n];
            for (int i = 0; i < n; i++)
            {
                iRatio[i] = 1.0;
                uRatio[i] = 1.0;
            }

            for (int i = k; i < n - k; i++)
            {
                // RMS токов в окне [i-k, i) (pre) и [i, i+k) (post)
                double iPreMax = Math.Max(
                    Math.Max(WindowRms(channels[0], i - k, i), WindowRms(channels[1], i - k, i)),
                    WindowRms(channels[2], i - k, i));

                double iPostMax = Math.Max(
                    Math.Max(WindowRms(channels[0], i, i + k), WindowRms(channels[1], i, i + k)),
                    WindowRms(channels[2], i, i + k));

                double uPreMin = Math.Min(
                    Math.Min(WindowRms(channels[3], i - k, i), WindowRms(channels[4], i - k, i)),
                    WindowRms(channels[5], i - k, i));

                double uPostMin = Math.Min(
                    Math.Min(WindowRms(channels[3], i, i + k), WindowRms(channels[4], i, i + k)),
                    WindowRms(channels[5], i, i + k));

                if (i == 189)
                {
                    Console.WriteLine($"[189] iPreMax={iPreMax}, iPostMax={iPostMax}");
                    for(int j=189; j<199; j++) Console.Write($"{channels[0][j]:F4} ");
                    Console.WriteLine();
                }
                iRatio[i] = iPreMax > 1e-6 ? iPostMax / (iPreMax + 1e-9) : 1.0;
                uRatio[i] = uPreMin > 1e-6 ? uPostMin / (uPreMin + 1e-9) : 1.0;
            }

            // Поиск первого отсчёта, удовлетворяющего критерию, с пропуском первых 2 окон
            int skip = 2 * k;
            for (int i = skip; i < n; i++)
            {
                if (i == 189) Console.WriteLine($"iRatio[189]={iRatio[189]}, uRatio[189]={uRatio[189]}");
                if (iRatio[i] > 1.0 + _config.T0EtaI && uRatio[i] < _config.T0EtaU)
                {
                    context.FaultInceptionIndex = i;
                    return;
                }
            }

            // Авария не найдена — используем середину сигнала
            context.FaultInceptionIndex = n / 2;
        }

        /// <summary>
        /// Вычисляет RMS (среднеквадратичное) для среза сигнала [start, end).
        /// </summary>
        private static double WindowRms(double[] signal, int start, int end)
        {
            start = Math.Max(0, start);
            end = Math.Min(signal.Length, end);
            if (end <= start) return 0.0;

            double sumSq = 0.0;
            for (int i = start; i < end; i++)
                sumSq += signal[i] * signal[i];

            return Math.Sqrt(sumSq / (end - start));
        }
    }
}
