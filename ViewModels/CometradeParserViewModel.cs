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

namespace OscilAnalyzer
{
    public class CometradeParserViewModel : BindableBase
    {
        //private List<double> _signalDataService._CurrentA;
        //private List<double> _signalDataService._CurrentB;
        //private List<double> _signalDataService._CurrentC;
        //private List<double> _signalDataService._VoltageA;
        //private List<double> _signalDataService._VoltageB;
        //private List<double> _signalDataService._VoltageC;
        //private List<double> _signalDataService._TimeValues;
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
        private bool _stopReadSelectSignal;
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
        private readonly SignalDataService _signalDataService;

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
        //public List<double> TimeValues { get => _signalDataService._TimeValues; set => _signalDataService._TimeValues = value; }
        public List<string> SignalNames { get => _signalNames; set => _signalNames = value; }

        public Plotter PlotIA { get => _plotIA; set => SetProperty(ref _plotIA, value); }
        public Plotter PlotIB { get => _plotIB; set => SetProperty(ref _plotIB, value); }
        public Plotter PlotIC { get => _plotIC; set => SetProperty(ref _plotIC, value); }
        public Plotter PlotUA { get => _plotUA; set => SetProperty(ref _plotUA, value); }
        public Plotter PlotUB { get => _plotUB; set => SetProperty(ref _plotUB, value); }
        public Plotter PlotUC { get => _plotUC; set => SetProperty(ref _plotUC, value); }
        public bool StopReadSelectSignal { get => _stopReadSelectSignal; set => SetProperty(ref _stopReadSelectSignal, value); }

        public CometradeParserViewModel(SignalDataService signalDataService)
        {
            _signalDataService = signalDataService;
            SignalALLNames = new List<string>();
            SignalNames = new List<string>();
            StartRead = new DelegateCommand(ReadSignal);
            SelectSignal = new DelegateCommand(SelectPhaseSignal, CanReadSelectSignal);

        }

        public void ReadSignal()
        {
            _signalDataService.CurrentA = new List<double>();
            _signalDataService.CurrentB = new List<double>();
            _signalDataService.CurrentC = new List<double>();
            _signalDataService.VoltageA = new List<double>();
            _signalDataService.VoltageB = new List<double>();
            _signalDataService.VoltageC = new List<double>();
            _signalDataService.TimeValues = new List<double>();

            // Открытие файла с осциллограммой
            OpenCfgDialog();
            reader = new Reader(CfgFileName, DatFileName);
            _analogChanells = reader.Config.AnalogChannels;
            SignalALLNames = _analogChanells.Select(x => x.Name).ToList();

            // Очистка старых данных
            _signalDataService.CurrentA.Clear();
            _signalDataService.CurrentB.Clear();
            _signalDataService.CurrentC.Clear();
            _signalDataService.VoltageA.Clear();
            _signalDataService.VoltageB.Clear();
            _signalDataService.VoltageC.Clear();
            _signalDataService.TimeValues.Clear();

            // Перевод времени из микросек. в миллисек.
            foreach (var time in reader.DataTime)
            {
                _signalDataService.TimeValues.Add(time / 1000);
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
            else
            {
                throw new NullReferenceException();
            }
            // Заполнение сигналов токов и напряжений пофазно
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
                                    _signalDataService.CurrentA.Add(sample[index]);
                                    break;
                                case 2:
                                    _signalDataService.CurrentB.Add(sample[index]);
                                    break;
                                case 3:
                                    _signalDataService.CurrentC.Add(sample[index]);
                                    break;
                                case 4:
                                    _signalDataService.VoltageA.Add(sample[index]);
                                    break;
                                case 5:
                                    _signalDataService.VoltageB.Add(sample[index]);
                                    break;
                                case 6:
                                    _signalDataService.VoltageC.Add(sample[index]);
                                    break;
                            }
                    }
                }
                j++;
            }

            // Построение графиков
            Plot();

            // Блокировка повторного заполнения сигналов
            StopReadSelectSignal = true;
            SelectSignal.RaiseCanExecuteChanged();
        }

        private bool CanReadSelectSignal()
        {
            return !string.IsNullOrEmpty(CurrentAName)
            && !string.IsNullOrEmpty(CurrentBName)
            && !string.IsNullOrEmpty(CurrentCName)
            && !string.IsNullOrEmpty(VoltageAName)
            && !string.IsNullOrEmpty(VoltageBName)
            && !string.IsNullOrEmpty(VoltageCName)
            && !StopReadSelectSignal;
        }

        private void Plot()
        {
            PlotIA = new Plotter(_signalDataService.CurrentA, _signalDataService.TimeValues, CurrentAName, "A");
            PlotIB = new Plotter(_signalDataService.CurrentB, _signalDataService.TimeValues, CurrentBName, "A");
            PlotIC = new Plotter(_signalDataService.CurrentC, _signalDataService.TimeValues, CurrentCName, "A");
            PlotUA = new Plotter(_signalDataService.VoltageA, _signalDataService.TimeValues, VoltageAName, "V");
            PlotUB = new Plotter(_signalDataService.VoltageB, _signalDataService.TimeValues, VoltageBName, "V");
            PlotUC = new Plotter(_signalDataService.VoltageC, _signalDataService.TimeValues, VoltageCName, "V");
            
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
