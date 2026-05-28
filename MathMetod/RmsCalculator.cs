using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace COMTRADE_parser
{
    internal class RmsCalculator
    {
        private readonly int _pOfPer;
        private readonly int _N;
        private double[] _rmsSignal;

        public RmsCalculator(int N, double pOfPer)
        {
            _pOfPer = (int)pOfPer;
            _N = N;
        }

        public double[] RmsCalculate(List<double> signal)
        {
            int resultLength = _N - _pOfPer + 1;
            _rmsSignal = new double[resultLength];

            for (int i = 0; i < resultLength; i++)
            {
                double sumSquareValue = 0;
                for (int m = i; m < i + _pOfPer; m++)
                {
                    sumSquareValue += signal[m] * signal[m];
                }
                _rmsSignal[i] = Math.Sqrt(sumSquareValue / _pOfPer);
            }

            return _rmsSignal;
        }

        public double[] RmsCalculateForComplex(IEnumerable<Complex> signal)
        {
            var arr = signal.ToArray();
            _rmsSignal = new double[arr.Length];

            for (int i = 0; i < arr.Length; i++)
            {
                _rmsSignal[i] = arr[i].Magnitude / Math.Sqrt(2);
            }
            return _rmsSignal;
        }
    }
}
