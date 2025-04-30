
using System.IO;
using OscilAnalyzer.Parce;

namespace COMTRADE_parser.Parce

{
    public class AsciiDatParser : IDatParser
    {
        private List<double> _datTime;
        public List<double> DatTime { get => _datTime;}

        public List<List<double>> ParseAnalogData(string filePath, ComtradeConfig config)
        {
            var analogData = new List<List<double>>();
            var DatTime = new List<double>();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var sample = new List<double>();
                var parts = line.Split(',');
                DatTime.Add(double.Parse(parts[1]));

                for (int i = 0; i < config.AnalogChannelsCount; i++)
                {
                    double rawValue = double.Parse(parts[2 + i]);
                    var channelConfig = config.AnalogChannels[i];
                    sample.Add(rawValue * channelConfig.A + channelConfig.B);
                }

                analogData.Add(sample);
            }

            return analogData;
        }

        public List<List<bool>> ParseDiscreteData(string filePath, ComtradeConfig config)
        {
            var discreteData = new List<List<bool>>();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var sample = new List<bool>();
                var parts = line.Split(',');
                int startIndex = 2 + config.AnalogChannelsCount;

                for (int i = 0; i < config.DiscreteChannelsCount; i++)
                {
                    sample.Add(parts[startIndex + i] == "1");
                }

                discreteData.Add(sample);
            }

            return discreteData;
        }
    }

}
