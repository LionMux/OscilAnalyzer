using System;

namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Изменяет частоту дискретизации сигнала (ресемплинг) с помощью линейной интерполяции.
    /// Модель обучалась на 2000 Гц, поэтому если на вход приходит 5000 Гц,
    /// необходимо понизить частоту, чтобы окно 240 отсчетов покрывало нужные 120 мс.
    /// </summary>
    public class SignalResampler : ISignalPreprocessingStep
    {
        private readonly double _targetFs;

        public SignalResampler(ModelConfig config)
        {
            _targetFs = config.TargetFsHz;
        }

        public void Process(PreprocessingContext context)
        {
            double currentFs = context.SamplingFrequencyHz;
            if (Math.Abs(currentFs - _targetFs) < 1.0)
                return; // Уже нужная частота

            var channels = context.PhaseChannels;
            if (channels == null || channels.Length == 0) return;

            int numChannels = channels.Length;
            int originalLen = channels[0].Length;
            
            // Новая длина
            int newLen = (int)Math.Round(originalLen * (_targetFs / currentFs));

            for (int ch = 0; ch < numChannels; ch++)
            {
                var oldSig = channels[ch];
                var newSig = new double[newLen];

                for (int i = 0; i < newLen; i++)
                {
                    // Исходный "индекс" как вещественное число
                    double oldIdx = i * (currentFs / _targetFs);
                    int idx1 = (int)Math.Floor(oldIdx);
                    int idx2 = idx1 + 1;
                    
                    if (idx2 >= originalLen)
                    {
                        newSig[i] = oldSig[Math.Min(idx1, originalLen - 1)];
                    }
                    else
                    {
                        double frac = oldIdx - idx1;
                        newSig[i] = oldSig[idx1] * (1.0 - frac) + oldSig[idx2] * frac;
                    }
                }
                channels[ch] = newSig;
            }

            context.SamplingFrequencyHz = _targetFs;
        }
    }
}
