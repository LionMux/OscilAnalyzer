using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System;
using COMTRADE_parser;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services;
using ScottPlot;

namespace OscilAnalyzer
{
    public class CometradeParserViewModel : BindableBase
    {
        private List<string?> _signalALLNames;
        private List<string> _signalNames;
        private string? _currentAName;
        private string? _currentBName;
        private string? _currentCName;
        private string? _voltageAName;
        private string? _voltageBName;
        private string? _voltageCName;
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
        public DelegateCommand MoveToNextCommand { get; set; }

        private Reader _reader;
        private List<AnalogChannelConfig> _analogChanells;
        private readonly SignalDataService _signalDataService;
        private readonly IRegionManager _regionManager;

        public List<string?> SignalALLNames { get => _signalALLNames; set => SetProperty(ref _signalALLNames, value); }
        public string? CurrentAName { get => _currentAName; set => UpdateSignal(ref _currentAName, value); }
        public string? CurrentBName { get => _currentBName; set => UpdateSignal(ref _currentBName, value); }
        public string? CurrentCName { get => _currentCName; set => UpdateSignal(ref _currentCName, value); }
        public string? VoltageAName { get => _voltageAName; set => UpdateSignal(ref _voltageAName, value); }
        public string? VoltageBName { get => _voltageBName; set => UpdateSignal(ref _voltageBName, value); }
        public string? VoltageCName { get => _voltageCName; set => UpdateSignal(ref _voltageCName, value); }
        public string CfgFileName { get => _cfgFileName; set => UpdateSignal(ref _cfgFileName, value); }
        public string DatFileName { get => _datFileName; set => UpdateSignal(ref _datFileName, value); }
        public List<string?> SignalNames { get => _signalNames; set => _signalNames = value; }

        public Plotter PlotIA { get => _plotIA; set => SetProperty(ref _plotIA, value); }
        public Plotter PlotIB { get => _plotIB; set => SetProperty(ref _plotIB, value); }
        public Plotter PlotIC { get => _plotIC; set => SetProperty(ref _plotIC, value); }
        public Plotter PlotUA { get => _plotUA; set => SetProperty(ref _plotUA, value); }
        public Plotter PlotUB { get => _plotUB; set => SetProperty(ref _plotUB, value); }
        public Plotter PlotUC { get => _plotUC; set => SetProperty(ref _plotUC, value); }
        public bool StopReadSelectSignal { get => _stopReadSelectSignal; set => SetProperty(ref _stopReadSelectSignal, value); }

        public CometradeParserViewModel(SignalDataService signalDataService, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _signalDataService = signalDataService;
            SignalALLNames = new List<string?>();
            SignalNames = new List<string?>();
            StartRead = new DelegateCommand(ReadSignal);
            SelectSignal = new DelegateCommand(SelectPhaseSignal, CanReadSelectSignal);
            MoveToNextCommand = new DelegateCommand(MoveToNext);
        }

        private void ReadSignal()
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
            _reader = new Reader(CfgFileName, DatFileName);
            _analogChanells = _reader.Config.AnalogChannels;
            SignalALLNames = _analogChanells.Select(x => x?.Name).ToList();

            CLearOldData();
            

            _signalDataService.NumOfPoints = (int)_reader.Config.EndSample;
            _signalDataService.PoOfPer = (int)(_reader.Config.Rate / _reader.Config.LineFrequency);
        }

        private void MoveToNext()
        {
            _regionManager.RequestNavigate("ContentRegion", "AnalizeOscillogramView");
        }

        private List<double> MicrosecondsToMilliseconds(IEnumerable<double> dataTime)
        {
            return dataTime.Select(t => t / 1000).ToList();
        }


        private void SelectPhaseSignal()
        {
            if (_reader != null)
            {
                SignalNames.Clear();
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
            FillCurrentAndVoltageSignals();
            _signalDataService.TimeValues = MicrosecondsToMilliseconds(_reader.DataTime);
            Plot();

            StopReadSelectSignal = true;
            SelectSignal.RaiseCanExecuteChanged();
        }

        private void CLearOldData()
        {
            _signalDataService.CurrentA.Clear();
            _signalDataService.CurrentB.Clear();
            _signalDataService.CurrentC.Clear();
            _signalDataService.VoltageA.Clear();
            _signalDataService.VoltageB.Clear();
            _signalDataService.VoltageC.Clear();
            _signalDataService.TimeValues.Clear();
        }
        private void FillCurrentAndVoltageSignals()
        {
            CLearOldData();
            int index = 0;
            for (int j = 0; j < SignalNames.Count; j++)
            {
                index = _analogChanells.FindIndex(x => x.Name == SignalNames[j]);
                if (SignalALLNames.Count != 0)
                {
                    foreach (var sample in _reader.AnalogData)
                    {
                        switch (j)
                        {
                            case 0:
                                _signalDataService.CurrentA.Add(sample[index]);
                                break;
                            case 1:
                                _signalDataService.CurrentB.Add(sample[index]);
                                break;
                            case 2:
                                _signalDataService.CurrentC.Add(sample[index]);
                                break;
                            case 3:
                                _signalDataService.VoltageA.Add(sample[index]);
                                break;
                            case 4:
                                _signalDataService.VoltageB.Add(sample[index]);
                                break;
                            case 5:
                                _signalDataService.VoltageC.Add(sample[index]);
                                break;
                        }
                    }
                }
            }
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

        private void UpdateSignal(ref string? field, string? newValue)
        {
            SetProperty(ref field, newValue);
            StopReadSelectSignal = false;
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
