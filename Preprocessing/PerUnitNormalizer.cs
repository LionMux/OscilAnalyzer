namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Перевод сигналов в относительные единицы (per-unit, p.u.).
    /// Воспроизводит apply_pu_normalization из inference.py.
    /// 
    /// Формулы:
    ///   I_base_rms = S_base / (√3 · U_nom)
    ///   U_base_phase_rms = U_nom / √3
    ///   I_base_peak = I_base_rms · √2
    ///   U_base_phase_peak = U_base_phase_rms · √2
    ///   
    /// Токи (каналы 0–2) делятся на I_base_peak.
    /// Напряжения (каналы 3–5) делятся на U_base_phase_peak (в кВ).
    /// </summary>
    public class PerUnitNormalizer : ISignalPreprocessingStep
    {
        private readonly double _iBasePeak;
        private readonly double _uBasePhasePeakKv;

        public PerUnitNormalizer(ModelConfig config)
        {
            double unomKv = config.LineUnomKv;
            double sBaseMva = config.SBaseMva;

            // I_base_rms = S_base [ВА] / (√3 · U_nom [В])
            double iBaseRms = (sBaseMva * 1_000_000.0) / (Math.Sqrt(3) * unomKv * 1000.0);
            // U_base_phase_rms = U_nom / √3 [кВ] - ТОЧНО КАК В ПИТОНЕ
            double uBasePhaseRmsKv = unomKv / Math.Sqrt(3);

            // Пиковые значения (× √2)
            _iBasePeak = iBaseRms * Math.Sqrt(2);
            _uBasePhasePeakKv = uBasePhaseRmsKv * Math.Sqrt(2);
        }

        public void Process(PreprocessingContext context)
        {
            var channels = context.PhaseChannels;
            if (channels == null || channels.Length < 6)
                throw new InvalidOperationException("PerUnitNormalizer требует минимум 6 фазных каналов");

            // ИМИТИРУЕМ ПИТОНОВСКИЙ БАГ НА 12-КАНАЛЬНОМ ТЕНЗОРЕ:
            // Чтобы симметричные составляющие рассчитались из правильных фазных величин (без нормализации),
            // а потом нормализация легла поверх них с нужным багом, мы переносим ВСЮ
            // нормализацию в TensorAssembler. 
            // Здесь метод ничего не делает.
        }
    }
}
