namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Обрезка сигналов вокруг момента аварии t0.
    /// Воспроизводит crop_around_t0 из fault_inception.py и _crop_around_t0 из inference.py.
    /// 
    /// Окно: [t0 - pre_ms, t0 + post_ms).
    /// При 2000 Гц и pre=50 мс, post=70 мс → 240 отсчётов = SEQ_LENGTH.
    /// Если окно выходит за пределы сигнала — сдвиг + дополнение нулями.
    /// Финальная обрезка/padding до точного SEQ_LENGTH.
    /// </summary>
    public class SignalCropper : ISignalPreprocessingStep
    {
        private readonly ModelConfig _config;

        public SignalCropper(ModelConfig config)
        {
            _config = config;
        }

        public void Process(PreprocessingContext context)
        {
            int t0 = context.FaultInceptionIndex ?? context.PhaseChannels[0].Length / 2;
            double fs = context.SamplingFrequencyHz;
            int seqLength = context.SeqLength;

            int preSamp = Math.Max((int)Math.Round(_config.T0PreMs * 1e-3 * fs), 1);
            int postSamp = Math.Max((int)Math.Round(_config.T0PostMs * 1e-3 * fs), 1);
            int windowLen = preSamp + postSamp;

            int n = context.PhaseChannels[0].Length;
            int start = t0 - preSamp;
            int end = start + windowLen;

            // Сдвиг окна внутрь диапазона [0, n)
            if (start < 0)
            {
                end -= start; // увеличиваем end на |start|
                start = 0;
            }
            if (end > n)
            {
                int shift = end - n;
                start -= shift;
                end = n;
            }
            start = Math.Max(0, start);
            end = Math.Min(n, end);
            int srcLen = end - start;

            var cropped = new double[context.PhaseChannels.Length][];

            for (int ch = 0; ch < context.PhaseChannels.Length; ch++)
            {
                var signal = context.PhaseChannels[ch];
                var window = new double[seqLength];

                for (int i = 0; i < seqLength; i++)
                {
                    int srcIdx = start + i;
                    if (srcIdx >= 0 && srcIdx < n && i < srcLen)
                        window[i] = signal[srcIdx];
                }

                cropped[ch] = window;
            }

            // Обрезка / padding до SEQ_LENGTH (если crop дал не ровно 240)
            for (int ch = 0; ch < cropped.Length; ch++)
            {
                if (cropped[ch].Length > seqLength)
                {
                    var trimmed = new double[seqLength];
                    Array.Copy(cropped[ch], cropped[ch].Length - seqLength, trimmed, 0, seqLength);
                    cropped[ch] = trimmed;
                }
                else if (cropped[ch].Length < seqLength)
                {
                    var padded = new double[seqLength];
                    Array.Copy(cropped[ch], 0, padded, 0, cropped[ch].Length);
                    cropped[ch] = padded;
                }
            }

            // Если SymSeqChannels уже вычислены, обрезаем и их (теми же индексами start..end)
            if (context.SymSeqChannels != null && context.SymSeqChannels.Length > 0)
            {
                var croppedSym = new double[context.SymSeqChannels.Length][];
                for (int ch = 0; ch < context.SymSeqChannels.Length; ch++)
                {
                    var signal = context.SymSeqChannels[ch];
                    var window = new double[seqLength];
                    for (int i = 0; i < seqLength; i++)
                    {
                        int srcIdx = start + i;
                        if (srcIdx >= 0 && srcIdx < signal.Length && i < srcLen)
                            window[i] = signal[srcIdx];
                    }
                    croppedSym[ch] = window;
                }

                // Обрезка / padding до SEQ_LENGTH
                for (int ch = 0; ch < croppedSym.Length; ch++)
                {
                    if (croppedSym[ch].Length > seqLength)
                    {
                        var trimmed = new double[seqLength];
                        Array.Copy(croppedSym[ch], croppedSym[ch].Length - seqLength, trimmed, 0, seqLength);
                        croppedSym[ch] = trimmed;
                    }
                    else if (croppedSym[ch].Length < seqLength)
                    {
                        var padded = new double[seqLength];
                        Array.Copy(croppedSym[ch], 0, padded, 0, croppedSym[ch].Length);
                        croppedSym[ch] = padded;
                    }
                }
                context.SymSeqChannels = croppedSym;
            }

            context.PhaseChannels = cropped;
        }
    }
}
