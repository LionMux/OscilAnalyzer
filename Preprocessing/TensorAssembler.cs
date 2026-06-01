using System;

namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Собирает финальный тензор [12, SEQ_LENGTH] из фазных каналов и симметричных составляющих.
    /// 
    /// ВАЖНО: порядок каналов соответствует фактическому порядку обучения модели.
    /// FaultDataset при preprocessed=True читает ВСЕ столбцы CSV и берёт первые 12.
    /// В CSV 08_tensor_prepared первые 12 столбцов (после distance_km, fs_hz):
    ///   [0]  IA     (CT1IA  — фаза A, ток, kA)
    ///   [1]  IB     (CT1IB  — фаза B, ток, kA)
    ///   [2]  IC     (CT1IC  — фаза C, ток, kA)
    ///   [3]  UA     (BUS1UA — фаза A, напряжение, kV)
    ///   [4]  UB     (BUS1UB — фаза B, напряжение, kV)
    ///   [5]  UC     (BUS1UC — фаза C, напряжение, kV)
    ///   [6]  |I1|   (I1_mag — прямая последовательность, ток, kA)
    ///   [7]  |I2|   (I2_mag — обратная последовательность, ток, kA)
    ///   [8]  |I0|   (I0_mag — нулевая последовательность, ток, kA)
    ///   [9]  |U1|   (U1_mag — прямая последовательность, напряжение, kV)
    ///   [10] |U2|   (U2_mag — обратная последовательность, напряжение, kV)
    ///   [11] |U0|   (U0_mag — нулевая последовательность, напряжение, kV)
    /// </summary>
    public class TensorAssembler : ISignalPreprocessingStep
    {
        private readonly ModelConfig _config;

        public TensorAssembler(ModelConfig config)
        {
            _config = config;
        }

        public void Process(PreprocessingContext context)
        {
            var phase = context.PhaseChannels;
            var symseq = context.SymSeqChannels;

            if (phase == null || phase.Length < 6)
                throw new InvalidOperationException("TensorAssembler: необходимо 6 фазных каналов");
            if (symseq == null || symseq.Length < 6)
                throw new InvalidOperationException("TensorAssembler: необходимо 6 каналов симметричных составляющих");

            int numChannels = _config.NumChannels; // 12
            int seqLength = _config.SeqLength;      // 240

            var tensor = new float[numChannels, seqLength];

            // ПУ НОРМАЛИЗАЦИЯ — пиковые базовые значения (S_base=10 MVA, Unom=110 kV)
            double sBaseMva = _config.SBaseMva; // 10.0
            double unomKv = _config.LineUnomKv; // 110.0

            double iBaseRmsA = (sBaseMva * 1000000.0) / (Math.Sqrt(3) * unomKv * 1000.0);
            double uBasePhaseRmsKv = unomKv / Math.Sqrt(3);

            double iBasePeakA = iBaseRmsA * Math.Sqrt(2);
            double uBasePhasePeakKv = uBasePhaseRmsKv * Math.Sqrt(2);

            // Токи в COMTRADE приходят в килоамперах (kA)
            double iBasePeakKa = iBasePeakA / 1000.0;
            double uBasePeakKv = uBasePhasePeakKv;

            // [0..2] IA, IB, IC (kA → p.u.)
            for (int ch = 0; ch < 3; ch++)
            {
                int len = Math.Min(phase[ch].Length, seqLength);
                for (int t = 0; t < len; t++)
                    tensor[ch, t] = (float)(phase[ch][t] / iBasePeakKa);
            }

            // [3..5] UA, UB, UC (kV → p.u.)  ← ПОРЯДОК А: напряжения идут перед симм. составляющими токов
            for (int ch = 3; ch < 6; ch++)
            {
                int len = Math.Min(phase[ch].Length, seqLength);
                for (int t = 0; t < len; t++)
                    tensor[3 + (ch - 3), t] = (float)(phase[ch][t] / uBasePeakKv);
            }

            // [6..8] |I1|, |I2|, |I0| (kA → p.u.)
            for (int ch = 0; ch < 3; ch++)
            {
                int len = Math.Min(symseq[ch].Length, seqLength);
                for (int t = 0; t < len; t++)
                    tensor[6 + ch, t] = (float)(symseq[ch][t] / iBasePeakKa);
            }

            // [9..11] |U1|, |U2|, |U0| (kV → p.u.)
            for (int ch = 3; ch < 6; ch++)
            {
                int len = Math.Min(symseq[ch].Length, seqLength);
                for (int t = 0; t < len; t++)
                    tensor[9 + (ch - 3), t] = (float)(symseq[ch][t] / uBasePeakKv);
            }

            context.Tensor = tensor;
        }
    }
}
