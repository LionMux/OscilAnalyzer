using COMTRADE_parser;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace OscilAnalyzer
{
    public class AnalizeOscillogramViewModel : BindableBase
    {
        private List<Complex> _processedSignalIA;
        private List<Complex> _processedSignalIB;
        private List<Complex> _processedSignalIC;
        private List<Complex> _processedSignalUA;
        private List<Complex> _processedSignalUB;
        private List<Complex> _processedSignalUC;

        private List<double> _currentARms;
        private List<double> _currentBRms;
        private List<double> _currentCRms;
        private List<double> _voltageARms;
        private List<double> _voltageBRms;
        private List<double> _voltageCRms;

        private List<double> _currentPramayaRms;
        private List<double> _currentObratnayaRms;
        private List<double> _currentNulevayaRms;
        private List<double> _voltagePramayaRms;
        private List<double> _voltageObratnayaRms;
        private List<double> _voltageNulevayaRms;

        private double _currentARmsNow;
        private double _currentBRmsNow;
        private double _currentCRmsNow;
        private double _currentPramayaRmsNow;
        private double _currentObratnayaRmsNow;
        private double _currentNulevayaRmsNow;
        private double _voltageARmsNow;
        private double _voltageBRmsNow;
        private double _voltageCRmsNow;
        private double _voltagePramayaRmsNow;
        private double _voltageObratnayaRmsNow;
        private double _voltageNulevayaRmsNow;

        private const int _windowMS = 20; // Окно измерения
        private int _totalTimeMS;
        private int _timeForVD;
        private int _numOfPoints;
        private int _maxIndex;
        private int _selectedIndex;
        private int _numOfPer;
        private bool _isLoading = false;
        private bool _notFoundFault;
        private double _progress;
        private double? _faultDistanceKm;
        private bool _isDistanceBusy = false;
        private string? _distanceError;
        private bool _modelAvailable;

        private readonly IRegionManager _regionManager;
        private readonly SignalDataService _signalDataService;
        private ISignalAnalizer _analizerI;
        private ISignalAnalizer _analizerU;
        private TypeOfFaultAnalizer _typeOfFaultAnalizer;
        private VectorPlotter _currentVectrorsPlotter;
        private VectorPlotter _voltageVectrorsPlotter;
        private RmsCalculator _rmsCalculator;
        private SymmetricalComponentsCalculator _symmetricalComponentsCalculatorI;
        private SymmetricalComponentsCalculator _symmetricalComponentsCalculatorU;

        public List<Complex> ProcessedSignalIA { get => _processedSignalIA; set => _processedSignalIA = value; }
        public List<Complex> ProcessedSignalIB { get => _processedSignalIB; set => _processedSignalIB = value; }
        public List<Complex> ProcessedSignalIC { get => _processedSignalIC; set => _processedSignalIC = value; }
        public List<Complex> ProcessedSignalUA { get => _processedSignalUA; set => _processedSignalUA = value; }
        public List<Complex> ProcessedSignalUB { get => _processedSignalUB; set => _processedSignalUB = value; }
        public List<Complex> ProcessedSignalUC { get => _processedSignalUC; set => _processedSignalUC = value; }

        public double CurrentARmsNow { get => _currentARmsNow; set => SetProperty(ref _currentARmsNow, value); }
        public double CurrentBRmsNow { get => _currentBRmsNow; set => SetProperty(ref _currentBRmsNow, value); }
        public double CurrentCRmsNow { get => _currentCRmsNow; set => SetProperty(ref _currentCRmsNow, value); }
        public double VoltageARmsNow { get => _voltageARmsNow; set => SetProperty(ref _voltageARmsNow, value); }
        public double VoltageBRmsNow { get => _voltageBRmsNow; set => SetProperty(ref _voltageBRmsNow, value); }
        public double VoltageCRmsNow { get => _voltageCRmsNow; set => SetProperty(ref _voltageCRmsNow, value); }

        public double CurrentPramayaRmsNow { get => _currentPramayaRmsNow; set => SetProperty(ref _currentPramayaRmsNow, value); }
        public double CurrentObratnayaRmsNow { get => _currentObratnayaRmsNow; set => SetProperty(ref _currentObratnayaRmsNow, value); }
        public double CurrentNulevayaRmsNow { get => _currentNulevayaRmsNow; set => SetProperty(ref _currentNulevayaRmsNow, value); }
        public double VoltagePramayaRmsNow { get => _voltagePramayaRmsNow; set => SetProperty(ref _voltagePramayaRmsNow, value); }
        public double VoltageObratnayaRmsNow { get => _voltageObratnayaRmsNow; set => SetProperty(ref _voltageObratnayaRmsNow, value); }
        public double VoltageNulevayaRmsNow { get => _voltageNulevayaRmsNow; set => SetProperty(ref _voltageNulevayaRmsNow, value); }

        public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public int NumOfPoints { get => _numOfPoints; set => _numOfPoints = value; }
        public int NumOfPer { get => _numOfPer; set => _numOfPer = value; }
        public int MaxIndex { get => _maxIndex; set => SetProperty(ref _maxIndex, value); }
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0) value = 0;
                if (value > MaxIndex) value = MaxIndex;
                if (SetProperty(ref _selectedIndex, value))
                {
                    int centerIndex = _selectedIndex + NumOfPer / 2;
                    _timeForVD = (int)_signalDataService.TimeValues[centerIndex];
                    RaisePropertyChanged(nameof(TimeForVD));
                    _currentVectrorsPlotter?.UpdatePlot(_selectedIndex);
                    _voltageVectrorsPlotter?.UpdatePlot(_selectedIndex);
                    UpdateRmsValues(_selectedIndex);
                }
            }
        }
        public bool IsLoading { get => _isLoading; set => UpdateVisibility(ref _isLoading, value); }
        public bool NotFoundFault { get => _notFoundFault; set => UpdateVisibility(ref _notFoundFault, value); }
        public Visibility LoadingVisibility => IsLoading == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MessageAboutFaultVisibility => NotFoundFault == true ? Visibility.Visible : Visibility.Collapsed;

        public DelegateCommand StartAnalizeFourie  { get; set; }
        public DelegateCommand StartAnalizeTypeOfFault { get; set; }
        public DelegateCommand MoveToBackCommand { get; }

        public VectorPlotter CurrentVectrorsPlotter { get => _currentVectrorsPlotter; set => SetProperty(ref _currentVectrorsPlotter, value); }
        public VectorPlotter VoltageVectrorsPlotter { get => _voltageVectrorsPlotter; set => SetProperty(ref _voltageVectrorsPlotter, value); }
                public int TimeForVD
        {
            get => _timeForVD;
            set
            {
                int newIndex = FindNearestIndex(value);
                if (newIndex != _selectedIndex)
                {
                    SelectedIndex = newIndex;
                }
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

        public double? FaultDistanceKm { get => _faultDistanceKm; set => SetProperty(ref _faultDistanceKm, value); }
        public bool IsDistanceBusy { get => _isDistanceBusy; set => UpdateVisibilityDistance(ref _isDistanceBusy, value); }
        public string? DistanceError { get => _distanceError; set => SetProperty(ref _distanceError, value); }
        public bool ModelAvailable { get => _modelAvailable; set => SetProperty(ref _modelAvailable, value); }
        public Visibility DistanceResultVisibility => FaultDistanceKm.HasValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DistanceErrorVisibility => !string.IsNullOrEmpty(DistanceError) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DistanceBusyVisibility => IsDistanceBusy ? Visibility.Visible : Visibility.Collapsed;

        public DelegateCommand CalculateDistanceCommand { get; }


        public AnalizeOscillogramViewModel(SignalDataService signalDataService, IRegionManager regionManager)
        {
            _signalDataService = signalDataService;
            _regionManager = regionManager;
            StartAnalizeFourie = new DelegateCommand(() => GetProcessedSignals(), CanGetAnalize);
            StartAnalizeTypeOfFault = new DelegateCommand(StartAnalizeTypeFault, () => _analizerI != null && _analizerU != null);
            MoveToBackCommand = new DelegateCommand(MoveToBack);
            CalculateDistanceCommand = new DelegateCommand(async () => await CalculateDistance(), () => _signalDataService.CurrentA.Count > 0 && ModelAvailable);
            StartAnalizeFourie.RaiseCanExecuteChanged();
            CheckModelAvailability();
        }
        private async Task GetProcessedSignals()
        {
            try
            {
                IsLoading = true;
                await Task.Run(() =>
                {
                    ProcessedSignalIA = new List<Complex>();
                    ProcessedSignalIB = new List<Complex>();
                    ProcessedSignalIC = new List<Complex>();
                    ProcessedSignalUA = new List<Complex>();
                    ProcessedSignalUB = new List<Complex>();
                    ProcessedSignalUC = new List<Complex>();

                    NumOfPoints = _signalDataService.NumOfPoints;
                    NumOfPer = _signalDataService.PoOfPer;
                    MaxIndex = NumOfPoints - NumOfPer;

                    _analizerI = new GoertzelAnalyzer(NumOfPoints, NumOfPer, _signalDataService.CurrentA, _signalDataService.CurrentB, _signalDataService.CurrentC, progress => Progress = progress * 0.5);
                    _analizerU = new GoertzelAnalyzer(NumOfPoints, NumOfPer, _signalDataService.VoltageA, _signalDataService.VoltageB, _signalDataService.VoltageC, progress => Progress = 50 + progress * 0.5);
                    ProcessedSignalIA = _analizerI.ProcessedSignalA.ToList();
                    ProcessedSignalIB = _analizerI.ProcessedSignalB.ToList();
                    ProcessedSignalIC = _analizerI.ProcessedSignalC.ToList();
                    ProcessedSignalUA = _analizerU.ProcessedSignalA.ToList();
                    ProcessedSignalUB = _analizerU.ProcessedSignalB.ToList();
                    ProcessedSignalUC = _analizerU.ProcessedSignalC.ToList();

                    _symmetricalComponentsCalculatorI = new SymmetricalComponentsCalculator(NumOfPoints, NumOfPer, ProcessedSignalIA, ProcessedSignalIB, ProcessedSignalIC);
                    _symmetricalComponentsCalculatorU = new SymmetricalComponentsCalculator(NumOfPoints, NumOfPer, ProcessedSignalUA, ProcessedSignalUB, ProcessedSignalUC);

                    _rmsCalculator = new RmsCalculator(NumOfPoints, NumOfPer);
                    _currentARms = _rmsCalculator.RmsCalculate(_signalDataService.CurrentA).ToList();
                    _currentBRms = _rmsCalculator.RmsCalculate(_signalDataService.CurrentB).ToList();
                    _currentCRms = _rmsCalculator.RmsCalculate(_signalDataService.CurrentC).ToList();
                    _voltageARms = _rmsCalculator.RmsCalculate(_signalDataService.VoltageA).ToList();
                    _voltageBRms = _rmsCalculator.RmsCalculate(_signalDataService.VoltageB).ToList();
                    _voltageCRms = _rmsCalculator.RmsCalculate(_signalDataService.VoltageC).ToList();
                    _currentPramayaRms = _rmsCalculator.RmsCalculateForComplex(_symmetricalComponentsCalculatorI.Pramaya).ToList();
                    _currentObratnayaRms = _rmsCalculator.RmsCalculateForComplex(_symmetricalComponentsCalculatorI.Obratnaya).ToList();
                    _currentNulevayaRms = _rmsCalculator.RmsCalculateForComplex(_symmetricalComponentsCalculatorI.Nulevaya).ToList();
                    _voltagePramayaRms = _rmsCalculator.RmsCalculateForComplex(_symmetricalComponentsCalculatorU.Pramaya).ToList();
                    _voltageObratnayaRms = _rmsCalculator.RmsCalculateForComplex(_symmetricalComponentsCalculatorU.Obratnaya).ToList();
                    _voltageNulevayaRms = _rmsCalculator.RmsCalculateForComplex(_symmetricalComponentsCalculatorU.Nulevaya).ToList();
                });
            }
            finally
            {
                IsLoading = false;
                _totalTimeMS = _signalDataService.TimeValues.Count;
                CurrentVectrorsPlotter = new VectorPlotter(ProcessedSignalIA, ProcessedSignalIB, ProcessedSignalIC, "Векторная диаграмма токов", new[] { "A", "B", "C" });
                VoltageVectrorsPlotter = new VectorPlotter(ProcessedSignalUA, ProcessedSignalUB, ProcessedSignalUC, "Векторная диаграмма напряжений", new[] { "A", "B", "C" });
                StartAnalizeTypeOfFault.RaiseCanExecuteChanged();
            }

        }


        private void MoveToBack()
        {
            _regionManager.RequestNavigate("ContentRegion", "CometradeParserView");
            StartAnalizeFourie.RaiseCanExecuteChanged();
        }

        private void StartAnalizeTypeFault()
        {
            NotFoundFault = false;
            _typeOfFaultAnalizer = new TypeOfFaultAnalizer(_analizerI, _analizerU, _symmetricalComponentsCalculatorI, _symmetricalComponentsCalculatorU, NumOfPer/2);
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

        private void UpdateVisibilityDistance(ref bool field, bool newValue)
        {
            SetProperty(ref field, newValue);
            RaisePropertyChanged(nameof(DistanceResultVisibility));
            RaisePropertyChanged(nameof(DistanceErrorVisibility));
            RaisePropertyChanged(nameof(DistanceBusyVisibility));
        }

        private void CheckModelAvailability()
        {
            var modelDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
            var onnxPath = Path.Combine(modelDir, "best_model.onnx");
            ModelAvailable = File.Exists(onnxPath);
        }

        private async Task CalculateDistance()
        {
            try
            {
                IsDistanceBusy = true;
                DistanceError = null;
                FaultDistanceKm = null;

                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var modelDir = Path.Combine(exeDir, "Models");

                double fsHz;
                if (_signalDataService.TimeValues.Count >= 2)
                {
                    // Время переведено в миллисекунды в CometradeParserViewModel.
                    // Разница между отсчетами составляет dt мс.
                    // fsHz = 1000.0 / dt. Например, 1000.0 / 0.5 = 2000 Гц.
                    fsHz = 1000.0 / (_signalDataService.TimeValues[1] - _signalDataService.TimeValues[0]);
                }
                else
                {
                    fsHz = 5000.0;
                }

                double result = await Task.Run(() =>
                {
                    using var model = new FaultDistanceModel(modelDir);
                    return model.Predict(
                        _signalDataService.CurrentA.ToArray(),
                        _signalDataService.CurrentB.ToArray(),
                        _signalDataService.CurrentC.ToArray(),
                        _signalDataService.VoltageA.ToArray(),
                        _signalDataService.VoltageB.ToArray(),
                        _signalDataService.VoltageC.ToArray(),
                        fsHz
                    );
                });

                FaultDistanceKm = result;
            }
            catch (Exception ex)
            {
                DistanceError = $"Ошибка расчёта расстояния: {ex.Message}";
            }
            finally
            {
                IsDistanceBusy = false;
            }
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



        private bool CanGetAnalize()
        {
            return _signalDataService.CurrentA.Count != 0 && !IsLoading;
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
                private int FindNearestIndex(int targetTimeMs)
        {
            if (_signalDataService?.TimeValues == null || _signalDataService.TimeValues.Count == 0)
                return 0;

            int centerOffset = NumOfPer / 2;
            double target = targetTimeMs - centerOffset;
            int closestIndex = 0;
            double minDiff = Math.Abs(_signalDataService.TimeValues[0] - target);

            for (int i = 1; i < _signalDataService.TimeValues.Count; i++)
            {
                double diff = Math.Abs(_signalDataService.TimeValues[i] - target);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestIndex = i;
                }
            }

            return Math.Min(closestIndex, MaxIndex);
        }
        private void UpdateRmsValues(int value)
        {
            if (_currentARms == null || value >= _currentARms.Count) return;
            CurrentARmsNow = _currentARms[value] * 1000;
            CurrentBRmsNow = _currentBRms[value] * 1000;
            CurrentCRmsNow = _currentCRms[value] * 1000;
            VoltageARmsNow = _voltageARms[value] * 1000;
            VoltageBRmsNow = _voltageBRms[value] * 1000;
            VoltageCRmsNow = _voltageCRms[value] * 1000;
            CurrentPramayaRmsNow = _currentPramayaRms[value] * 1000;
            CurrentObratnayaRmsNow = _currentObratnayaRms[value] * 1000;
            CurrentNulevayaRmsNow = _currentNulevayaRms[value] * 1000;
            VoltagePramayaRmsNow = _voltagePramayaRms[value];
            VoltageObratnayaRmsNow = _voltageObratnayaRms[value];
            VoltageNulevayaRmsNow = _voltageNulevayaRms[value];
        }
    }
}




