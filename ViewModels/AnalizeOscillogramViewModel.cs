using COMTRADE_parser;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        private double _currentARms;
        private double _currentBRms;
        private double _currentCRms;
        private double _voltageARms;
        private double _voltageBRms;
        private double _voltageCRms;

        private const int _windowMS = 20; // Окно измерения
        private int _totalTimeMS;
        private int _timeForVD;
        private int _numOfPoints;
        private int _numOfPointsForVD;
        private int _numOfPer;
        private bool _isLoading = false;
        private bool _notFoundFault;
        private double _progress;

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

        public double CurrentARms { get => _currentARms; set => SetProperty(ref _currentARms, value); }
        public double CurrentBRms { get => _currentBRms; set => SetProperty(ref _currentBRms, value); }
        public double CurrentCRms { get => _currentCRms; set => SetProperty(ref _currentCRms, value); }
        public double VoltageARms { get => _voltageARms; set => SetProperty(ref _voltageARms, value); }
        public double VoltageBRms { get => _voltageBRms; set => SetProperty(ref _voltageBRms, value); }
        public double VoltageCRms { get => _voltageCRms; set => SetProperty(ref _voltageCRms, value); }

        public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public int NumOfPoints { get => _numOfPoints; set => _numOfPoints = value; }
        public int NumOfPer { get => _numOfPer; set => _numOfPer = value; }
        public int NumOfPointsForVD { get => _numOfPointsForVD; set => SetProperty(ref _numOfPointsForVD, value); }
        public bool IsLoading { get => _isLoading; set => UpdateVisibility(ref _isLoading, value); }
        public bool NotFoundFault { get => _notFoundFault; set => UpdateVisibility(ref _notFoundFault, value); }
        public Visibility LoadingVisibility => IsLoading == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MessageAboutFaultVisibility => NotFoundFault == true ? Visibility.Visible : Visibility.Collapsed;

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
                    UpdateRmsValues(value);
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
            StartAnalizeFourie = new DelegateCommand(() => GetFourieSignals(), CanGetFourie);
            StartAnalizeTypeOfFault = new DelegateCommand(StartAnalizeTypeFault);
            MoveToBackCommand = new DelegateCommand(MoveToBack);
            StartAnalizeFourie.RaiseCanExecuteChanged();
        }
        private async Task GetFourieSignals()
        {
            try
            {
                IsLoading = true;
                await Task.Run(() =>
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

                    _fourieAnalizerI = new FourieAnalizer(NumOfPoints, NumOfPer, _signalDataService.CurrentA, _signalDataService.CurrentB, _signalDataService.CurrentC, progress => Progress = progress);
                    _fourieAnalizerU = new FourieAnalizer(NumOfPoints, NumOfPer, _signalDataService.VoltageA, _signalDataService.VoltageB, _signalDataService.VoltageC, progress => Progress = progress);
                    _fourieAnalizerI.RunAnalize();
                    _fourieAnalizerU.RunAnalize();
                    FourieIA = _fourieAnalizerI.FourieSignalA.ToList();
                    FourieIB = _fourieAnalizerI.FourieSignalB.ToList();
                    FourieIC = _fourieAnalizerI.FourieSignalC.ToList();
                    FourieUA = _fourieAnalizerU.FourieSignalA.ToList();
                    FourieUB = _fourieAnalizerU.FourieSignalB.ToList();
                    FourieUC = _fourieAnalizerU.FourieSignalC.ToList();

                });
            }
            finally
            {
                IsLoading = false;
                VectorsPlot();
            }

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
            StartAnalizeFourie.RaiseCanExecuteChanged();
        }

        private void StartAnalizeTypeFault()
        {
            NotFoundFault = false;
            _typeOfFaultAnalizer = new TypeOfFaultAnalizer(_fourieAnalizerI, _fourieAnalizerU, _numOfPer);
            _typeOfFaultAnalizer.StartFaultAnalize();
            CheckOfColorChange();
            CheckExistFault();
        }

        //public bool CanStartAnalize()
        //{
        //    return _fourieAnalizeF;
        //}

        private void UpdateVisibility(ref bool field, bool newValue)
        {
            SetProperty(ref field, newValue);
            RaisePropertyChanged(nameof(LoadingVisibility));
            RaisePropertyChanged(nameof(MessageAboutFaultVisibility));
        }

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

        private void UpdateRmsValues(int index)
        {
            if (_fourieAnalizerU.Nulevaya[NumOfPoints - NumOfPer - 1] != 0)
            {
                CurrentARms = _fourieIA[index].Magnitude / Math.Sqrt(2);
                CurrentBRms = _fourieIB[index].Magnitude / Math.Sqrt(2);
                CurrentCRms = _fourieIC[index].Magnitude / Math.Sqrt(2);
                VoltageARms = _fourieUA[index].Magnitude / Math.Sqrt(2);
                VoltageBRms = _fourieUB[index].Magnitude / Math.Sqrt(2);
                VoltageCRms = _fourieUC[index].Magnitude / Math.Sqrt(2);
            }
            else
            {
                CurrentARms = 0;
                CurrentBRms = 0;    
                CurrentCRms = 0;
                VoltageARms = 0;
                VoltageBRms = 0;
                VoltageCRms = 0;
            }
        }



        private bool CanGetFourie()
        {
            return _signalDataService.CurrentA.Count != 0;
        }

        private void CheckExistFault()
        {
            if (_typeOfFaultAnalizer?.K3 == false && _typeOfFaultAnalizer?.Ka1 == false && _typeOfFaultAnalizer?.Kb1 == false &&
                _typeOfFaultAnalizer?.Kc1 == false && _typeOfFaultAnalizer?.Kab2 == false && _typeOfFaultAnalizer?.Kbc2 == false &&
                _typeOfFaultAnalizer?.Kca2 == false && _typeOfFaultAnalizer?.Kab11 == false && _typeOfFaultAnalizer?.Kbc11 == false &&
                _typeOfFaultAnalizer?.Kca11 == false)
            {
                NotFoundFault = true;
            }
            else
            {
                NotFoundFault = false;
            }
        }
    }
}
