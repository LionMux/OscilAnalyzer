using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace COMTRADE_parser
{
    internal class RmsCalculator
    {
        private int _pOfPer;
        private int _N;
        private double _sumSquareValue;
        private double[] _rmsSignal;
        //private List<double> _naturSignal;
        //private double[] _rmsSignalWindow;

        public RmsCalculator(int N, double pOfPer)
        {
            _pOfPer = (int)pOfPer;
            _N = N;
        }

        public double[] RmsCalculate(List<double> signal)
        {
            _rmsSignal = new double[_N - _pOfPer];
            for (int i = 0; i < _N - _pOfPer; i++)
            {
                for (int n = 0; n < _pOfPer; n++)
                {
                    _sumSquareValue = 0;
                    //_rmsSignalWindow[n] = 0;
                    for (int m = i; m < i + _pOfPer; m++)
                    {
                        _sumSquareValue += signal[m] * signal[m];
                    }
                }
                _rmsSignal[i] = Math.Sqrt(_sumSquareValue / _pOfPer);
            }

            return _rmsSignal;
        }

        public double[] RmsCalculateForComplex(IEnumerable<Complex> signal)
        {
            var arr = signal.ToArray();
            _rmsSignal = new double[_N - _pOfPer];

            for (int i = 0; i < signal.ToArray().Length; i++)
            {
                _rmsSignal[i] = arr[i].Magnitude/Math.Sqrt(2);
            }
            return _rmsSignal;
        }
    }
}
