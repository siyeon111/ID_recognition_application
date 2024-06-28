using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication0_basic.ViewModels;

namespace AvaloniaApplication0_basic.Views
{
    public partial class BackWindow : Window
    {
        private MainWindowViewModel _mainViewModel;

        public BackWindow()
        {
            InitializeComponent();
        }

        public BackWindow(MainWindowViewModel mainViewModel)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
            DataContext = new BackWindowViewModel(mainViewModel);
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (BackWindowViewModel)DataContext;
            viewModel.StopBackWebcam();
            this.Close();
        }

        public void ReturnToMainWindow()
        {
            _mainViewModel.IsButtonEnabled = true;
            this.Close();
            var mainWindow = new MainWindow
            {
                DataContext = _mainViewModel
            };
            mainWindow.Show();
        }
    }
}
