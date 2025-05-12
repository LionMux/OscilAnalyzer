using System;
using Prism.Commands;
using System.ComponentModel;
using Prism.Mvvm;
namespace OscilAnalyzer
{
    public class SignalDataService : BindableBase
    {
        private List<double> _currentA;
        private List<double> _currentB;
        private List<double> _currentC;
        private List<double> _voltageA;
        private List<double> _voltageB;
        private List<double> _voltageC;
        private List<double> _timeValues;

        public SignalDataService()
        {
            CurrentA = new List<double>();
            CurrentB = new List<double>();
            CurrentC = new List<double>();
            VoltageA = new List<double>();
            VoltageB = new List<double>();
            VoltageC = new List<double>();
            TimeValues = new List<double>();
        }
        public List<double> CurrentA { get => _currentA; set => SetProperty(ref _currentA, value); }
        public List<double> CurrentB { get => _currentB; set => SetProperty(ref _currentB, value); }
        public List<double> CurrentC { get => _currentC; set => SetProperty(ref _currentC, value); }
        public List<double> VoltageA { get => _voltageA; set => SetProperty(ref _voltageA, value); }
        public List<double> VoltageB { get => _voltageB; set => SetProperty(ref _voltageB, value); }
        public List<double> VoltageC { get => _voltageC; set => SetProperty(ref _voltageC, value); }
        public List<double> TimeValues { get => _timeValues; set => SetProperty(ref _timeValues, value); }
    }
}
