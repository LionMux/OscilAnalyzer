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
            containerRegistry.RegisterSingleton<Reader>();

            containerRegistry.Register<CometradeParserViewModel>();
            containerRegistry.Register<AnalizeOscillogramViewModel>();

            // Регистрация View для навигации
            containerRegistry.RegisterForNavigation<CometradeParserView, CometradeParserViewModel>();
            containerRegistry.RegisterForNavigation<AnalizeOscillogramView, AnalizeOscillogramViewModel>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            try
            {
                var test = Container.Resolve<AnalizeOscillogramViewModel>();
            }
            catch (Exception ex)
            {
                File.WriteAllText("resolve_error.txt", ex.ToString());
                throw;
            }
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("CometradeRegion", "CometradeParserView");
            regionManager.RequestNavigate("OscillAnalizeRegion", "AnalizeOscillogramView");
        }


    }

}
