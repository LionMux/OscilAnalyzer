using System;
using System.ComponentModel;

public class SignalDataService : INotifyPropertyChanged
{
    private List<double> _currentA;
    private List<double> _currentB;
    private List<double> _currentC;
    private List<double> _voltageA;
    private List<double> _voltageB;
    private List<double> _voltageC;
    private List<double> _timeValues;

    public List<double> CurrentA { get => _currentA; set => _currentA = value; }
    public List<double> CurrentB { get => _currentB; set => _currentB = value; }
    public List<double> CurrentC { get => _currentC; set => _currentC = value; }
    public List<double> VoltageA { get => _voltageA; set => _voltageA = value; }
    public List<double> VoltageB { get => _voltageB; set => _voltageB = value; }
    public List<double> VoltageC { get => _voltageC; set => _voltageC = value; }
    public List<double> TimeValues { get => _timeValues; set => _timeValues = value; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
