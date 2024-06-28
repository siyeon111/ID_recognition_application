using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication0_basic.ViewModels;

namespace AvaloniaApplication0_basic.Views
{
    public partial class GoWindow : Window
    {
        private MainWindowViewModel _mainViewModel;

        public GoWindow()
        {
            InitializeComponent();
        }

        public GoWindow(MainWindowViewModel mainViewModel)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
            DataContext = new GoWindowViewModel(mainViewModel);
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (GoWindowViewModel)DataContext;
            viewModel.StopGoWebcam();
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
