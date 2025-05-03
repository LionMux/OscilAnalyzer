namespace COMTRADE_parser
{
    using System.Collections.Generic;

    // Общие модели данных
    public class ComtradeConfig
    {
        public string StationName { get; set; }
        public int AnalogChannelsCount { get; set; }
        public int DiscreteChannelsCount { get; set; }
        public DataFileType Encoding { get; set; }
        public double LineFrequency { get; set; }
        public double Rate { get; set; }
        public long EndSample { get; set; }
        public List<AnalogChannelConfig> AnalogChannels { get; } = new List<AnalogChannelConfig>();
        public List<DiscreteChannelConfig> DiscreteChannels { get; } = new List<DiscreteChannelConfig>();
    }

    public class AnalogChannelConfig
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public double A { get; set; }
        public string Unit { get; set; }
        public double B { get; set; }
    }

    public class DiscreteChannelConfig
    {
        public int Index { get; set; }
        public string Name { get; set; }
    }

    public enum DataFileType
    {
        ASCII,
        Binary
    }
}
