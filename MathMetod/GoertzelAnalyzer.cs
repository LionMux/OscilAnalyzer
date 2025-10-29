using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace COMTRADE_parser
{
    internal class GoertzelAnalyzer
    {
        private Complex _a = new Complex(-0.5, Math.Sqrt(3) / 2);//задаем стандартный фазовый поворот
        private Complex _aa = new Complex(-0.5, -Math.Sqrt(3) / 2);
        private Complex _imagine = new Complex(0, 1);
        double T = 0.02; //Период
        private double _requireFreq;
        private double _omega;
        double _getzelCoeff;
        private int _pofPer;
        private int _N;
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

        //private Complex[] gertzel_A { get; set; }
        //private Complex[] gertzel_B { get; set; }
        //private Complex[] gertzel_C { get; set; }

        private Complex[] _pramaya;
        private Complex[] _obratnaya;
        private Complex[] _nulevaya;
        private Complex[] _gertzelSignalA;
        private Complex[] _gertzelSignalB;
        private Complex[] _gertzelSignalC;

        public double ProgressBar;
        public double[] amplitudeA_arr { get; set; }
        public double[] amplitudeB_arr { get; set; }
        public double[] amplitudeC_arr { get; set; }
        public Complex[] Pramaya { get => _pramaya; set => _pramaya = value; }
        public Complex[] Obratnaya { get => _obratnaya; set => _obratnaya = value; }
        public Complex[] Nulevaya { get => _nulevaya; set => _nulevaya = value; }
        public Complex[] GertzelSignalA { get => _gertzelSignalA; set => _gertzelSignalA = value; }
        public Complex[] GertzelSignalB { get => _gertzelSignalB; set => _gertzelSignalB = value; }
        public Complex[] GertzelSignalC { get => _gertzelSignalC; set => _gertzelSignalC = value; }

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
            //_fullIteration = 3 * (_N - _pofPer) * _pofPer * _pofPer + (_N - _pofPer);
            _omega = _pofPer * _f / _N;
            _getzelCoeff = 2 * Math.Cos(2 * Math.PI / _pofPer);
            _w = Complex.Exp(-_imagine * 2 * Math.PI / _pofPer);

            Pramaya = new Complex[_N - _pofPer];
            Obratnaya = new Complex[_N - _pofPer];
            Nulevaya = new Complex[_N - _pofPer];
            GertzelSignalA = new Complex[_N - _pofPer];
            GertzelSignalB = new Complex[_N - _pofPer];
            GertzelSignalC = new Complex[_N - _pofPer];
        }

        public void RunAnalize()
        {
            GertzelSignalA = GertzelAlgoritm(_signalA);
            GertzelSignalB = GertzelAlgoritm(_signalB);
            GertzelSignalC = GertzelAlgoritm(_signalC);

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
                    _currentIteration++;
                    _reportProgress?.Invoke(progress);
                }
                GertzelSignal[i] = sn[_pofPer - 1] - _w * sn[_pofPer - 2];
            }

            return GertzelSignal;
        }
    }

}
