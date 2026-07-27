using System.Windows;
using TeamAAS.ViewModels;

namespace TeamAAS
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                (DataContext as MainWindowViewModel)?.OnLoaded();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"导航初始化失败: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }
}
