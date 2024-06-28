using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication0_basic.ViewModels;
using System;

namespace AvaloniaApplication0_basic.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Width = 800;
            Height = 600;
            Position = new PixelPoint(300, 300);
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            var mainViewModel = (MainWindowViewModel)DataContext;
            var goWindow = new GoWindow(mainViewModel);
            goWindow.Closed += (s, args) => this.Show();
            goWindow.Show();
            this.Hide();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainViewModel = (MainWindowViewModel)DataContext;
            var backWindow = new BackWindow(mainViewModel);
            backWindow.Closed += (s, args) => this.Show();
            backWindow.Show();
            this.Hide();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private async void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var mainViewModel = (MainWindowViewModel)DataContext;
            var helpWindow = new HelpWindow(mainViewModel);
            helpWindow.Closed += (s, args) => this.IsEnabled = true;
            this.IsEnabled = false;
            await helpWindow.ShowDialog(this);
        }
    }
}
