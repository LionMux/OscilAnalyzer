namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Удаление постоянной составляющей (DC) из сигналов.
    /// Воспроизводит center_by_prehistory + remove_dc_period из dc_filters.py.
    /// 
    /// Шаг 1: Вычесть среднее предаварийного участка (первые 20 мс).
    /// Шаг 2: Вычесть скользящее среднее с центрированным окном = 1 период сети.
    /// </summary>
    public class DcRemover : ISignalPreprocessingStep
    {
        private const double PreHistoryMs = 20.0;

        public void Process(PreprocessingContext context)
        {
            var channels = context.PhaseChannels;
            double fs = context.SamplingFrequencyHz;
            double fNet = context.MainsFrequencyHz;

            for (int ch = 0; ch < channels.Length; ch++)
            {
                channels[ch] = CenterByPrehistory(channels[ch], fs);
                channels[ch] = RemoveDcPeriod(channels[ch], fs, fNet);
            }
        }

        /// <summary>
        /// Вычитает среднее первых pre_ms миллисекунд (предаварийный участок).
        /// Соответствует center_by_prehistory из dc_filters.py.
        /// </summary>
        private static double[] CenterByPrehistory(double[] signal, double fs)
        {
            int preWindow = Math.Max((int)(PreHistoryMs * 1e-3 * fs), 1);
            var result = new double[signal.Length];

            double mean = 0.0;
            int count = Math.Min(preWindow, signal.Length);
            for (int i = 0; i < count; i++)
                mean += signal[i];
            mean /= count;

            for (int i = 0; i < signal.Length; i++)
                result[i] = signal[i] - mean;

            return result;
        }

        /// <summary>
        /// Вычитает скользящее среднее с центрированным окном длиной в 1 период сети.
        /// Соответствует remove_dc_period из dc_filters.py (pandas rolling, center=True, min_periods=1).
        /// Края обрабатываются заменой на среднее ближайшего полного окна.
        /// </summary>
        private static double[] RemoveDcPeriod(double[] signal, double fs, double fNet)
        {
            int period = Math.Max((int)Math.Round(fs / fNet), 1);
            if (period < 2 || period >= signal.Length)
                return (double[])signal.Clone();

            int n = signal.Length;
            var rollingMean = new double[n];

            // Центрированное скользящее среднее (аналог pandas rolling center=True, min_periods=1)
            int half = period / 2;
            for (int i = 0; i < n; i++)
            {
                int start = Math.Max(0, i - half);
                int end = Math.Min(n, i + half + 1);
                // Для чётного окна: pandas center=True с window=period
                // Эквивалентно: [i - period//2, i + period//2 + 1) с обрезкой по краям
                if (end - start > period)
                    end = start + period;

                double sum = 0.0;
                for (int j = start; j < end; j++)
                    sum += signal[j];
                rollingMean[i] = sum / (end - start);
            }

            // Обработка краёв: pandas заполняет крайние значения средним из ближайшего полного окна
            if (half > 0)
            {
                // Левый край: заменить первые half значений средним из rollingMean[half..period]
                double leftEdgeMean = 0.0;
                int leftCount = 0;
                for (int i = half; i < Math.Min(period, n); i++)
                {
                    leftEdgeMean += rollingMean[i];
                    leftCount++;
                }
                if (leftCount > 0)
                {
                    leftEdgeMean /= leftCount;
                    for (int i = 0; i < Math.Min(half, n); i++)
                        rollingMean[i] = leftEdgeMean;
                }

                // Правый край: заменить последние half значений средним из rollingMean[n-period..n-half]
                double rightEdgeMean = 0.0;
                int rightCount = 0;
                for (int i = Math.Max(0, n - period); i < Math.Max(0, n - half); i++)
                {
                    rightEdgeMean += rollingMean[i];
                    rightCount++;
                }
                if (rightCount > 0)
                {
                    rightEdgeMean /= rightCount;
                    for (int i = Math.Max(0, n - half); i < n; i++)
                        rollingMean[i] = rightEdgeMean;
                }
            }

            var result = new double[n];
            for (int i = 0; i < n; i++)
                result[i] = signal[i] - rollingMean[i];

            return result;
        }
    }
}
