using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Unity;
using System.Windows;
using COMTRADE_parser;
using Prism.Regions;
using System.IO;

namespace OscilAnalyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.AppendAllText("errors.txt", e.ExceptionObject.ToString());
                MessageBox.Show(Directory.GetCurrentDirectory());
            };
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Регистрация сервисов
            containerRegistry.RegisterSingleton<SignalDataService>();

            // Регистрация View для навигации
            containerRegistry.RegisterForNavigation<CometradeParserView, CometradeParserViewModel>();
            containerRegistry.RegisterForNavigation<AnalizeOscillogramView, AnalizeOscillogramViewModel>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("ContentRegion", "CometradeParserView");
        }


    }

}
