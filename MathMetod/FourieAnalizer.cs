﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace COMTRADE_parser
{
    internal class FourieAnalizer
    {
        private Complex _a = new Complex(-0.5, Math.Sqrt(3) / 2);//задаем стандартный фазовый поворот
        private Complex _aa = new Complex(-0.5, -Math.Sqrt(3) / 2);
        private Complex _imagine = new Complex(0, 1);
        double T = 0.02; //Период
        private int _pofPer;
        private int _N;
        private double[] _signal_Time;
        private double[] _signalA;
        private double[] _signalC;
        private double[] _signalB;
        private List<double> _freq_Out;
        private double _f;
        private double _w;
        private double _dt;
        private double _periods;
        private double _currentIteration;
        private double _fullIteration;
        private Action<double>? _reportProgress;

        private Complex[] fourie_A { get; set; }
        private Complex[] fourie_B { get; set; }
        private Complex[] fourie_C { get; set; }

        private Complex[] _pramaya;
        private Complex[] _obratnaya;
        private Complex[] _nulevaya;
        private Complex[] _fourieSignalA;
        private Complex[] _fourieSignalB;
        private Complex[] _fourieSignalC;

        public double ProgressBar;
        public double[] amplitudeA_arr { get; set; }
        public double[] amplitudeB_arr { get; set; }
        public double[] amplitudeC_arr { get; set; }
        public Complex[] Pramaya { get => _pramaya; set => _pramaya = value; }
        public Complex[] Obratnaya { get => _obratnaya; set => _obratnaya = value; }
        public Complex[] Nulevaya { get => _nulevaya; set => _nulevaya = value; }
        public Complex[] FourieSignalA { get => _fourieSignalA; set => _fourieSignalA = value; }
        public Complex[] FourieSignalB { get => _fourieSignalB; set => _fourieSignalB = value; }
        public Complex[] FourieSignalC { get => _fourieSignalC; set => _fourieSignalC = value; }

        public FourieAnalizer(int N, double _pOfPer, IEnumerable<double> signal_A, IEnumerable<double> signal_B, IEnumerable<double> signal_C, Action<double>? reportProgress = null)
        {
            _pofPer = (int)_pOfPer; // Число точек на период
            _N = N; // Число точек
            _signal_Time = new double[_N];
            _signalA = signal_A.ToArray();
            _signalC = signal_C.ToArray();
            _signalB = signal_B.ToArray();
            _freq_Out = new List<double>();
            _f = 1 / T;             //Частота
            _w = 2 * Math.PI * _f;        // угловая частота
            _dt = T / _pofPer;       // Шаг дискретизации
            _periods = N / _pofPer;  //число периодов 
            _reportProgress = reportProgress;
            _fullIteration = 3 * (_N - _pofPer) * _pofPer * _pofPer + (_N - _pofPer);

            Pramaya = new Complex[_N - _pofPer];
            Obratnaya = new Complex[_N - _pofPer];
            Nulevaya = new Complex[_N - _pofPer];
            FourieSignalA = new Complex[_N - _pofPer];
            FourieSignalB = new Complex[_N - _pofPer];
            FourieSignalC = new Complex[_N - _pofPer];
        }

        public void RunAnalize()
        {
            for (int i = 0; i < _N; i++)
            {
                _signal_Time[i] = i * _dt;
            }
            // По умолчанию в преобразовании 1-я гармоника - весь период, если "наша"
            // гармоника укладывается в период 10 раз - то она будет 10-й
            // Нас это не устраивает, поэтому масштабируем:

            // простое фурье через два цикла 
            fourie_A = new Complex[_pofPer];
            amplitudeA_arr = new double[(_N - _pofPer)];
            double[] angleA_arr = new double[(_N - _pofPer)];

            for (int i = 0; i < _N - _pofPer; i++)
            {

                for (int n = 0; n < _pofPer; n++)
                {
                    fourie_A[n] = 0;
                    for (int m = i; m < i + _pofPer; m++)
                    {
                        double mm = m * 2 * Math.PI * n / _pofPer;
                        double N2 = _pofPer / 2;
                        double progress = _currentIteration*100 / _fullIteration;
                        fourie_A[n] += _signalA[m] * Complex.Exp(-_imagine * mm) / N2; // делим не на N, а на N / 2, потому что вторая часть суммы приходится на "зеркало"
                        _currentIteration++;
                        _reportProgress?.Invoke(progress);
                    }
                }
                amplitudeA_arr[i] = fourie_A[1].Magnitude;
                angleA_arr[i] = fourie_A[1].Phase;// -(2 * pi / (PofPer));
                FourieSignalA[i] = Complex.FromPolarCoordinates(fourie_A[1].Magnitude, fourie_A[1].Phase);
            }
            //_________________________________________________________________________________________   
            // простое фурье через два цикла 
            fourie_B = new Complex[_pofPer];
            amplitudeB_arr = new double[(_N - _pofPer)];
            double[] angleB_arr = new double[(_N - _pofPer)];

            for (int i = 0; i < _N - _pofPer; i++)
            {
                for (int n = 0; n < _pofPer; n++)
                {
                    fourie_B[n] = 0;
                    for (int m = i; m < i + _pofPer; m++)
                    {
                        double mm = m * 2 * Math.PI * n / _pofPer;
                        double N2 = _pofPer / 2;
                        double progress = _currentIteration * 100 / _fullIteration;
                        fourie_B[n] = fourie_B[n] + _signalB[m] * Complex.Exp(-_imagine * mm) / N2; // делим не на N, а на N / 2, потому что вторая часть суммы приходится на "зеркало"
                        _currentIteration++;
                        _reportProgress?.Invoke(progress);
                    }
                }
                amplitudeB_arr[i] = fourie_B[1].Magnitude;
                angleB_arr[i] = fourie_B[1].Phase;// -(2 * pi / (PofPer));
                FourieSignalB[i] = Complex.FromPolarCoordinates(fourie_B[1].Magnitude, fourie_B[1].Phase);
            }
            //________________________________________________________________________________________________
            // простое фурье через два цикла 
            fourie_C = new Complex[_pofPer];
            amplitudeC_arr = new double[(_N -_pofPer)];
            double[] angleC_arr = new double[(_N - _pofPer)];

            for (int i = 0; i < _N - _pofPer; i++)
            {
                for (int n = 0; n < _pofPer; n++)
                {
                    fourie_C[n] = 0;
                    for (int m = i; m < i + _pofPer; m++)
                    {
                        double mm = m * 2 * Math.PI * n / _pofPer;
                        double N2 = _pofPer / 2;
                        double progress = _currentIteration * 100 / _fullIteration;
                        fourie_C[n] = fourie_C[n] + _signalC[m] * Complex.Exp(-_imagine * mm) / N2; // делим не на N, а на N / 2, потому что вторая часть суммы приходится на "зеркало"
                        _currentIteration++;
                        _reportProgress?.Invoke(progress);
                    }
                }
                amplitudeC_arr[i] = fourie_C[1].Magnitude;
                angleC_arr[i] = fourie_C[1].Phase;// -(2 * pi / (PofPer));
                FourieSignalC[i] = Complex.FromPolarCoordinates(fourie_C[1].Magnitude, fourie_C[1].Phase);
            }

            double A1_3 = (double)1 / 3;

            for (int i = 0; i < _N - _pofPer; i++)
            {
                Pramaya[i] = A1_3 * (amplitudeA_arr[i] * Complex.Exp(_imagine * angleA_arr[i]) +
                    amplitudeB_arr[i] * Complex.Exp(_imagine * angleB_arr[i]) * _a +
                    amplitudeC_arr[i] * Complex.Exp(_imagine * angleC_arr[i]) * _aa);

                Obratnaya[i] = A1_3 * (amplitudeA_arr[i] * Complex.Exp(_imagine * angleA_arr[i])
                    + amplitudeB_arr[i] * Complex.Exp(_imagine * angleB_arr[i]) * _aa
                    + amplitudeC_arr[i] * Complex.Exp(_imagine * angleC_arr[i]) * _a);

                Nulevaya[i] = A1_3 * (amplitudeA_arr[i] * Complex.Exp(_imagine * angleA_arr[i])
                    + amplitudeB_arr[i] * Complex.Exp(_imagine * angleB_arr[i])
                    + amplitudeC_arr[i] * Complex.Exp(_imagine * angleC_arr[i]));
                double progress = _currentIteration * 100 / _fullIteration;
                _currentIteration++;
                _reportProgress?.Invoke(progress);
            }
        }
    }
}

