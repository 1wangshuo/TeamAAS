using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using TeamAAS.ViewModels;
using TeamAAS.Views;

namespace TeamAAS
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<MainWindowViewModel>();

            containerRegistry.RegisterForNavigation<HomeView>("HomeView");
            containerRegistry.RegisterForNavigation<ProductView>("ProductView");
            containerRegistry.RegisterForNavigation<DebugView>("DebugView");
            containerRegistry.RegisterForNavigation<SettingView>("SettingView");
            containerRegistry.RegisterForNavigation<CalibrationView>("CalibrationView");
            containerRegistry.RegisterForNavigation<StatisticsView>("StatisticsView");
            containerRegistry.RegisterForNavigation<ParamsView>("ParamsView");
            containerRegistry.RegisterForNavigation<UserView>("UserView");
        }
    }
}
