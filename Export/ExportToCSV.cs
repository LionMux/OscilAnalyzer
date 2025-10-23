using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace COMTRADE_parser.Export
{
    public class CsvExporter
    {
        /// <summary>
        /// Экспортирует данные в CSV с вертикальным расположением сигналов (каждый сигнал в отдельном столбце)
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="data">Данные в формате: List<List<double>> где каждый внутренний список - значения всех сигналов в один момент времени</param>
        /// <param name="samplingRate">Частота дискретизации (Гц)</param>
        /// <param name="decimalSeparator">Разделитель десятичных знаков</param>
        public static void ExportToCsv(string filePath, List<List<double>> data, double samplingRate = 1000, string decimalSeparator = ".")
        {
            if (data == null || data.Count == 0)
            {
                throw new ArgumentException("Нет данных для экспорта");
            }

            // Настройка культуры для форматирования чисел
            var culture = decimalSeparator == "," ? CultureInfo.GetCultureInfo("ru-RU") : CultureInfo.GetCultureInfo("en-US");
            double timeStep = 1.0 / samplingRate;

            // Проверяем, что все подсписки имеют одинаковую длину
            int signalCount = data[0].Count;
            if (data.Any(sublist => sublist.Count != signalCount))
            {
                throw new ArgumentException("Все подсписки данных должны иметь одинаковую длину");
            }

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Заголовок - время + номера сигналов
                var headers = new List<string> { "Time(sec)" };
                for (int i = 0; i < signalCount; i++)
                {
                    headers.Add($"Signal_{i + 1}");
                }
                writer.WriteLine(string.Join(",", headers));

                // Транспонируем данные: из строк-моментов времени делаем столбцы-сигналы
                var signals = new List<List<double>>();
                for (int signalIndex = 0; signalIndex < signalCount; signalIndex++)
                {
                    var signalValues = new List<double>();
                    for (int timeIndex = 0; timeIndex < data.Count; timeIndex++)
                    {
                        signalValues.Add(data[timeIndex][signalIndex]);
                    }
                    signals.Add(signalValues);
                }

                // Записываем данные построчно
                for (int i = 0; i < data.Count; i++)
                {
                    var lineParts = new List<string>
                    {
                        (i * timeStep).ToString("0.00000", culture)
                    };

                    // Добавляем значения всех сигналов для текущего момента времени
                    for (int signalIndex = 0; signalIndex < signalCount; signalIndex++)
                    {
                        lineParts.Add(signals[signalIndex][i].ToString("0.00000", culture));
                    }

                    writer.WriteLine(string.Join(",", lineParts));
                }
            }
        }
    }
}