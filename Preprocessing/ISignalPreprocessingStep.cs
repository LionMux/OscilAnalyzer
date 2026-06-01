namespace OscilAnalyzer.Preprocessing
{
    /// <summary>
    /// Один шаг предобработки сигнала в конвейере (Pipeline Pattern).
    /// Каждая реализация выполняет ровно одну операцию над контекстом.
    /// </summary>
    public interface ISignalPreprocessingStep
    {
        /// <summary>
        /// Применяет преобразование к контексту пайплайна.
        /// </summary>
        /// <param name="context">Контекст, содержащий текущее состояние сигналов.</param>
        void Process(PreprocessingContext context);
    }
}
