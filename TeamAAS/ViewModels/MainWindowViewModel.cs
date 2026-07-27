using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows;
using HandyControl.Themes;

namespace TeamAAS.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        private string _currentView = "HomeView";
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    UpdateTheme(value);
                    RaisePropertyChanged(nameof(ThemeIcon));
                }
            }
        }

        public string ThemeIcon => IsDarkMode ? "☀️" : "🌙";

        public DelegateCommand<string> NavigateCommand { get; }
        public DelegateCommand ToggleThemeCommand { get; }

        public MainWindowViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string>(Navigate);
            ToggleThemeCommand = new DelegateCommand(() => IsDarkMode = !IsDarkMode);
        }

        public void OnLoaded()
        {
            Navigate("HomeView");
        }

        private void Navigate(string viewName)
        {
            if (!string.IsNullOrEmpty(viewName))
            {
                _regionManager.RequestNavigate("ContentRegion", viewName);
                CurrentView = viewName;
            }
        }

        private void UpdateTheme(bool isDark)
        {
            ThemeManager.Current.ApplicationTheme = isDark ? ApplicationTheme.Light : ApplicationTheme.Dark;
        }
    }
}
