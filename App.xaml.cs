using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Unity;
using System.Windows;
using COMTRADE_parser;
using Prism.Regions;

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
            // Регистрация сервисов
            containerRegistry.RegisterSingleton<SignalDataService>();
            containerRegistry.RegisterSingleton<Reader>();

            // Регистрация ViewModel
            containerRegistry.Register<CometradeParserViewModel>();
            containerRegistry.Register<AnalizeOscillogramViewModel>();

            // Регистрация View для навигации (без указания ViewModel)
            containerRegistry.RegisterForNavigation<CometradeParserView>();
            containerRegistry.RegisterForNavigation<AnalizeOscillogramView>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("CometradeRegion", "CometradeParserView");
            regionManager.RequestNavigate("AnalizeRegion", "AnalizeOscillogramView");
        }

    }

}
