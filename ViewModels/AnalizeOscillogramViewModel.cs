using COMTRADE_parser;
using COMTRADE_parser.MathMetod;
using ComtradeParser;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OscilAnalyzer
{
    public class AnalizeOscillogramViewModel : BindableBase
    {
        private List<Complex> _fourieIA;
        private List<Complex> _fourieIB;
        private List<Complex> _fourieIC;
        private List<Complex> _fourieUA;
        private List<Complex> _fourieUB;
        private List<Complex> _fourieUC;
        private const int _windowMS = 20; // Окно измерения
        private int _totalTimeMS;
        private int _timeForVD;
        private int _numOfPoints;
        private int _numOfPointsForVD;
        private int _numOfPer;
        private bool _fourieAnalizeF;

        private readonly IRegionManager _regionManager;
        private readonly SignalDataService _signalDataService;
        private FourieAnalizer _fourieAnalizerI;
        private FourieAnalizer _fourieAnalizerU;
        private TypeOfFaultAnalizer _typeOfFaultAnalizer;
        private VectorPlotter _vectrorPlotter;

        public List<Complex> FourieIA { get => _fourieIA; set => _fourieIA = value; }
        public List<Complex> FourieIB { get => _fourieIB; set => _fourieIB = value; }
        public List<Complex> FourieIC { get => _fourieIC; set => _fourieIC = value; }
        public List<Complex> FourieUA { get => _fourieUA; set => _fourieUA = value; }
        public List<Complex> FourieUB { get => _fourieUB; set => _fourieUB = value; }
        public List<Complex> FourieUC { get => _fourieUC; set => _fourieUC = value; }


        public int NumOfPoints { get => _numOfPoints; set => _numOfPoints = value; }
        public int NumOfPer { get => _numOfPer; set => _numOfPer = value; }
        public int NumOfPointsForVD { get => _numOfPointsForVD; set => SetProperty(ref _numOfPointsForVD, value); }
        public DelegateCommand StartAnalizeFourie { get; set; }
        public DelegateCommand StartAnalizeTypeOfFault { get; set; }
        public DelegateCommand MoveToBackCommand { get; }

        public VectorPlotter VectrorPlotter { get => _vectrorPlotter; set => SetProperty(ref _vectrorPlotter, value); }
        public int TimeForVD
        {
            get => _timeForVD;
            set
            {
                if (SetProperty(ref _timeForVD, value) || value <= NumOfPoints-NumOfPer)
                {
                    _vectrorPlotter?.UpdatePlot(value);
                }
                _timeForVD = value;
            }
        }


        public AnalizeOscillogramViewModel(SignalDataService signalDataService, IRegionManager regionManager)
        {
            _signalDataService = signalDataService;
            _regionManager = regionManager;
            StartAnalizeFourie = new DelegateCommand(GetFourieSignals, CanGetFourie);
            StartAnalizeTypeOfFault = new DelegateCommand(StartAnalizeTypeFault, CanStartAnalize);
            MoveToBackCommand = new DelegateCommand(MoveToBack);
        }
        public void GetFourieSignals()
        {
            FourieIA = new List<Complex>();
            FourieIB = new List<Complex>();
            FourieIC = new List<Complex>();
            FourieUA = new List<Complex>();
            FourieUB = new List<Complex>();
            FourieUC = new List<Complex>();

            NumOfPoints = _signalDataService.NumOfPoints;
            NumOfPer = _signalDataService.PoOfPer;
            NumOfPointsForVD = NumOfPoints - NumOfPer - 1;

            _fourieAnalizerI = new FourieAnalizer(NumOfPoints, NumOfPer, _signalDataService.CurrentA, _signalDataService.CurrentB, _signalDataService.CurrentC);
            _fourieAnalizerU = new FourieAnalizer(NumOfPoints, NumOfPer, _signalDataService.VoltageA, _signalDataService.VoltageB, _signalDataService.VoltageC);
            _fourieAnalizerI.RunAnalize();
            _fourieAnalizerU.RunAnalize();
            FourieIA = _fourieAnalizerI.FourieSignalA.ToList();
            FourieIB = _fourieAnalizerI.FourieSignalB.ToList();
            FourieIC = _fourieAnalizerI.FourieSignalC.ToList();
            FourieUA = _fourieAnalizerU.FourieSignalA.ToList();
            FourieUB = _fourieAnalizerU.FourieSignalB.ToList();
            FourieUC = _fourieAnalizerU.FourieSignalC.ToList();

            VectorsPlot();

            _fourieAnalizeF = true;
        }

        private void VectorsPlot()
        {
            VectrorPlotter = new VectorPlotter(FourieIA, FourieIB, FourieIC, "Векторная диаграмма токов");
            _totalTimeMS = _signalDataService.TimeValues.Count;
        }

        private void MoveToBack()
        {
            _regionManager.RequestNavigate("ContentRegion", "CometradeParserView");
        }

        private void StartAnalizeTypeFault()
        {
            _typeOfFaultAnalizer = new TypeOfFaultAnalizer(_fourieAnalizerI, _fourieAnalizerU);
        }

        public bool CanStartAnalize()
        {
            return _fourieAnalizeF;
        }

        public bool CanGetFourie()
        {
            return _signalDataService.CurrentA.Count != 0;
        }
    }
}
