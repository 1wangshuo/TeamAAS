using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using TeamAAS.Camera.Interfaces;
using TeamAAS.Camera.Services;
using TeamAAS.Robot.Interfaces;
using TeamAAS.Robot.Services;
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

            // 注册相机服务
            containerRegistry.RegisterSingleton<ICameraService, CameraService>();

            // 注册机器人服务
            containerRegistry.RegisterSingleton<IRobotService, RobotService>();
            containerRegistry.RegisterSingleton<SettingViewModel>();

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
