using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace COMTRADE_parser
{
    internal class SymmetricalComponentsCalculator
    {
        private const double A1_3 = (double)1 / 3;
        private Complex _a = new Complex(-0.5, Math.Sqrt(3) / 2);//задаем стандартный фазовый поворот
        private Complex _aa = new Complex(-0.5, -Math.Sqrt(3) / 2);
        private Complex _imagine = new Complex(0, 1);
        private Complex[] _pramaya;
        private Complex[] _obratnaya;
        private Complex[] _nulevaya;
        private Complex[] _processedSignalA;
        private Complex[] _processedsignalB;
        private Complex[] _processedsignalC;
        private int _N;
        private int _pofPer;

        public Complex[] Pramaya { get => _pramaya; set => _pramaya = value; }
        public Complex[] Obratnaya { get => _obratnaya; set => _obratnaya = value; }
        public Complex[] Nulevaya { get => _nulevaya; set => _nulevaya = value; }

        public SymmetricalComponentsCalculator(int N, double pOfPer, IEnumerable<Complex> ProcessedSignalA, IEnumerable<Complex> ProcessedSignalB, IEnumerable<Complex> ProcessedSignalC)
        {
            _N = N;
            _pofPer = (int)pOfPer;
            _processedSignalA = ProcessedSignalA.ToArray();
            _processedsignalB = ProcessedSignalB.ToArray();
            _processedsignalC = ProcessedSignalC.ToArray();

            Pramaya = new Complex[_N - _pofPer];
            Obratnaya = new Complex[_N - _pofPer];
            Nulevaya = new Complex[_N - _pofPer];

            GetSymmetricalComponents();
        }

        public void GetSymmetricalComponents()
        {
            for (int i = 0; i < _N - _pofPer; i++)
            {
                Pramaya[i] = A1_3 * (_processedSignalA[i].Magnitude * Complex.Exp(_imagine * _processedSignalA[i].Phase) +
                    _processedsignalB[i].Magnitude * Complex.Exp(_imagine * _processedsignalB[i].Phase) * _a +
                    _processedsignalC[i].Magnitude * Complex.Exp(_imagine * _processedsignalC[i].Phase) * _aa);

                Obratnaya[i] = A1_3 * (_processedSignalA[i].Magnitude * Complex.Exp(_imagine * _processedSignalA[i].Phase)
                    + _processedsignalB[i].Magnitude * Complex.Exp(_imagine * _processedsignalB[i].Phase) * _aa
                    + _processedsignalC[i].Magnitude * Complex.Exp(_imagine * _processedsignalC[i].Phase) * _a);

                Nulevaya[i] = A1_3 * (_processedSignalA[i].Magnitude * Complex.Exp(_imagine * _processedSignalA[i].Phase)
                    + _processedsignalB[i].Magnitude * Complex.Exp(_imagine * _processedsignalB[i].Phase)
                    + _processedsignalC[i].Magnitude * Complex.Exp(_imagine * _processedsignalC[i].Phase));
                //double progress = _currentIteration * 100 / _fullIteration;
                //_currentIteration++;
                //_reportProgress?.Invoke(progress);
            }
        }
}
}
