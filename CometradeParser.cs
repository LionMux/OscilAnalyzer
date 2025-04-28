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
    internal class CometradeParser: BindableBase
    {
        private List<double> _currentA;
        private List<double> _currentB;
        private List<double> _currentC;
        private List<double> _voltageA;
        private List<double> _voltageB;
        private List<double> _voltageC;
        private List<double> _timeValues;
        private List<string> _signalNames;
        private double _numOfPoints;

        public DelegateCommand StartRead { get; set; }

        private string _cfgFileName = "Files\\25_newRTDS.cfg";
        private string _datFileName = "Files\\25_newRTDS.dat";

        Reader reader;
        List<AnalogChannelConfig> _analogChanells;

        public List<string> SignalNames { get => _signalNames; set => SetProperty(ref _signalNames, value); }

        public CometradeParser()
        {
            SignalNames = new List<string>();
            StartRead = new DelegateCommand(ReadSignal, CanReadSignal);
        }

        public void ReadSignal()
        {
            int j = 0;
            _currentA = new List<double>();
            _currentB = new List<double>();
            _currentC = new List<double>();
            _voltageA = new List<double>();
            _voltageB = new List<double>();
            _voltageC = new List<double>();
            _timeValues = new List<double>();

            Reader reader = new Reader(_cfgFileName, _datFileName);
            _analogChanells = reader.Config.AnalogChannels;
            SignalNames = _analogChanells.Select(x => x.Name).ToList();

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

            //заполнение фаз токов и напряжений
            foreach (var signalName in SignalNames)
            {
                int index = _analogChanells.FindIndex(x => x.Name == signalName);
                if (index >= 0)
                {
                    foreach (var sample in reader.AnalogData)
                    {
                        if(_timeValues.Count != reader.AnalogData.Count)
                        switch (index)
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
            }
            // Перевод из микросек. в миллисек.
            for (int i = 0; i < reader.DatTime.Count; i++)
            {
                _timeValues[i] = reader.DatTime[i]/1000;
            }
        }

        private bool CanReadSignal()
        {
            if (SignalNames.Count < 6)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
