//namespace COMTRADE_parser.MathMetod
//{
//    internal class SymmetricComponents
//    {
//        private readonly List<AnalogChannelConfig> _cfgChannels;
//        private readonly List<List<double>> _datSamples;
//        private List<int> _phaseIndicesA = new List<int>();
//        private List<int> _phaseIndicesB = new List<int>();
//        private List<int> _phaseIndicesC = new List<int>();

//        public SymmetricComponents(List<AnalogChannelConfig> cfgChannels, List<List<double>> datSamples)
//        {
//            _cfgChannels = cfgChannels;
//            _datSamples = datSamples;
//            FindPhaseIndices();
//        }

//        public List<List<double>> CalculateI0()
//        {
//            List<List<double>> I0 = new List<List<double>>();
//            if (_phaseIndicesA.Count != _phaseIndicesB.Count || _phaseIndicesA.Count != _phaseIndicesB.Count)
//            {
//                throw new ArgumentException("Недостаточно значений для расчета I0");
//            }
//            // Проходим по всем временным точкам (сэмплам)
//            for (int i = 0; i < _phaseIndicesA.Count; i++)
//            {
//                double sum = 0;
//                List<double> sampleI0 = new List<double>();
//                int sampleA = _phaseIndicesA[i];
//                int sampleB = _phaseIndicesB[i];
//                int sampleC = _phaseIndicesC[i];
//                foreach (var sample in _datSamples)
//                {
//                    sum = sample[sampleA] + sample[sampleB] + sample[sampleC];
//                    sampleI0.Add(sum / 3.0);
//                }
//                I0.Add(sampleI0);
//            }

//            return I0;
//        }

//        private void FindPhaseIndices()
//        {
//            for (int i = 0; i < _cfgChannels.Count; i++)
//            {
//                var channel = _cfgChannels[i];
//                if (channel.Name.ToUpper().Contains("IA"))
//                {
//                    _phaseIndicesA.Add(i);
//                }
//                else if (channel.Name.ToUpper().Contains("IB"))
//                {
//                    _phaseIndicesB.Add(i);
//                }
//                else if (channel.Name.ToUpper().Contains("IC"))
//                {
//                    _phaseIndicesC.Add(i);
//                }
//            }
//        }
//    }
//}