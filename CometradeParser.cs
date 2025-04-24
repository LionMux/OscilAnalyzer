using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using ScottPlot;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System;
using COMTRADE_parser;

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

        private string _cfgFileName = "Files\\25_newRTDS.cfg";
        private string _datFileName = "Files\\25_newRTDS.dat";

        Reader reader;
        List<AnalogChannelConfig> _analogChanells;


        public List<double> CurrentA { get => _currentA; set => SetProperty(ref _currentA, value); }
        public List<double> CurrentB { get => _currentB; set => SetProperty(ref _currentB, value); }
        public List<double> CurrentC { get => _currentC; set => SetProperty(ref _currentC, value); }
        public List<double> VoltageA { get => _voltageA; set => SetProperty(ref _voltageA, value); }
        public List<double> VoltageB { get => _voltageB; set => SetProperty(ref _voltageB, value); }
        public List<double> VoltageC { get => _voltageC; set => SetProperty(ref _voltageC, value); }
        public List<double> TimeValues { get => _timeValues; set => SetProperty(ref _timeValues, value); }
        public List<string> SignalNames { get => _signalNames; set => SetProperty(ref _signalNames, value); }

        public CometradeParser()
        {
            
        }

        public void ReadSignal()
        {
            int j = 0;
            CurrentA = new List<double>();
            CurrentB = new List<double>();
            CurrentC = new List<double>();
            VoltageA = new List<double>();
            VoltageB = new List<double>();
            VoltageC = new List<double>();
            TimeValues = new List<double>();

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
            ScottPlotControls.Clear();
            ScottPlots.Clear();

            //заполнение фаз токов и напряжений

            foreach (var sample in reader.AnalogData)
            {
                for (int i = 0; i < reader.Config.AnalogChannelsCount; i++)
                {
                    if (SignalNames[j] == _analogChanells[i].Name)
                    {
                        SignalName[j]
                    }
                }
            }

        }
    }
    public enum SignalName
    {
        _currentA = 0,
        _currentB = 1,
        _currentC = 2,
        _voltageA = 3,
        _voltageB = 4,
        _voltageC = 5
    }
}
