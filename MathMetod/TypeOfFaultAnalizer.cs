using Prism.Mvvm;
using System.Numerics;

namespace COMTRADE_parser
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
        private bool _existFault;

        private int _k3count;
        private int _kab2count;
        private int _kbc2count;
        private int _kca2count;
        private int _ka1count;
        private int _kb1count;
        private int _kc1count;
        private int _kab11count;
        private int _kbc11count;
        private int _kca11count;
        private int _poOfPer;

        private FourieAnalizer _fourieAnalizer_I;
        private FourieAnalizer _fourieAnalizer_U;

        private Complex[] current_A;
        private Complex[] current_B;
        private Complex[] current_C;
        private Complex[] voltage_A;
        private Complex[] voltage_B;
        private Complex[] voltage_C;
        private double[] z_A;
        private double[] z_B;
        private double[] z_C;

        public bool K3 { get => _k3; set => _k3 = value; }
        public bool Kab2 { get => _kab2; set => _kab2 = value; }
        public bool Kbc2 { get => _kbc2; set => _kbc2 = value; }
        public bool Kca2 { get => _kca2; set => _kca2 = value; }
        public bool Ka1 { get => _ka1; set => _ka1 = value; }
        public bool Kb1 { get => _kb1; set => _kb1 = value; }
        public bool Kc1 { get => _kc1; set => _kc1 = value; }
        public bool Kab11 { get => _kab11; set => _kab11 = value; }
        public bool Kbc11 { get => _kbc11; set => _kbc11 = value; }
        public bool Kca11 { get => _kca11; set => _kca11 = value; }

        public TypeOfFaultAnalizer(FourieAnalizer fourieAnalizer_I, FourieAnalizer fourieAnalizer_U, int poOfPer)
        {
            K3 = false;
            Kab2 = false;
            Kbc2 = false;
            Kca2 = false;
            Ka1 = false;
            Kb1 = false;
            Kc1 = false;
            Kab11 = false;
            Kbc11 = false;
            Kca11 = false;
            _poOfPer = poOfPer;
            _fourieAnalizer_I = fourieAnalizer_I;
            _fourieAnalizer_U = fourieAnalizer_U;

            z_A = new double[fourieAnalizer_I.FourieSignalA.Length];
            z_B = new double[fourieAnalizer_I.FourieSignalB.Length];
            z_C = new double[fourieAnalizer_I.FourieSignalC.Length];

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
            ResetFlags();
            for (int i = 0; i < _fourieAnalizer_I.Pramaya.Length; i++)
            {
                Complex relationPramayaUandI = _fourieAnalizer_U.Pramaya[i] / _fourieAnalizer_I.Pramaya[i];
                double relationabsIObratAndPram = _fourieAnalizer_I.Obratnaya[i].Magnitude / _fourieAnalizer_I.Pramaya[i].Magnitude;
                if (_fourieAnalizer_I.Nulevaya[i].Magnitude / _fourieAnalizer_I.Obratnaya[i].Magnitude < 0.1)
                {
                    TESTFLT2P(i);
                    TESTFLT3P(i, relationPramayaUandI, relationabsIObratAndPram);
                }
                else
                {
                    TESTFLTOnGround(i, relationPramayaUandI);
                }
            }
        }

        public void CalculateImpedance()
        {
            for (int i = 0; i < current_A.Length; i++)
            {
                if (current_A[i].Magnitude == 0)
                {
                    z_A[i] = 10 ^ 8;
                }
                else if (current_B[i].Magnitude == 0)
                {
                    z_B[i] = 10 ^ 8;
                }
                else if (current_C[i].Magnitude == 0)
                {
                    z_C[i] = 10 ^ 8;
                }
                else
                {
                    z_A[i] = voltage_A[i].Magnitude / current_A[i].Magnitude;
                    z_B[i] = voltage_B[i].Magnitude / current_B[i].Magnitude;
                    z_C[i] = voltage_C[i].Magnitude / current_C[i].Magnitude;
                }
            }
        }

        public void TESTFLT2P(int i)
        {
            bool conditionForKab2 = 0.5 * z_C[i] > z_A[i] && 0.5 * z_C[i] > z_B[i];
            bool conditionForKbc2 = 0.5 * z_A[i] > z_B[i] && 0.5 * z_A[i] > z_C[i];
            bool conditionForKca2 = 0.5 * z_B[i] > z_A[i] && 0.5 * z_B[i] > z_C[i];

            CheckFaultCondition(conditionForKab2, ref _kab2count, ref _kab2);
            CheckFaultCondition(conditionForKbc2, ref _kbc2count, ref _kbc2);
            CheckFaultCondition(conditionForKca2, ref _kca2count, ref _kca2);
        }
        public void TESTFLT3P(int i, Complex relationPramayaUandI, double relationabsIObratAndPram)
        {
            CheckFaultCondition(Math.Abs(relationabsIObratAndPram) < 0.2 &&
                Math.Abs(relationPramayaUandI.Phase) > ConvertToRadian(30),
                ref _k3count, ref _k3);
        }
        public void TESTFLTOnGround(int i, Complex relationPramayaUandI)
        {
            bool conditionForKa1 = 2 * z_A[i] > z_B[i] && 2 * z_A[i] > z_C[i];
            bool conditionForKb1 = 2 * z_B[i] > z_A[i] && 2 * z_B[i] > z_C[i];
            bool conditionForKc1 = 2 * z_C[i] > z_A[i] && 2 * z_C[i] > z_B[i];
            bool conditionForKab11 = 0.5 * z_C[i] > z_A[i] && 0.5 * z_C[i] > z_B[i];
            bool conditionForKbc11 = 0.5 * z_A[i] > z_B[i] && 0.5 * z_A[i] > z_C[i];
            bool conditionForKca11 = 0.5 * z_B[i] > z_A[i] && 0.5 * z_B[i] > z_C[i];

            if (relationPramayaUandI.Phase > ConvertToRadian(-60) && relationPramayaUandI.Phase < ConvertToRadian(60))
            {
                CheckFaultCondition(conditionForKa1, ref _ka1count, ref _ka1);
                CheckFaultCondition(conditionForKbc11, ref _kbc11count, ref _kbc11);
            }
            else if (relationPramayaUandI.Phase > ConvertToRadian(60) && relationPramayaUandI.Phase < ConvertToRadian(180))
            {
                CheckFaultCondition(conditionForKc1, ref _kc1count, ref _kc1);
                CheckFaultCondition(conditionForKab11, ref _kab11count, ref _kab11);
            }
            else if (relationPramayaUandI.Phase > ConvertToRadian(-180) && relationPramayaUandI.Phase < ConvertToRadian(-60))
            {
                CheckFaultCondition(conditionForKb1, ref _kb1count, ref _kb1);
                CheckFaultCondition(conditionForKca11, ref _kca11count, ref _kca11);
            }
        }
        private void CheckFaultCondition(bool condition, ref int counter, ref bool faultFlag)
        {
            if (condition)
            {
                counter++;
                if (counter >= _poOfPer)
                {
                    faultFlag = true;
                }
            }
            else
            {
                counter = 0;
            }
        }

        private double ConvertToRadian(double deg)
        {
            return deg * Math.PI / 180;
        }
        private void ResetCounters()
        {
            _k3count = 0;
            _kab2count = 0;
            _kbc2count = 0;
            _kca2count = 0;
            _ka1count = 0;
            _kb1count = 0;
            _kc1count = 0;
            _kab11count = 0;
            _kbc11count = 0;
            _kca11count = 0;
        }
        private void ResetFlags()
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
        }
    }
}
