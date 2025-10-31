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
        private int _condForRegFault;

        private ISignalAnalizer _analizer_I;
        private ISignalAnalizer _analizer_U;
        private SymmetricalComponentsCalculator _symmetricalComponentsI;
        private SymmetricalComponentsCalculator _symmetricalComponentsU;

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

        public TypeOfFaultAnalizer(ISignalAnalizer Analizer_I, ISignalAnalizer Analizer_U,
            SymmetricalComponentsCalculator SymmetricalComponentsI, SymmetricalComponentsCalculator SymmetricalComponentsU, int condForRegFault)
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
            _analizer_I = Analizer_I;
            _analizer_U = Analizer_U;
            _symmetricalComponentsI = SymmetricalComponentsI;
            _symmetricalComponentsU = SymmetricalComponentsU;
            _condForRegFault = condForRegFault;

            z_A = new double[Analizer_I.ProcessedSignalA.Length];
            z_B = new double[Analizer_I.ProcessedSignalB.Length];
            z_C = new double[Analizer_I.ProcessedSignalC.Length];

            current_A = Analizer_I.ProcessedSignalA;
            current_B = Analizer_I.ProcessedSignalB;
            current_C = Analizer_I.ProcessedSignalC;
            voltage_A = Analizer_U.ProcessedSignalA;
            voltage_B = Analizer_U.ProcessedSignalB;
            voltage_C = Analizer_U.ProcessedSignalC;

            CalculateImpedance();
        }

        public void StartFaultAnalize()
        {
            ResetFlags();
            ResetCounters();
            for (int i = 0; i < _symmetricalComponentsI.Pramaya.Length; i++)
            {
                Complex relationPramayaUandI = _symmetricalComponentsU.Pramaya[i] / _symmetricalComponentsI.Pramaya[i];
                double relationabsIObratAndPram = _symmetricalComponentsI.Obratnaya[i].Magnitude / _symmetricalComponentsI.Pramaya[i].Magnitude;
                if (_symmetricalComponentsI.Nulevaya[i].Magnitude / _symmetricalComponentsI.Obratnaya[i].Magnitude < 0.1)
                {
                    TESTFLT2P(i);
                    TESTFLT3P(relationPramayaUandI, relationabsIObratAndPram);
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

        public void TESTFLT2P(int numPosition)
        {
            bool conditionForKab2 = 0.5 * z_C[numPosition] > z_A[numPosition] && 0.5 * z_C[numPosition] > z_B[numPosition];
            bool conditionForKbc2 = 0.5 * z_A[numPosition] > z_B[numPosition] && 0.5 * z_A[numPosition] > z_C[numPosition];
            bool conditionForKca2 = 0.5 * z_B[numPosition] > z_A[numPosition] && 0.5 * z_B[numPosition] > z_C[numPosition];

            CheckFaultCondition(conditionForKab2, ref _kab2count, ref _kab2);
            CheckFaultCondition(conditionForKbc2, ref _kbc2count, ref _kbc2);
            CheckFaultCondition(conditionForKca2, ref _kca2count, ref _kca2);
        }
        public void TESTFLT3P(Complex relationPramayaUandI, double relationabsIObratAndPram)
        {
            CheckFaultCondition(Math.Abs(relationabsIObratAndPram) < 0.2 && Math.Abs(relationPramayaUandI.Phase) > ConvertDegToRadian(30),
                                ref _k3count, ref _k3);
        }
        public void TESTFLTOnGround(int numPosition, Complex relationPramayaUandI)
        {
            bool conditionForKa1 = 2 * z_A[numPosition] < z_B[numPosition] && 2 * z_A[numPosition] < z_C[numPosition];
            bool conditionForKb1 = 2 * z_B[numPosition] < z_A[numPosition] && 2 * z_B[numPosition] < z_C[numPosition];
            bool conditionForKc1 = 2 * z_C[numPosition] < z_A[numPosition] && 2 * z_C[numPosition] < z_B[numPosition];
            bool conditionForKab11 = 0.5 * z_C[numPosition] > z_A[numPosition] && 0.5 * z_C[numPosition] > z_B[numPosition];
            bool conditionForKbc11 = 0.5 * z_A[numPosition] > z_B[numPosition] && 0.5 * z_A[numPosition] > z_C[numPosition];
            bool conditionForKca11 = 0.5 * z_B[numPosition] > z_A[numPosition] && 0.5 * z_B[numPosition] > z_C[numPosition];

            if (relationPramayaUandI.Phase > ConvertDegToRadian(-60) && relationPramayaUandI.Phase < ConvertDegToRadian(60))
            {
                CheckFaultCondition(conditionForKa1, ref _ka1count, ref _ka1);
                CheckFaultCondition(conditionForKbc11, ref _kbc11count, ref _kbc11);
            }
            else if (relationPramayaUandI.Phase > ConvertDegToRadian(60) && relationPramayaUandI.Phase < ConvertDegToRadian(180))
            {
                CheckFaultCondition(conditionForKc1, ref _kc1count, ref _kc1);
                CheckFaultCondition(conditionForKab11, ref _kab11count, ref _kab11);
            }
            else if (relationPramayaUandI.Phase > ConvertDegToRadian(-180) && relationPramayaUandI.Phase < ConvertDegToRadian(-60))
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
                if (counter >= _condForRegFault)
                {
                    faultFlag = true;
                }
            }
            else
            {
                counter = 0;
            }
        }

        private double ConvertDegToRadian(double deg)
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
