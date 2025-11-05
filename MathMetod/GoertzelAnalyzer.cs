using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace COMTRADE_parser
{
    internal class GoertzelAnalyzer : ISignalAnalizer
    {
        private Complex _imagine = new Complex(0, 1);
        double T = 0.02; //Период
        private double _requireFreq;
        private double _omega;
        double _getzelCoeff;
        private int _pofPer;
        private int _N;
        private int _N2;
        private double[] _signal_Time;
        private double[] _signalA;
        private double[] _signalC;
        private double[] _signalB;
        private List<double> _freq_Out;
        private double _f;
        private Complex _w;
        private double _dt;
        private double _periods;
        private double _currentIteration;
        private double _fullIteration;
        private Action<double>? _reportProgress;
        private Complex[] _gertzelSignalA;
        private Complex[] _gertzelSignalB;
        private Complex[] _gertzelSignalC;

        public double ProgressBar;
        public double[] amplitudeA_arr { get; set; }
        public double[] amplitudeB_arr { get; set; }
        public double[] amplitudeC_arr { get; set; }
        public Complex[] ProcessedSignalA { get => _gertzelSignalA; set => _gertzelSignalA = value; }
        public Complex[] ProcessedSignalB { get => _gertzelSignalB; set => _gertzelSignalB = value; }
        public Complex[] ProcessedSignalC { get => _gertzelSignalC; set => _gertzelSignalC = value; }

        public GoertzelAnalyzer(int N, double _pOfPer, IEnumerable<double> signal_A, IEnumerable<double> signal_B, IEnumerable<double> signal_C, Action<double>? reportProgress = null)
        {
            _pofPer = (int)_pOfPer; // Число точек на период
            _N = N; // Число точек
            _signal_Time = new double[_N];
            _signalA = signal_A.ToArray();
            _signalC = signal_C.ToArray();
            _signalB = signal_B.ToArray();
            _freq_Out = new List<double>();
            _f = 1 / T;             //Частота
            _dt = T / _pofPer;       // Шаг дискретизации
            _periods = N / _pofPer;  //число периодов 
            _reportProgress = reportProgress;
            _fullIteration = 3 * (_N - _pofPer) * _pofPer + (_N - _pofPer);
            _omega = _pofPer * _f / _N;
            _getzelCoeff = 2 * Math.Cos(2 * Math.PI / _pofPer);
            _w = Complex.Exp(-_imagine * 2 * Math.PI / _pofPer);
            _N2 = _pofPer / 2;

            ProcessedSignalA = new Complex[_N - _pofPer];
            ProcessedSignalB = new Complex[_N - _pofPer];
            ProcessedSignalC = new Complex[_N - _pofPer];

            RunAnalize();
        }

        private void RunAnalize()
        {
            ProcessedSignalA = GertzelAlgoritm(_signalA);
            ProcessedSignalB = GertzelAlgoritm(_signalB);
            ProcessedSignalC = GertzelAlgoritm(_signalC);
        }

        private Complex[] GertzelAlgoritm(double[] signal)
        {
            double[] sn;
            Complex[] GertzelSignal = new Complex[_N - _pofPer];

            for (int i = 0; i < _N - _pofPer; i++)
            {
                sn = new double[_pofPer];
                for (int n = 0; n < _pofPer; n++)
                {
                    int signalIndex = i + n;

                    if (n == 0)
                    {
                        sn[n] = signal[signalIndex];
                    }
                    else if (n == 1)
                    {
                        sn[n] = signal[signalIndex] + _getzelCoeff * sn[n - 1];
                    }
                    else
                    {
                        sn[n] = signal[signalIndex] + _getzelCoeff * sn[n - 1] - sn[n - 2];
                    }
                    double progress = _currentIteration * 100 / _fullIteration;
                    _currentIteration++;
                    _reportProgress?.Invoke(progress);
                }
                GertzelSignal[i] =  ( sn[_pofPer - 1] - _w * sn[_pofPer - 2] ) / _N2;
            }

            return GertzelSignal;
        }
    }

}
