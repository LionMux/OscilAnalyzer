using COMTRADE_parser;
using COMTRADE_parser.MathMetod;
using ComtradeParser;
using Prism.Commands;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using COMTRADE_parser;

namespace OscilAnalyzer
{
    class AnalizeOscillogramViewModel
    {
        private List<double> _currentA;
        private List<double> _currentB;
        private List<double> _currentC;
        private List<double> _voltageA;
        private List<double> _voltageB;
        private List<double> _voltageC;
        private List<Complex> _fourieIA;
        private List<Complex> _fourieIB;
        private List<Complex> _fourieIC;
        private List<Complex> _fourieUA;
        private List<Complex> _fourieUB;
        private List<Complex> _fourieUC;
        private long _numOfPoints;
        private int _numOfPer;
        private bool _fourieAnalizeF;

        private readonly SignalDataService _signalDataService;
        public Reader _reader;
        private FourieAnalizer _fourieAnalizerI;
        private FourieAnalizer _fourieAnalizerU;
        private TypeOfFaultAnalizer _typeOfFaultAnalizer;
        IContainerRegistry containerRegistry;

        public List<Complex> FourieIA { get => _fourieIA; set => _fourieIA = value; }
        public List<Complex> FourieIB { get => _fourieIB; set => _fourieIB = value; }
        public List<Complex> FourieIC { get => _fourieIC; set => _fourieIC = value; }
        public List<Complex> FourieUA { get => _fourieUA; set => _fourieUA = value; }
        public List<Complex> FourieUB { get => _fourieUB; set => _fourieUB = value; }
        public List<Complex> FourieUC { get => _fourieUC; set => _fourieUC = value; }

        public DelegateCommand StartAnalizeFourie { get; set; }
        public DelegateCommand StartAnalizeTypeOfFault { get; set; }

        //public AnalizeOscillogramViewModel(SignalDataService signalDataService, Reader reader)
        //{
        //    _signalDataService = signalDataService;
        //    _reader = reader;
        //}
        public AnalizeOscillogramViewModel(SignalDataService signalDataService, Reader reader)
        {
            _signalDataService = signalDataService;
            _reader = reader;
            StartAnalizeFourie = new DelegateCommand(GetFourieSignals, CanGetFourie);
            StartAnalizeTypeOfFault = new DelegateCommand(StartAnalizeTypeFault, CanStartAnalize);
        }
        public void GetFourieSignals()
        {
            FourieIA = new List<Complex>();
            FourieIB = new List<Complex>();
            FourieIC = new List<Complex>();
            FourieUA = new List<Complex>();
            FourieUB = new List<Complex>();
            FourieUC = new List<Complex>();
            _numOfPoints = _reader.Config.EndSample;

            _fourieAnalizerI = new FourieAnalizer(5000, 20, _signalDataService.CurrentA, _signalDataService.CurrentB, _signalDataService.CurrentC);
            _fourieAnalizerU = new FourieAnalizer(5000, 20, _signalDataService.VoltageA, _signalDataService.VoltageB, _signalDataService.VoltageC);
            FourieIA = _fourieAnalizerI.fourie_A.ToList();
            FourieIB = _fourieAnalizerI.fourie_B.ToList();
            FourieIC = _fourieAnalizerI.fourie_C.ToList();
            FourieUA = _fourieAnalizerU.fourie_A.ToList();
            FourieUB = _fourieAnalizerU.fourie_B.ToList();
            FourieUC = _fourieAnalizerU.fourie_C.ToList();

            _fourieAnalizerI.RunAnalize();
            _fourieAnalizerU.RunAnalize();
            _fourieAnalizeF = true;
        }

        public void StartAnalizeTypeFault()
        {
            _typeOfFaultAnalizer = new TypeOfFaultAnalizer(_fourieAnalizerI, _fourieAnalizerU);
        }

        public bool CanStartAnalize()
        {
            return _fourieAnalizeF;
        }
        public bool CanGetFourie()
        {
            return _reader != null;
        }
    }
}
