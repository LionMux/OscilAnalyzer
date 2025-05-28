using System;
using Prism.Commands;
using System.ComponentModel;
using Prism.Mvvm;
namespace OscilAnalyzer
{
    public class SignalDataService : BindableBase
    {
        private int _numOfPoints;
        private int _poOfPer;
        private List<double> _currentA;
        private List<double> _currentB;
        private List<double> _currentC;
        private List<double> _voltageA;
        private List<double> _voltageB;
        private List<double> _voltageC;
        private List<double> _timeValues;

        public SignalDataService()
        {
            _currentA = new List<double>();
            _currentB = new List<double>();
            _currentC = new List<double>();
            _voltageA = new List<double>();
            _voltageB = new List<double>();
            _voltageC = new List<double>();
            _timeValues = new List<double>();
        }

        public int PoOfPer { get => _poOfPer; set => SetProperty(ref _poOfPer, value); }
        public int NumOfPoints { get => _numOfPoints; set => SetProperty(ref _numOfPoints, value); }
        public List<double> CurrentA { get => _currentA; set => SetProperty(ref _currentA, value); }
        public List<double> CurrentB { get => _currentB; set => SetProperty(ref _currentB, value); }
        public List<double> CurrentC { get => _currentC; set => SetProperty(ref _currentC, value); }
        public List<double> VoltageA { get => _voltageA; set => SetProperty(ref _voltageA, value); }
        public List<double> VoltageB { get => _voltageB; set => SetProperty(ref _voltageB, value); }
        public List<double> VoltageC { get => _voltageC; set => SetProperty(ref _voltageC, value); }
        public List<double> TimeValues { get => _timeValues; set => SetProperty(ref _timeValues, value); }
    }
}
