using Prism.Commands;
using Prism.Mvvm;
using System.Windows.Input;

namespace OscilAnalyzer;

internal class MainWindowViewModel : BindableBase
{
    public bool UpdateEnable { get; set; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private CometradeParser _cometradeParser;

    public int Count
    {
        get => _count;
        set
        {
            SetProperty(ref _count, value);
            //DoCommand.RaiseCanExecuteChanged();
        }
    }

    //public SimpleCommand DoCommand { get; }

    public DelegateCommand DoCommand { get; }

    private int _count = 10;
    private string _name;

    public MainWindowViewModel()
    {
        Name = "count";
        //DoCommand = new SimpleCommand(this);
        _cometradeParser = new CometradeParser();

        DoCommand = new DelegateCommand(Clear, CanPushBtn)
            .ObservesProperty(() => Count)
            .ObservesProperty(() => Name);
        UpdateText();

    }

    private bool CanPushBtn()
    {
        return Count % 2 == 0 && Name != "";
    }

    private async void UpdateText()
    {
        while (true)
        {
            await Task.Delay(5000);

            if (UpdateEnable)
            {
                Count++;
                Name = "count " + Count;

                //if (Count % 2 == 0)
                //{

                //    //DoCommand.SetExecutable(false);
                //}
                //else
                //{
                //    //DoCommand.SetExecutable(true);
                //}
            }

        }
    }

    private void Clear()
    {
        Name = "count";
        _cometradeParser.ReadSignal();
    }

    internal class SimpleCommand : ICommand
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private bool _canExecute = true;


        public SimpleCommand(MainWindowViewModel mainWindowViewModel)
        {
            this._mainWindowViewModel = mainWindowViewModel;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute;
        }

        public void SetExecutable(bool canExecute)
        {
            _canExecute = canExecute;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Execute(object? parameter)
        {
            _mainWindowViewModel.Clear();
        }
    }
}
