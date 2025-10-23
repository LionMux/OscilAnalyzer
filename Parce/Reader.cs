
using System.Data;

namespace COMTRADE_parser
{
    public class Reader : IDataReader
    {
        public ComtradeConfig Config { get; }
        public List<List<double>> AnalogData { get; }
        public List<List<bool>> DiscreteData { get; }
        public List<double> DataTime { get; }

        public Reader(string cfgPath, string datPath)
        {
            DataTime = new List<double>();
            ICfgParser cfgParser = new CfgParser();
            Config = cfgParser.Parse(cfgPath);
            IDatParser datParser = new AsciiDatParser();
            AnalogData = datParser.ParseAnalogData(datPath, Config);
            DiscreteData = datParser.ParseDiscreteData(datPath, Config);
            DataTime = datParser.DatTime;
        }
    }
}
