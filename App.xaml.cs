using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Unity;
using System.Windows;
using COMTRADE_parser;

namespace OscilAnalyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<SignalDataService>();
            containerRegistry.RegisterSingleton<Reader>();
            containerRegistry.Register<CometradeParserViewModel>();
            //containerRegistry.Register<CometradeParserView>();
            containerRegistry.Register<AnalizeOscillogramViewModel>();
            //containerRegistry.Register<AnalizeOscillogramView>();
            containerRegistry.RegisterForNavigation<CometradeParserView, CometradeParserViewModel>();
            containerRegistry.RegisterForNavigation<AnalizeOscillogramView, AnalizeOscillogramViewModel>();
        }
        //public IServiceProvider ServiceProvider { get; private set; }
        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);

        //    var services = new ServiceCollection();

        //    //Регистрация всех зависимостей
        //    services.AddSingleton<SignalDataService>();
        //    services.AddSingleton<CometradeParserViewModel>();
        //    services.AddSingleton<AnalizeOscillogramViewModel>();
        //    var serviceProvider = services.BuildServiceProvider();
        //}

    }

}
