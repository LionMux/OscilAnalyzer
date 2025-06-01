using COMTRADE_parser;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using ScottPlot;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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
        private bool _fourieAnalizeF = true;

        private Brush _k3color;
        private Brush _kab2color;
        private Brush _kbc2color;
        private Brush _kca2color;
        private Brush _ka1color;
        private Brush _kb1color;
        private Brush _kc1color;
        private Brush _kab11color;
        private Brush _kbc11color;
        private Brush _kca11color;

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
        public bool FourieAnalizeF { get => _fourieAnalizeF; set => SetProperty(ref _fourieAnalizeF, value); }
        public DelegateCommand StartAnalizeFourie { get; set; }
        public DelegateCommand StartAnalizeTypeOfFault { get; set; }
        public DelegateCommand MoveToBackCommand { get; }

        public VectorPlotter VectrorPlotter { get => _vectrorPlotter; set => SetProperty(ref _vectrorPlotter, value); }
        public int TimeForVD
        {
            get => _timeForVD;
            set
            {
                if (SetProperty(ref _timeForVD, value) || value <= NumOfPoints - NumOfPer)
                {
                    _vectrorPlotter?.UpdatePlot(value);
                }
                _timeForVD = value;
            }
        }

        public Brush K3color => _typeOfFaultAnalizer?.K3 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kab2color => _typeOfFaultAnalizer?.Kab2 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kbc2color => _typeOfFaultAnalizer?.Kbc2 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kca2color => _typeOfFaultAnalizer?.Kca2 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Ka1color => _typeOfFaultAnalizer?.Ka1 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kb1color => _typeOfFaultAnalizer?.Kb1 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kc1color => _typeOfFaultAnalizer?.Kc1 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kab11color => _typeOfFaultAnalizer?.Kab11 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kbc11color => _typeOfFaultAnalizer?.Kbc11 == true ? Brushes.Yellow : Brushes.Gray;
        public Brush Kca11color => _typeOfFaultAnalizer?.Kca11 == true ? Brushes.Yellow : Brushes.Gray;

        public AnalizeOscillogramViewModel(SignalDataService signalDataService, IRegionManager regionManager)
        {
            _signalDataService = signalDataService;
            _regionManager = regionManager;
            StartAnalizeFourie = new DelegateCommand(GetFourieSignals, CanGetFourie);
            StartAnalizeTypeOfFault = new DelegateCommand(StartAnalizeTypeFault);
            MoveToBackCommand = new DelegateCommand(MoveToBack);
        }
        private void GetFourieSignals()
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
            FourieAnalizeF = false;
            StartAnalizeFourie.RaiseCanExecuteChanged();
        }

        private void VectorsPlot()
        {
            var labels = new[] { "A", "B", "C" };
            VectrorPlotter = new VectorPlotter(FourieIA, FourieIB, FourieIC, "Векторная диаграмма токов", labels);
            _totalTimeMS = _signalDataService.TimeValues.Count;
        }

        private void MoveToBack()
        {
            _regionManager.RequestNavigate("ContentRegion", "CometradeParserView");
        }

        private void StartAnalizeTypeFault()
        {
            _typeOfFaultAnalizer = new TypeOfFaultAnalizer(_fourieAnalizerI, _fourieAnalizerU, _numOfPer);
            _typeOfFaultAnalizer.StartFaultAnalize();
            CheckOfColorChange();
        }

        //public bool CanStartAnalize()
        //{
        //    return _fourieAnalizeF;
        //}

        private void CheckOfColorChange()
        {
            RaisePropertyChanged(nameof(K3color));
            RaisePropertyChanged(nameof(Ka1color));
            RaisePropertyChanged(nameof(Kb1color));
            RaisePropertyChanged(nameof(Kc1color));
            RaisePropertyChanged(nameof(Kab2color));
            RaisePropertyChanged(nameof(Kbc2color));
            RaisePropertyChanged(nameof(Kca2color));
            RaisePropertyChanged(nameof(Kab11color));
            RaisePropertyChanged(nameof(Kbc11color));
            RaisePropertyChanged(nameof(Kca11color));
        }

        private bool CanGetFourie()
        {
            return _signalDataService.CurrentA.Count != 0;
        }
    }
}
