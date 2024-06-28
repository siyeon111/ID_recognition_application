using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication0_basic.ViewModels;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AvaloniaApplication0_basic.Views
{
    public partial class HelpWindow : Window
    {
        private MainWindowViewModel _mainViewModel;

        public HelpWindow(MainWindowViewModel mainViewModel)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
            DataContext = _mainViewModel;
            UpdateTexts();
            _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
        }

        private void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.HelpText) ||
                e.PropertyName == nameof(MainWindowViewModel.ChatText) ||
                e.PropertyName == nameof(MainWindowViewModel.EtcText))
            {
                UpdateTexts();
            }
        }

        private void UpdateTexts()
        {
            ChatButton.Content = _mainViewModel.ChatText;
            etcButton.Content = _mainViewModel.EtcText;
            CloseButton.Content = _mainViewModel.ExitText;
            //HelpTitle.Text = _mainViewModel.HelpText;
            ResultTextBlock.Text = "";
        }

        private void etc_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "Breakdown button clicked.";
            string notice = "����";
            SendDataToServerAsync(notice);
        }

        private async Task SendDataToServerAsync(string notice)
        {
            // ������ IP �ּ� �� ��Ʈ ��ȣ
            string serverIp = "192.168.0.36"; // ������ IP �ּҸ� �Է��ϼ���
            int serverPort = 13000; // ������ ��Ʈ ��ȣ�� �Է��ϼ���

            // �׽�Ʈ�� ������ ����
            var testData = new
            {
                Notice = notice
            };

            string message = JsonSerializer.Serialize(testData);

            try
            {
                // ������ ����
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, serverPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] data = Encoding.ASCII.GetBytes(message);

                        // ������ ����
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }
            }
            catch (Exception e)
            {
                ResultTextBlock.Text = "Exception: " + e.Message;
            }
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            string notice = "ä��";
            SendDataToServerAsync(notice);
            ResultTextBlock.Text = "Click Chat button.";

            // Chat ��ư Ŭ�� �� ���ο� â�� �̹����� ǥ���մϴ�.
            string imagePath = @"C:\LeeJunYoung\Final_Project\2024_06_02\Emgu\ex_qr\QR_Naver.png";
            if (System.IO.File.Exists(imagePath))
            {
                var qrWindow = new QrWindow();
                qrWindow.SetImage(imagePath);
                qrWindow.Show();
            }
            else
            {
                ResultTextBlock.Text = "Image file not found.";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
