
using ComtradeParser;
using System.Numerics;

namespace COMTRADE_parser.MathMetod
{
    internal class TypeOfFaultAnalizer
    {
        private bool _k3;
        private bool _kab2;
        private bool _kbc2;
        private bool _kca2;
        private bool _ka1;
        private bool _kb1;
        private bool _kc1;
        private bool _kab11;
        private bool _kbc11;
        private bool _kca11;

        private FourieAnalizer _fourieAnalizer_I;
        private FourieAnalizer _fourieAnalizer_U;

        private Complex[] current_A;
        private Complex[] current_B;
        private Complex[] current_C;
        private Complex[] voltage_A;
        private Complex[] voltage_B;
        private Complex[] voltage_C;
        private Complex[] z_A;
        private Complex[] z_B;
        private Complex[] z_C;

        public TypeOfFaultAnalizer(FourieAnalizer fourieAnalizer_I, FourieAnalizer fourieAnalizer_U)
        {
            _k3 = false;
            _kab2 = false;
            _kbc2 = false;
            _kca2 = false;
            _ka1 = false;
            _kb1 = false;
            _kc1 = false;
            _kab11 = false;
            _kbc11 = false;
            _kca11 = false;

            _fourieAnalizer_I = fourieAnalizer_I;
            _fourieAnalizer_U = fourieAnalizer_U;

            current_A = fourieAnalizer_I.FourieSignalA;
            current_B = fourieAnalizer_I.FourieSignalB;
            current_C = fourieAnalizer_I.FourieSignalC;
            voltage_A = fourieAnalizer_U.FourieSignalA;
            voltage_B = fourieAnalizer_U.FourieSignalB;
            voltage_C = fourieAnalizer_U.FourieSignalC;

            CalculateImpedance();
        }

        public void StartFaultAnalize()
        {
            for (int i = 0; i < _fourieAnalizer_I.Pramaya.Length; i++)
            {
                if ((_fourieAnalizer_I.Nulevaya[i].Magnitude / _fourieAnalizer_I.Obratnaya[i].Magnitude) < 0.1)
                {
                    TESTFLT2P(i);
                    TESTFLT3P(i);
                }
                TESTFLTOnGround(i);
            }
        }

        public void CalculateImpedance()
        {
            for (int i = 0; i < current_A.Length; i++)
            {
                z_A[i] = voltage_A[i] / current_A[i];
                z_B[i] = voltage_B[i] / current_B[i];
                z_C[i] = voltage_C[i] / current_C[i];
            }
        }

        public void TESTFLT2P(int i)
        {

            if (0.5 * z_C[i].Magnitude > z_A[i].Magnitude && 0.5 * z_C[i].Magnitude > z_B[i].Magnitude)
            {
                _kab2 = true;
            }
            else if (0.5 * z_A[i].Magnitude > z_B[i].Magnitude && 0.5 * z_A[i].Magnitude > z_C[i].Magnitude)
            {
                _kbc2 = true;
            }
            else if (0.5 * z_B[i].Magnitude > z_A[i].Magnitude && 0.5 * z_B[i].Magnitude > z_C[i].Magnitude)
            {
                _kca2 = true;
            }
        }
        public void TESTFLT3P(int i)
        {

            if ((_fourieAnalizer_I.Nulevaya[i].Magnitude / _fourieAnalizer_I.Obratnaya[i].Magnitude) < 0.1 &&
                Math.Abs((_fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i]).Phase) > (30 * Math.PI / 180))
            {
                _k3 = true;
            }

        }
        public void TESTFLTOnGround(int i)
        {
            if ((_fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i]).Phase > (-60 * Math.PI / 180) &&
                    (_fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i]).Phase < (60 * Math.PI / 180))
            {
                if (2 * z_A[i].Magnitude > z_B[i].Magnitude && 2 * z_A[i].Magnitude > z_C[i].Magnitude)
                {
                    _ka1 = true;
                }
                if (0.5 * z_A[i].Magnitude > z_B[i].Magnitude && 0.5 * z_A[i].Magnitude > z_C[i].Magnitude)
                {
                    _kbc11 = true;
                }
            }
            else if ((_fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i]).Phase > (60 * Math.PI / 180) &&
                (_fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i]).Phase < (180 * Math.PI / 180))
            {
                if (2 * z_C[i].Magnitude > z_A[i].Magnitude && 2 * z_C[i].Magnitude > z_B[i].Magnitude)
                {
                    _kc1 = true;
                }
                if (0.5 * z_C[i].Magnitude > z_A[i].Magnitude && 0.5 * z_C[i].Magnitude > z_B[i].Magnitude)
                {
                    _kab11 = true;
                }
            }
            else if ((_fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i]).Phase > (-180 * Math.PI / 180) &&
                (_fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i]).Phase < (-60 * Math.PI / 180))
            {
                if (2 * z_B[i].Magnitude > z_A[i].Magnitude && 2 * z_B[i].Magnitude > z_C[i].Magnitude)
                {
                    _kb1 = true;
                }
                if (0.5 * z_B[i].Magnitude > z_A[i].Magnitude && 0.5 * z_B[i].Magnitude > z_C[i].Magnitude)
                {
                    _kca11 = true;
                }
            }
        }
    }
}
