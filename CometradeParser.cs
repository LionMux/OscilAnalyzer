using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System;
using COMTRADE_parser;
using Prism.Mvvm;
using ScottPlot;
using System.Reflection.Emit;
using static System.Net.Mime.MediaTypeNames;

namespace OscilAnalyzer
{
    public class CometradeParser : BindableBase
    {
        private List<double> _currentA;
        private List<double> _currentB;
        private List<double> _currentC;
        private List<double> _voltageA;
        private List<double> _voltageB;
        private List<double> _voltageC;
        private List<double> _timeValues;
        private List<string> _signalALLNames;
        private List<string> _signalNames;
        private string _currentAName;
        private string _currentBName;
        private string _currentCName;
        private string _voltageAName;
        private string _voltageBName;
        private string _voltageCName;
        private double _numOfPoints;
        private string _cfgFileName;
        private string _datFileName;
        private Plotter _plotIA;
        private Plotter _plotIB;
        private Plotter _plotIC;
        private Plotter _plotUA;
        private Plotter _plotUB;
        private Plotter _plotUC;
        public DelegateCommand StartRead { get; set; }
        public DelegateCommand SelectSignal { get; set; }
        public DelegateCommand SelectCfgFile { get; set; }

        Reader reader;
        List<AnalogChannelConfig> _analogChanells;

        public List<string> SignalALLNames { get => _signalALLNames; set => SetProperty(ref _signalALLNames, value); }
        public string CurrentAName { get => _currentAName; set => UpdateSignal(ref _currentAName, value); }
        public string CurrentBName { get => _currentBName; set => UpdateSignal(ref _currentBName, value); }
        public string CurrentCName { get => _currentCName; set => UpdateSignal(ref _currentCName, value); }
        public string VoltageAName { get => _voltageAName; set => UpdateSignal(ref _voltageAName, value); }
        public string VoltageBName { get => _voltageBName; set => UpdateSignal(ref _voltageBName, value); }
        public string VoltageCName { get => _voltageCName; set => UpdateSignal(ref _voltageCName, value); }
        public string CfgFileName { get => _cfgFileName; set => UpdateSignal(ref _cfgFileName, value); }
        public string DatFileName { get => _datFileName; set => UpdateSignal(ref _datFileName, value); }

        public double NumOfPoints { get => _numOfPoints; set => _numOfPoints = value; }
        public List<double> TimeValues { get => _timeValues; set => _timeValues = value; }
        public List<string> SignalNames { get => _signalNames; set => _signalNames = value; }

        public Plotter PlotIA { get => _plotIA; set => SetProperty(ref _plotIA, value); }
        public Plotter PlotIB { get => _plotIB; set => SetProperty(ref _plotIB, value); }
        public Plotter PlotIC { get => _plotIC; set => SetProperty(ref _plotIC, value); }
        public Plotter PlotUA { get => _plotUA; set => SetProperty(ref _plotUA, value); }
        public Plotter PlotUB { get => _plotUB; set => SetProperty(ref _plotUB, value); }
        public Plotter PlotUC { get => _plotUC; set => SetProperty(ref _plotUC, value); }

        public CometradeParser()
        {
            SignalALLNames = new List<string>();
            SignalNames = new List<string>();
            SelectCfgFile = new DelegateCommand(OpenCfgDialog);
            StartRead = new DelegateCommand(ReadSignal, CanReadStart);
            SelectSignal = new DelegateCommand(SelectPhaseSignal, CanReadSelectSignal);

        }

        public void ReadSignal()
        {
            _currentA = new List<double>();
            _currentB = new List<double>();
            _currentC = new List<double>();
            _voltageA = new List<double>();
            _voltageB = new List<double>();
            _voltageC = new List<double>();
            TimeValues = new List<double>();

            reader = new Reader(CfgFileName, DatFileName);
            _analogChanells = reader.Config.AnalogChannels;
            SignalALLNames = _analogChanells.Select(x => x.Name).ToList();

            // Очистка старых данных
            _currentA.Clear();
            _currentB.Clear();
            _currentC.Clear();
            _voltageA.Clear();
            _voltageB.Clear();
            _voltageC.Clear();
            TimeValues.Clear();

            // Перевод из микросек. в миллисек.
            foreach (var time in reader.DataTime)
            {
                TimeValues.Add(time / 1000);
            }
        }
        public void SelectPhaseSignal()
        {
            int j = 1;
            if (reader != null)
            {
                SignalNames.Add(CurrentAName);
                SignalNames.Add(CurrentBName);
                SignalNames.Add(CurrentCName);
                SignalNames.Add(VoltageAName);
                SignalNames.Add(VoltageBName);
                SignalNames.Add(VoltageCName);
            }
            //заполнение фаз токов и напряжений
            foreach (var signalName in SignalNames)
            {

                int index = _analogChanells.FindIndex(x => x.Name == signalName);
                if (SignalALLNames.Count != 0)
                {
                    foreach (var sample in reader.AnalogData)
                    {
                            switch (j)
                            {
                                case 1:
                                    _currentA.Add(sample[index]);
                                    break;
                                case 2:
                                    _currentB.Add(sample[index]);
                                    break;
                                case 3:
                                    _currentC.Add(sample[index]);
                                    break;
                                case 4:
                                    _voltageA.Add(sample[index]);
                                    break;
                                case 5:
                                    _voltageB.Add(sample[index]);
                                    break;
                                case 6:
                                    _voltageC.Add(sample[index]);
                                    break;
                            }
                    }
                }
                j++;
            }

            //Построение графиков
            Plot();
        }
        private bool CanReadSelectSignal()
        {
            return !string.IsNullOrEmpty(CurrentAName)
            && !string.IsNullOrEmpty(CurrentBName)
            && !string.IsNullOrEmpty(CurrentCName)
            && !string.IsNullOrEmpty(VoltageAName)
            && !string.IsNullOrEmpty(VoltageBName)
            && !string.IsNullOrEmpty(VoltageCName);
        }

        private void Plot()
        {
            PlotIA = new Plotter(_currentA, TimeValues, CurrentAName, "A");
            PlotIB = new Plotter(_currentB, TimeValues, CurrentBName, "A");
            PlotIC = new Plotter(_currentC, TimeValues, CurrentCName, "A");
            PlotUA = new Plotter(_voltageA, TimeValues, VoltageAName, "V");
            PlotUB = new Plotter(_voltageB, TimeValues, VoltageBName, "V");
            PlotUC = new Plotter(_voltageC, TimeValues, VoltageCName, "V");
        }

        private bool CanReadStart()
        {
            return !string.IsNullOrEmpty(CfgFileName)
                && !string.IsNullOrEmpty(DatFileName);
        }
        private void BuildMultiPlot()
        {
            var multiPlot = new ScottPlot.Multiplot();

            // configure the multiplot to use 2 subplots
            multiPlot.AddPlots(2);
            var plot1 = multiPlot.Subplots.GetPlot(0);
            var plot2 = multiPlot.Subplots.GetPlot(1);

            // add sample data to each subplot
            plot1.Add.Scatter(TimeValues, _currentA);
            plot2.Add.Scatter(TimeValues, _currentB);

        }
        private void UpdateSignal(ref string field, string newValue)
        {
            SetProperty(ref field, newValue);
            SelectSignal.RaiseCanExecuteChanged();
            StartRead.RaiseCanExecuteChanged();

        }

        private void OpenCfgDialog()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "COMETRADE CFG|*.cfg|Все файлы|*.*",
                Title = "Выберите CFG-файл"
            };
            if (dlg.ShowDialog() == true)
            {
                CfgFileName = dlg.FileName;
                DatFileName = CfgFileName.Replace(".cfg", ".dat");
            }
            
        }
    }
}
