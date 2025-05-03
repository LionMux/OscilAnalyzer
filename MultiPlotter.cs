using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScottPlot.WPF;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using static System.Net.Mime.MediaTypeNames;

namespace OscilAnalyzer
{
    public class DataToPlot
    {
        public string NameSignal { get; }
        public string Unit { get; }
        public double[] DataY { get; }
        public double[] DataX { get; }
        public WpfPlot PlotControl { get; set; }
        public DataToPlot(IEnumerable<double> dataY, IEnumerable<double> dataX, string nameSignal, string unit)
        {

            DataY = dataY.ToArray();
            DataX = dataX.ToArray();
            NameSignal = nameSignal;
            Unit = unit ?? string.Empty; 

            if (DataX == null || DataY == null)
            {
                throw new ArgumentNullException("Данные не могут быть null");
            }
            if (DataX.Length != DataY.Length)
            {
                throw new ArgumentException("dataX и dataY должны быть одинаковой длины");
            }
        }
    }

    public class MultiPlotter
    {
        private string _nameSignal;
        private string _unit;
        private double[] _dataY;
        private double[] _dataX;
        public WpfPlot PlotControl { get; set; }
        public MultiPlotter(IEnumerable<DataToPlot> series, int rows)
        {

            if (series == null)
            {
                throw new ArgumentNullException(nameof(series));
            }
            if (rows <= 0 )
            {
                throw new ArgumentException("Номер строки должен быть положительным.");
            }
            var data = series.ToArray();

            // создаем MultiPlot
            var mp = new ScottPlot.Multiplot();
            mp.AddPlots(data.Length);
            Plot[] plots = new Plot[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                plots[i] = mp.Subplots.GetPlot(i);
                _dataY = data[i].DataY;
            }

            foreach (var plot in plots)
            {
                plot.Add.Scatter(series)
            }

            PlotControl.Refresh();
        }
        }
}
