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
            string notice = "고장";
            SendDataToServerAsync(notice);
        }

        private async Task SendDataToServerAsync(string notice)
        {
            // 서버의 IP 주소 및 포트 번호
            string serverIp = "192.168.0.36"; // 서버의 IP 주소를 입력하세요
            int serverPort = 13000; // 서버의 포트 번호를 입력하세요

            // 테스트용 데이터 생성
            var testData = new
            {
                Notice = notice
            };

            string message = JsonSerializer.Serialize(testData);

            try
            {
                // 서버에 연결
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, serverPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] data = Encoding.ASCII.GetBytes(message);

                        // 데이터 전송
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
            string notice = "채팅";
            SendDataToServerAsync(notice);
            ResultTextBlock.Text = "Click Chat button.";

            // Chat 버튼 클릭 시 새로운 창에 이미지를 표시합니다.
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
