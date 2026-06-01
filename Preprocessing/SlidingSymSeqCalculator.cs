using System.Numerics;

namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Вычисление симметричных составляющих скользящим ДПФ с окном Ханна.
    /// Воспроизводит sliding_window_symseq из symseq.py + abc_to_seq (Фортескью).
    /// 
    /// Для каждого отсчёта t:
    /// 1. Берётся окно [t - period/2, t + period/2] длиной в 1 период сети.
    /// 2. Применяется оконная функция Ханна.
    /// 3. Вычисляется DFT на основной гармонике (k = round(f0 · n / fs)).
    /// 4. Фазоры преобразуются по Фортескью: abc → I0, I1, I2 / U0, U1, U2.
    /// 5. Выход: модули |I1|, |I2|, |I0|, |U1|, |U2|, |U0| (6 каналов).
    /// </summary>
    public class SlidingSymSeqCalculator : ISignalPreprocessingStep
    {
        // Поворотный оператор Фортескью: a = exp(j·2π/3)
        private static readonly Complex Alpha = new(Math.Cos(2.0 * Math.PI / 3.0), Math.Sin(2.0 * Math.PI / 3.0));
        private static readonly Complex Alpha2 = Alpha * Alpha;

        public void Process(PreprocessingContext context)
        {
            var channels = context.PhaseChannels;
            double fs = context.SamplingFrequencyHz;
            double f0 = context.MainsFrequencyHz;
            int T = channels[0].Length;

            int period = Math.Max((int)Math.Round(fs / f0), 1);
            int half = period / 2;

            // Результат: 6 каналов — I1, I2, I0, U1, U2, U0
            var result = new double[6][];
            for (int i = 0; i < 6; i++)
                result[i] = new double[T];

            for (int t = 0; t < T; t++)
            {
                int start = Math.Max(0, t - half);
                int end = Math.Min(T, t + half + 1); // +1 для нечётной длины
                int n = end - start;

                // Гармонический номер для основной частоты
                int k = Math.Min((int)Math.Round(f0 * n / fs), n - 1);

                // Окно Ханна длиной n
                var hann = new double[n];
                double hannSum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    hann[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / n));
                    hannSum += hann[i];
                }

                // DFT на гармонике k для каждого из 6 каналов
                var phasors = new Complex[6];
                for (int ch = 0; ch < 6; ch++)
                {
                    Complex sum = Complex.Zero;
                    for (int i = 0; i < n; i++)
                    {
                        double angle = -2.0 * Math.PI * k * i / n;
                        double sw = channels[ch][start + i] * hann[i];
                        sum += new Complex(sw * Math.Cos(angle), sw * Math.Sin(angle));
                    }
                    phasors[ch] = sum * (2.0 / hannSum);
                }

                // Преобразование Фортескью для токов (каналы 0, 1, 2)
                var (i0, i1, i2) = AbcToSeq(phasors[0], phasors[1], phasors[2]);
                result[0][t] = i1.Magnitude; // |I1| — прямая
                result[1][t] = i2.Magnitude; // |I2| — обратная
                result[2][t] = i0.Magnitude; // |I0| — нулевая

                // Преобразование Фортескью для напряжений (каналы 3, 4, 5)
                var (u0, u1, u2) = AbcToSeq(phasors[3], phasors[4], phasors[5]);
                result[3][t] = u1.Magnitude; // |U1| — прямая
                result[4][t] = u2.Magnitude; // |U2| — обратная
                result[5][t] = u0.Magnitude; // |U0| — нулевая
            }

            context.SymSeqChannels = result;
        }

        /// <summary>
        /// Преобразование Фортескью: фазные фазоры (a, b, c) → 
        /// симметричные составляющие (нулевая, прямая, обратная).
        /// 
        /// [X0]     1  [ 1   1      1    ] [Xa]
        /// [X1]  = --- [ 1   α      α²   ] [Xb]
        /// [X2]     3  [ 1   α²     α    ] [Xc]
        /// </summary>
        private static (Complex seq0, Complex seq1, Complex seq2) AbcToSeq(
            Complex a, Complex b, Complex c)
        {
            Complex seq0 = (a + b + c) / 3.0;
            Complex seq1 = (a + Alpha * b + Alpha2 * c) / 3.0;
            Complex seq2 = (a + Alpha2 * b + Alpha * c) / 3.0;
            return (seq0, seq1, seq2);
        }
    }
}
