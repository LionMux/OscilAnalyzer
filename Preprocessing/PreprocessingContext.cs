namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Контекст пайплайна предобработки. Передаётся между шагами конвейера
    /// и хранит текущее состояние сигналов, метаданные и промежуточные результаты.
    /// </summary>
    public class PreprocessingContext
    {
        /// <summary>
        /// 6 фазных каналов: [0]=IA, [1]=IB, [2]=IC, [3]=UA, [4]=UB, [5]=UC.
        /// Каждый элемент — массив отсчётов.
        /// </summary>
        public double[][] PhaseChannels { get; set; } = Array.Empty<double[]>();

        /// <summary>
        /// 6 каналов симметричных составляющих: [0]=|I1|, [1]=|I2|, [2]=|I0|, [3]=|U1|, [4]=|U2|, [5]=|U0|.
        /// Заполняется шагом SlidingSymSeqCalculator.
        /// </summary>
        public double[][]? SymSeqChannels { get; set; }

        /// <summary>Частота дискретизации, Гц.</summary>
        public double SamplingFrequencyHz { get; set; }

        /// <summary>Частота сети, Гц (50 или 60).</summary>
        public double MainsFrequencyHz { get; set; } = 50.0;

        /// <summary>
        /// Индекс момента возникновения аварии (t0).
        /// Заполняется шагом FaultInceptionDetector.
        /// </summary>
        public int? FaultInceptionIndex { get; set; }

        /// <summary>
        /// Финальный тензор [NUM_CHANNELS, SEQ_LENGTH] для подачи в ONNX-модель.
        /// Заполняется шагом TensorAssembler.
        /// </summary>
        public float[,]? Tensor { get; set; }

        /// <summary>Длина целевой последовательности (по умолчанию 240).</summary>
        public int SeqLength { get; set; } = 240;

        /// <summary>Количество каналов модели (по умолчанию 12).</summary>
        public int NumChannels { get; set; } = 12;
    }
}
