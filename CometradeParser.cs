using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using OxyPlot;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System;

namespace OscilAnalyzer
{
    internal class CometradeParser
    {
        private List<double> _currentA;
        private List<double> _currentB;
        private List<double> _currentC;
        private List<double> _voltageA;
        private List<double> _voltageB;
        private List<double> _voltageC;
        private List<double> _timeValue;
        private double numOfPoints;

        private string _cfgFileName = "Files\\25_newRTDS.cfg";
        private string _datFileName = "Files\\25_newRTDS.dat";

        public List<double> CurrentA { get => _currentA; set => _currentA = value; }
        public List<double> CurrentB { get => _currentB; set => _currentB = value; }
        public List<double> CurrentC { get => _currentC; set => _currentC = value; }
        public List<double> VoltageA { get => _voltageA; set => _voltageA = value; }
        public List<double> VoltageB { get => _voltageB; set => _voltageB = value; }
        public List<double> VoltageC { get => _voltageC; set => _voltageC = value; }

        public CometradeParser()
        {

        }

        public void ReadSignal()
        {

        }
    }
}
