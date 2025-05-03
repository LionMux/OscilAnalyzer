
namespace COMTRADE_parser
{
    public interface ICfgParser
    {
        ComtradeConfig Parse(string filePath);
    }

    public interface IDatParser
    {
        List<List<double>> ParseAnalogData(string filePath, ComtradeConfig config);
        List<List<bool>> ParseDiscreteData(string filePath, ComtradeConfig config);
        List<double> DatTime { get;}
    }

    //public interface IDataExporter
    //{
    //    void ExportToCsv(ComtradeConfig config, List<List<double>> analogData, List<List<bool>> discreteData, string outputPath);
    //}

    public interface IDataReader
    {
        ComtradeConfig Config { get; }
        List<List<double>> AnalogData { get; }
        List<List<bool>> DiscreteData { get; }
    }
}
