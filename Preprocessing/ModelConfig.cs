namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Параметры линии электропередачи и модели для инференса.
    /// Значения по умолчанию соответствуют чекпоинту модели 1.4681_MAE__cnn1d.
    /// </summary>
    public class ModelConfig
    {
        /// <summary>Номинальное напряжение линии, кВ.</summary>
        public double LineUnomKv { get; init; } = 110.0;

        /// <summary>Длина линии, км (для денормализации выхода модели).</summary>
        public double LineLengthKm { get; init; } = 50.0;

        /// <summary>Базовая мощность для p.u. нормализации, МВА.</summary>
        public double SBaseMva { get; init; } = 10.0;

        /// <summary>Целевая частота дискретизации для модели, Гц.</summary>
        public double TargetFsHz { get; init; } = 2000.0;

        /// <summary>Количество входных каналов модели.</summary>
        public int NumChannels { get; init; } = 12;

        /// <summary>Длина входной последовательности (отсчётов).</summary>
        public int SeqLength { get; init; } = 240;

        /// <summary>Частота сети, Гц.</summary>
        public double MainsFreqHz { get; init; } = 50.0;

        /// <summary>Предаварийное окно, мс (для crop).</summary>
        public double T0PreMs { get; init; } = 50.0;

        /// <summary>Послеаварийное окно, мс (для crop).</summary>
        public double T0PostMs { get; init; } = 70.0;

        /// <summary>Порог роста тока (η_I), относительный.</summary>
        public double T0EtaI { get; init; } = 0.5;

        /// <summary>Порог падения напряжения (η_U), относительный.</summary>
        public double T0EtaU { get; init; } = 0.85;
    }
}
