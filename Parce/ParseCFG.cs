
using COMTRADE_parser;
using OscilAnalyzer.Parce;
using System.Globalization;
using System.IO;

class ParseCFG
{
    public class CfgParser : ICfgParser
    {
        public ComtradeConfig Parse(string filePath)
        {
            var config = new ComtradeConfig();
            var lines = File.ReadAllLines(filePath);

            ParseHeader(lines, config);
            ParseChannels(lines, config);
            ParseMetadata(lines, config);

            return config;
        }

        private void ParseHeader(string[] lines, ComtradeConfig config)
        {
            // Первая строка: Название станции
            config.StationName = lines[0].Split(',')[0].Trim();

            // Вторая строка: Количество каналов
            var channelInfo = lines[1].Split(',');
            config.AnalogChannelsCount = int.Parse(channelInfo[1].Replace("A", ""));
            config.DiscreteChannelsCount = int.Parse(channelInfo[2].Replace("D", ""));
        }

        private void ParseChannels(string[] lines, ComtradeConfig config)
        {
            // Аналоговые каналы (строки 2-35)
            for (int i = 2; i < 2 + config.AnalogChannelsCount; i++)
            {
                var line = lines[i];
                var parts = line.Split(',');

                // Проверка на достаточное количество элементов
                if (parts.Length < 8)
                {
                    throw new FormatException($"Ошибка в строке {i + 1}: недостаточно данных.");
                }

                // Очистка значений от пробелов и лишних символов
                var aValue = parts[6].Trim();
                var bValue = parts[7].Trim();

                // Парсинг с указанием культуры (разделитель точка)
                config.AnalogChannels.Add(new AnalogChannelConfig
                {
                    Index = int.Parse(parts[0].Trim()),
                    Name = parts[1].Trim(),
                    Unit = parts[4].Trim(),
                    A = double.Parse(aValue, CultureInfo.InvariantCulture),
                    B = double.Parse(bValue, CultureInfo.InvariantCulture)
                });
            }

            // Дискретные каналы (строки 35-56)
            for (int i = 2 + config.AnalogChannelsCount; i < 2 + config.AnalogChannelsCount + config.DiscreteChannelsCount; i++)
            {
                var parts = lines[i].Split(',');
                config.DiscreteChannels.Add(new DiscreteChannelConfig
                {
                    Index = int.Parse(parts[0]),
                    Name = parts[1]
                });
            }
        }

        private void ParseMetadata(string[] lines, ComtradeConfig config)
        {
            string line = lines[lines.Length-5].Trim();
            config.LineFrequency = double.Parse(line, CultureInfo.InvariantCulture);
            string[] line1 = lines[lines.Length-4].Trim().Split(",");
            config.Rate = double.Parse(line1[0], CultureInfo.InvariantCulture);
            config.EndSample = long.Parse(line1[1], CultureInfo.InvariantCulture);
            config.Encoding = lines[lines.Length-1].Trim().Equals("ASCII", StringComparison.OrdinalIgnoreCase)
                ? DataFileType.ASCII
                : DataFileType.Binary;
        }
    }

}