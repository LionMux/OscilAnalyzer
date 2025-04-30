using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using ScottPlot;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System;
using COMTRADE_parser;
using Prism.Mvvm;

namespace OscilAnalyzer
{
    internal class CometradeParser : BindableBase
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

        public DelegateCommand StartRead { get; set; }
        public DelegateCommand SelectSignal { get; set; }

        private string _cfgFileName = "C:\\Users\\muhac\\Desktop\\Diplom\\Files\\25_newRTDS.cfg";
        private string _datFileName = "C:\\Users\\muhac\\Desktop\\Diplom\\Files\\25_newRTDS.dat";

        Reader reader;
        List<AnalogChannelConfig> _analogChanells;

        public List<string> SignalALLNames { get => _signalALLNames; set => SetProperty(ref _signalALLNames, value); }
        public string CurrentAName { get => _currentAName; 
            set => UpdateSignal(ref _currentAName, value); }
        public string CurrentBName { get => _currentBName; set => UpdateSignal(ref _currentBName, value); }
        public string CurrentCName { get => _currentCName; set => UpdateSignal(ref _currentCName, value); }
        public string VoltageAName { get => _voltageAName; set => UpdateSignal(ref _voltageAName, value); }
        public string VoltageBName { get => _voltageBName; set => UpdateSignal(ref _voltageBName, value); }
        public string VoltageCName { get => _voltageCName; set => UpdateSignal(ref _voltageCName, value); }
        public double NumOfPoints { get => _numOfPoints; set => _numOfPoints = value; }
        public List<string> SignalNames { get => _signalNames; set => _signalNames = value; }

        public CometradeParser()
        {
            SignalALLNames = new List<string>();
            SignalNames = new List<string>();
            StartRead = new DelegateCommand(ReadSignal);
            SelectSignal = new DelegateCommand(SelectPhaseSignal, CanReadSignal);
        }

        public void ReadSignal()
        {
            _currentA = new List<double>();
            _currentB = new List<double>();
            _currentC = new List<double>();
            _voltageA = new List<double>();
            _voltageB = new List<double>();
            _voltageC = new List<double>();
            _timeValues = new List<double>();

            reader = new Reader(_cfgFileName, _datFileName);
            _analogChanells = reader.Config.AnalogChannels;
            SignalALLNames = _analogChanells.Select(x => x.Name).ToList();

            // Очистка старых данных
            _currentA.Clear();
            _currentB.Clear();
            _currentC.Clear();
            _voltageA.Clear();
            _voltageB.Clear();
            _voltageC.Clear();
            _timeValues.Clear();
            //ScottPlotControls.Clear();
            //ScottPlots.Clear();
            // Перевод из микросек. в миллисек.
            //for (int i = 0; i < reader.DatTime.Count; i++)
            //{
            //    _timeValues[i] = reader.DatTime[i]/1000;
            //}
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
                if (SignalALLNames.Count > 0)
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
        }
        private bool CanReadSignal()
        {
            return !string.IsNullOrEmpty(CurrentAName)
            && !string.IsNullOrEmpty(CurrentBName)
            && !string.IsNullOrEmpty(CurrentCName)
            && !string.IsNullOrEmpty(VoltageAName)
            && !string.IsNullOrEmpty(VoltageBName)
            && !string.IsNullOrEmpty(VoltageCName);
        }
        private void UpdateSignal(ref string field, string newValue)
        {
            SetProperty(ref field, newValue);
            SelectSignal.RaiseCanExecuteChanged();
        }
    }
}
