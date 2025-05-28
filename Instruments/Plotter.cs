using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScottPlot.WPF;
using ScottPlot;

namespace OscilAnalyzer
{
    public class Plotter
    {
        private string? _nameSignal;
        private string _unit;
        private double[] _dataY;
        private double[] _dataX;
        public WpfPlot PlotControl { get; set; }

        /// <summary>
        /// Строит график в соответствии с двумя одинаковыми массивами данных X и Y
        /// </summary>
        /// <param name="dataY">Значения оси ординат</param>
        /// <param name="dataX">Значения оси абцисс</param>
        /// <param name="nameSignal">Наименование сигнала</param>
        /// <param name="unit">Единицы измерения</param>
        public Plotter(List<double> dataY, List<double> dataX, string? nameSignal, string unit)
        {

            _dataY = dataY.ToArray();
            _dataX = dataX.ToArray();
            _nameSignal = nameSignal;
            _unit = unit ?? string.Empty;

            if (dataX == null || dataY == null)
            {
                throw new ArgumentNullException("Данные не могут быть null");
            }
            if (dataX.Count != dataY.Count)
            {
                throw new ArgumentException("dataX и dataY должны быть одинаковой длины");
            }

            PlotControl = new WpfPlot();
            CreatePlot();
        }

        private void CreatePlot()
        {
            var plt = PlotControl.Plot;
            plt.Clear();
            plt.Add.Scatter(_dataX, _dataY);
            plt.Title(_nameSignal);
            plt.XLabel("time, ms");

            switch (_unit.ToLower())
            {
                case "a": plt.YLabel("I, A"); break;
                case "v": plt.YLabel("U, V"); break;
                default: plt.YLabel(_unit); break;
            }
            PlotControl.Refresh();
        }
    }
}