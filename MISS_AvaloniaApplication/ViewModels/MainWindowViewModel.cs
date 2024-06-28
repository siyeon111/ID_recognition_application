using ReactiveUI;
using RPI_dll;
using System;
using System.Device.Gpio;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaApplication0_basic.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _weatherDescription;
        private double _temperature;
        private string _date;
        private bool _isButtonEnabled = true; // Set initial state to disabled
        private Timer _timer;
        private string _welcomeText = "Welcome Company";
        private string _chooseText = "Choose";
        private string _attendText = "Attend";
        private string _leaveText = "Leave";
        private string _helpText = "Help";
        private string _exitText = "Exit";
        private string _dateText = "Date:";
        private string _temperatureText = "Temperature:";
        private string _weatherText = "Weather:";
        private string _startText = "Start";
        private string _returnText = "Return";
        private string _qrCodeResultText = "QR Code Result";
        private string _attendHeaderText = "Attend";
        private string _leaveHeaderText = "Leave";
        private string _chatText = "Chat";
        private string _etcText = "Breakdown";

        //private RPIModule _rpiModule;
        private CancellationTokenSource _cts;

        public MainWindowViewModel()
        {
            FetchWeatherCommand = ReactiveCommand.CreateFromTask(FetchWeatherAsync);
            FetchWeatherCommand.ThrownExceptions.Subscribe(ex => {
                Console.WriteLine($"Error fetching weather: {ex.Message}");
            });

            SetEnglishCommand = ReactiveCommand.Create(SetEnglish);
            SetKoreanCommand = ReactiveCommand.Create(SetKorean);

            FetchWeatherAsync().ConfigureAwait(false);

            _timer = new Timer(async _ => await FetchWeatherAsync(), null, TimeSpan.Zero, TimeSpan.FromHours(1));

            //_rpiModule = new RPIModule(18, 50, 0.5, 2.5, 23, 24, 4, 17);
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorDistance(_cts.Token));
        }

        public string WeatherDescription
        {
            get => _weatherDescription;
            set => this.RaiseAndSetIfChanged(ref _weatherDescription, value);
        }

        public double Temperature
        {
            get => _temperature;
            set => this.RaiseAndSetIfChanged(ref _temperature, value);
        }

        public string Date
        {
            get => _date;
            set => this.RaiseAndSetIfChanged(ref _date, value);
        }

        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref _isButtonEnabled, value);
        }

        public string WelcomeText
        {
            get => _welcomeText;
            set => this.RaiseAndSetIfChanged(ref _welcomeText, value);
        }

        public string ChooseText
        {
            get => _chooseText;
            set => this.RaiseAndSetIfChanged(ref _chooseText, value);
        }

        public string AttendText
        {
            get => _attendText;
            set => this.RaiseAndSetIfChanged(ref _attendText, value);
        }

        public string LeaveText
        {
            get => _leaveText;
            set => this.RaiseAndSetIfChanged(ref _leaveText, value);
        }

        public string HelpText
        {
            get => _helpText;
            set => this.RaiseAndSetIfChanged(ref _helpText, value);
        }

        public string ExitText
        {
            get => _exitText;
            set => this.RaiseAndSetIfChanged(ref _exitText, value);
        }

        public string DateText
        {
            get => _dateText;
            set => this.RaiseAndSetIfChanged(ref _dateText, value);
        }

        public string TemperatureText
        {
            get => _temperatureText;
            set => this.RaiseAndSetIfChanged(ref _temperatureText, value);
        }

        public string WeatherText
        {
            get => _weatherText;
            set => this.RaiseAndSetIfChanged(ref _weatherText, value);
        }

        public string StartText
        {
            get => _startText;
            set => this.RaiseAndSetIfChanged(ref _startText, value);
        }

        public string ReturnText
        {
            get => _returnText;
            set => this.RaiseAndSetIfChanged(ref _returnText, value);
        }

        public string QRCodeResultText
        {
            get => _qrCodeResultText;
            set => this.RaiseAndSetIfChanged(ref _qrCodeResultText, value);
        }

        public string AttendHeaderText
        {
            get => _attendHeaderText;
            set => this.RaiseAndSetIfChanged(ref _attendHeaderText, value);
        }

        public string LeaveHeaderText
        {
            get => _leaveHeaderText;
            set => this.RaiseAndSetIfChanged(ref _leaveHeaderText, value);
        }

        public string ChatText
        {
            get => _chatText;
            set => this.RaiseAndSetIfChanged(ref _chatText, value);
        }

        public string EtcText
        {
            get => _etcText;
            set => this.RaiseAndSetIfChanged(ref _etcText, value);
        }

        public ReactiveCommand<Unit, Unit> FetchWeatherCommand { get; }
        public ReactiveCommand<Unit, Unit> SetEnglishCommand { get; }
        public ReactiveCommand<Unit, Unit> SetKoreanCommand { get; }

        private void SetEnglish()
        {
            WelcomeText = "Welcome Company";
            ChooseText = "Choose";
            AttendText = "Attend";
            LeaveText = "Leave";
            HelpText = "Help";
            ExitText = "Exit";
            DateText = "Date:";
            TemperatureText = "Temperature:";
            WeatherText = "Weather:";
            StartText = "Start";
            ReturnText = "Return";
            QRCodeResultText = "QR Code Result";
            AttendHeaderText = "Attend";
            LeaveHeaderText = "Leave";
            ChatText = "Chat";
            EtcText = "etc.";
        }

        private void SetKorean()
        {
            WelcomeText = "환영합니다";
            ChooseText = "선택하세요";
            AttendText = "출근";
            LeaveText = "퇴근";
            HelpText = "도움말";
            ExitText = "종료";
            DateText = "날짜:";
            TemperatureText = "온도:";
            WeatherText = "날씨:";
            StartText = "시작";
            ReturnText = "돌아가기";
            QRCodeResultText = "QR 코드 결과";
            AttendHeaderText = "출근";
            LeaveHeaderText = "퇴근";
            ChatText = "채팅";
            EtcText = "기타";
        }

        private async Task FetchWeatherAsync()
        {
            try
            {
                (WeatherDescription, Temperature, Date) = await WeatherService.Weather_Information.GetWeatherInfoAsync("Cheonan");
                Console.WriteLine($"Weather: {WeatherDescription}, Temperature: {Temperature}, Date: {Date}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in FetchWeatherAsync: {ex.Message}");
            }
        }

        private async Task MonitorDistance(CancellationToken cancellationToken)
        {
            RPI_dll.RPIModule module = new RPI_dll.RPIModule(18, 50, 0.5, 2.5, 23, 24, 4, 17);
            module.OpenPin(23, PinMode.Output);
            module.OpenPin(24, PinMode.Input);
            double distance = 100;

            while (!cancellationToken.IsCancellationRequested)
            {
                distance = module.GetDistance();
                                
                Console.WriteLine($"Distance: {distance} cm");

                if (distance <= 150)
                {
                    IsButtonEnabled = true;
                }
                else
                {
                    IsButtonEnabled = false;
                }

                await Task.Delay(500);
            }
            module.ClosePin(23);
            module.ClosePin(24);

        }

        ~MainWindowViewModel()
        {
            _cts.Cancel();
        }
    }
}
