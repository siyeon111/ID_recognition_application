using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Avalonia.Media.Imaging;
using System.IO;
using SkiaSharp;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.Threading;
using AvaloniaApplication0_basic.Views;
using System.Linq;
using System.Device.Gpio; // GPIO 핀을 제어하기 위한 네임스페이스
using System.Device.Pwm; // PWM을 사용하기 위한 네임스페이스
using System.Device.Pwm.Drivers; // PWM 드라이버 사용을 위한 네임스페이스
using System.IO.Ports;
using System.Threading;

namespace AvaloniaApplication0_basic.ViewModels
{
    public class GoWindowViewModel : ViewModelBase
    {
        private Bitmap _videoFrame;
        private string _qrCodeResult;
        private VideoCapture _capture;
        private bool _isRunning;
        private Mat _frame;
        private string _startText;
        private string _returnText;
        private string _qrCodeResultText;
        private string _attendHeaderText;
        private MainWindowViewModel _mainViewModel;

        public GoWindowViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            StartGoWebcamCommand = ReactiveCommand.CreateFromTask(StartGoWebcamAsync);
            StopGoWebcamCommand = ReactiveCommand.Create(StopGoWebcam, Observable.Return(true));

            mainViewModel.WhenAnyValue(
                vm => vm.StartText,
                vm => vm.ReturnText,
                vm => vm.QRCodeResultText,
                vm => vm.AttendHeaderText)
                .Subscribe(tuple =>
                {
                    StartText = tuple.Item1;
                    ReturnText = tuple.Item2;
                    QRCodeResultText = tuple.Item3;
                    AttendHeaderText = tuple.Item4;
                });
        }

        public Bitmap VideoFrame
        {
            get => _videoFrame;
            set => this.RaiseAndSetIfChanged(ref _videoFrame, value);
        }

        public string QRCodeResult
        {
            get => _qrCodeResult;
            set => this.RaiseAndSetIfChanged(ref _qrCodeResult, value);
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

        public ReactiveCommand<Unit, Unit> StartGoWebcamCommand { get; }
        public ReactiveCommand<Unit, Unit> StopGoWebcamCommand { get; }

        private async Task StartGoWebcamAsync()
        {
            if (_isRunning) return;

            _isRunning = true;

            await Task.Run(() =>
            {
                try
                {
                    _capture = new VideoCapture(0, VideoCapture.API.V4L2);
                    //_capture = new VideoCapture(0, VideoCapture.API.DShow);
                    _capture.Set(CapProp.FrameWidth, 320);
                    _capture.Set(CapProp.FrameHeight, 240);
                    _capture.Set(CapProp.Fps, 60);

                    // String test = "win1";
                    // CvInvoke.NamedWindow(test);
                    // while (CvInvoke.WaitKey(1) == -1)
                    // {
                    //     _frame = _capture.QueryFrame();
                    //     CvInvoke.Imshow(test, _frame);
                    // }

                    if (!_capture.IsOpened)
                    {
                        QRCodeResult = "Error: Camera not found!";
                        _isRunning = false;
                        return;
                    }

                    _frame = new Mat();
                }
                catch (Exception ex)
                {
                    QRCodeResult = $"Error initializing camera: {ex.Message}";
                    _isRunning = false;
                }
            });

            if (_isRunning)
            {
                await CaptureAndProcessAsync();
            }

            _capture?.Dispose();
            _capture = null;
            _isRunning = false;
        }

        private async Task CaptureAndProcessAsync()
        {
            while (_isRunning)
            {
                try
                {
                    //_capture.Read(_frame);
                    _frame = _capture.QueryFrame();
                }
                catch (Exception ex)
                {
                    QRCodeResult = $"Error reading frame: {ex.Message}";
                    _isRunning = false;
                    break;
                }

                string emp_number = await Task.Run(() => CaptureQRCode());

                if (!string.IsNullOrEmpty(emp_number))
                {
                    if (await Task.Run(() => ProcessAndSendImage(emp_number)))
                    {
                        QRCodeResult = "이미지 전송 준비 완료";

                        await Task.Run(async () =>
                        {
                            using (TcpClient client = new TcpClient())
                            {
                                await client.ConnectAsync("192.168.0.38", 13000);
                                QRCodeResult = "서버에 연결됨";
                                NetworkStream stream = client.GetStream();

                                var qrdata = new QRData
                                {
                                    Id = emp_number,
                                    QRSuccess = "QR Recogenation"
                                };
                                string qrJson = JsonSerializer.Serialize(qrdata);

                                await SendMessageToServerAsync(stream, qrJson);

                                while (true)
                                {
                                    string response = await ReceiveMessageFromServerAsync(stream);
                                    QRCodeResult = $"서버로부터 받은 신호: {response}";

                                    if (response == "Face True")
                                    {
                                        QRCodeResult = "문열림";

                                        using (TcpClient client2 = new TcpClient())
                                        {
                                            await client2.ConnectAsync("192.168.0.38", 13000);
                                            QRCodeResult = "서버에 연결됨";
                                            NetworkStream stream2 = client2.GetStream();
                                            var logdata = new LogData
                                            {
                                                Id = emp_number,
                                                Time = DateTime.Now.ToString("yyyy:MM:dd T hh:mm:ss"),
                                                Commute = "1"
                                            };

                                            string logJson = JsonSerializer.Serialize(logdata);
                                            await SendMessageToServerAsync(stream2, logJson);
                                        }

                                        try
                                        {
                                            RPI_dll.RPIModule module = new RPI_dll.RPIModule();
                                            module.OnServoLEDWhite();
                                            module.PlayMp3(20, 4);
                                        }
                                        catch (PlatformNotSupportedException)
                                        {
                                            QRCodeResult = "GPIO 작업이 현재 플랫폼에서 지원되지 않습니다.";
                                        }

                                        // 초기화면으로 돌아가는 로직 추가
                                        await Dispatcher.UIThread.InvokeAsync(() =>
                                        {
                                            var applicationLifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                                            var goWindow = applicationLifetime?.Windows.OfType<GoWindow>().FirstOrDefault();
                                            goWindow?.ReturnToMainWindow();
                                        });

                                        break;
                                    }
                                    else if ((response == "Face False") || (response == "None Face"))
                                    {
                                        QRCodeResult = "문 안열림";

                                        try
                                        {
                                            RPI_dll.RPIModule module = new RPI_dll.RPIModule(18, 50, 0.5, 2.5, 23, 24, 4, 17);

                                            module.OpenPin(17, PinMode.Output);
                                            module.WritePin(17, PinValue.High);
                                            module.PlayMp3(15, 2);
                                            module.WritePin(17, PinValue.Low);
                                            module.ClosePin(17);
                                        }
                                        catch (PlatformNotSupportedException)
                                        {
                                            QRCodeResult = "GPIO 작업이 현재 플랫폼에서 지원되지 않습니다.";
                                        }

                                        // 초기화면으로 돌아가는 로직 추가
                                        await Dispatcher.UIThread.InvokeAsync(() =>
                                        {
                                            var applicationLifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                                            var goWindow = applicationLifetime?.Windows.OfType<GoWindow>().FirstOrDefault();
                                            goWindow?.ReturnToMainWindow();
                                        });

                                        break;
                                    }

                                    await Task.Delay(1000);
                                }

                                stream.Close();
                                client.Close();
                            }
                        });
                    }
                }

                if (_frame != null && !_frame.IsEmpty)
                {
                    VideoFrame = ConvertToAvaloniaBitmap(_frame);
                    
                }

                await Task.Delay(100);
            }
        }

        private string CaptureQRCode()
        {
            string emp_number = "";
            string[] qrResults = new string[20];
            int qrCount = 0;

            var qrCodeDetector = new QRCodeDetector();

            while (string.IsNullOrEmpty(emp_number))
            {
                if (_frame == null)
                {
                    break;
                }

                _capture.Read(_frame);
                if (!_frame.IsEmpty)
                {
                    int centerX = _frame.Width / 2;
                    int centerY = _frame.Height / 2;
                    int rectSize = Math.Min(_frame.Width, _frame.Height) / 2;
                    var qrRect = new Rectangle(centerX - rectSize / 2, centerY - rectSize / 2, rectSize, rectSize);

                    using (var mask = new Mat(_frame.Size, DepthType.Cv8U, 1))
                    {
                        mask.SetTo(new MCvScalar(0));
                        CvInvoke.Rectangle(mask, new System.Drawing.Rectangle(qrRect.X, qrRect.Y, qrRect.Width, qrRect.Height), new MCvScalar(255), -1);

                        var maskedImage = new Mat();
                        _frame.CopyTo(maskedImage, mask);

                        using Mat points = new Mat();
                        bool detected = qrCodeDetector.Detect(maskedImage, points);

                        if (detected && points.Total > 0)
                        {
                            try
                            {
                                string decodedText = qrCodeDetector.Decode(maskedImage, points);
                                if (!string.IsNullOrEmpty(decodedText))
                                {
                                    decodedText = Decryption_dll.Xor.XorDecrypt(decodedText);
                                    qrResults[qrCount] = decodedText;
                                    qrCount++;

                                    if (qrCount == 20)
                                    {
                                        bool allSame = true;
                                        string first = qrResults[0];
                                        qrCount = 0;

                                        foreach (string str in qrResults)
                                        {
                                            if (str != first)
                                            {
                                                allSame = false;
                                                break;
                                            }
                                        }

                                        if (allSame)
                                        {
                                            emp_number = first;
                                            QRCodeResult = "QR코드 인식완료: " + emp_number;
                                        }
                                        else
                                        {
                                            qrResults = new string[20];
                                        }
                                    }
                                }
                            }
                            catch (Emgu.CV.Util.CvException ex)
                            {
                                QRCodeResult = "QR 코드 디코딩 오류: " + ex.Message;
                            }
                        }

                        using (var transparent = new Mat(_frame.Size, DepthType.Cv8U, 3))
                        {
                            transparent.SetTo(new MCvScalar(128, 128, 128));

                            var frameData = _frame.ToImage<Bgr, byte>();
                            var maskData = mask.ToImage<Gray, byte>();
                            var transparentData = transparent.ToImage<Bgr, byte>();

                            for (int y = 0; y < frameData.Rows; y++)
                            {
                                for (int x = 0; x < frameData.Cols; x++)
                                {
                                    if (maskData.Data[y, x, 0] == 0)
                                    {
                                        frameData.Data[y, x, 0] = (byte)((frameData.Data[y, x, 0] * 0.5) + (transparentData.Data[y, x, 0] * 0.5));
                                        frameData.Data[y, x, 1] = (byte)((frameData.Data[y, x, 1] * 0.5) + (transparentData.Data[y, x, 1] * 0.5));
                                        frameData.Data[y, x, 2] = (byte)((frameData.Data[y, x, 2] * 0.5) + (transparentData.Data[y, x, 2] * 0.5));
                                    }
                                }
                            }

                            _frame = frameData.Mat;
                        }

                        CvInvoke.Rectangle(_frame, new System.Drawing.Rectangle(qrRect.X, qrRect.Y, qrRect.Width, qrRect.Height), new MCvScalar(0, 255, 0), 2);

                        VideoFrame = ConvertToAvaloniaBitmap(_frame);
                    }
                }

                if (!_isRunning)
                {
                    break;
                }
            }

            return emp_number;
        }

        public struct Rectangle
        {
            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }

            public Rectangle(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        private async Task<bool> ProcessAndSendImage(string emp_number)
        {
            Mat cleanFrame = new Mat();
            bool captured = false;
            DateTime startTime = DateTime.Now;
            bool result = false;

            while (_isRunning)
            {
                _capture.Read(_frame);
                _frame.CopyTo(cleanFrame);

                if (!_frame.IsEmpty)
                {
                    int centerX = _frame.Width / 2;
                    int centerY = _frame.Height / 2;
                    int rectWidth = 500;
                    int rectHeight = 400;

                    double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                    int countdown = 10 - (int)elapsedSeconds;

                    MCvScalar color = (countdown == 1) ? new MCvScalar(0, 0, 255) : new MCvScalar(0, 255, 0);

                    DrawCorners(_frame, centerX, centerY, rectWidth, rectHeight, 20, color, 2);

                    if (elapsedSeconds >= 0 && elapsedSeconds < 10)
                    {
                        CvInvoke.PutText(_frame, countdown.ToString(), new System.Drawing.Point(centerX - 20, centerY - 20),
                            FontFace.HersheySimplex, 2.0, new MCvScalar(0, 0, 255), 2);
                    }

                    VideoFrame = ConvertToAvaloniaBitmap(_frame);
                }

                if ((DateTime.Now - startTime).TotalSeconds >= 10 && !captured)
                {
                    byte[] imageData = cleanFrame.ToImage<Bgr, Byte>().ToJpegData();

                    if (imageData != null)
                    {
                        result = ProcessImage(imageData, emp_number);
                    }

                    captured = true;
                }

                if (result || !_isRunning) break;

                await Task.Delay(100);
            }
            return result;
        }


        private bool ProcessImage(byte[] imageData, string number)
        {
            bool isSaved;

            if (number.Length > 4)
            {
                Encryption_dll.Encrypt.Encrypt_ToDB(int.Parse(number), imageData, "faceimg2");
                isSaved = Check_FaceImg2(number);
                if (isSaved)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if ((number.Length <= 3) && (number.Length > 0))
            {
                Encryption_dll.Encrypt.Encrypt_ToDB(int.Parse(number), imageData, "face_img2");
                isSaved = Check_GuestFaceImg2(number);
                if (isSaved)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private bool Check_FaceImg2(string number)
        {
            try
            {
                MySQL_dll.Handler handler = new MySQL_dll.Handler();
                byte[] imageData = handler.Get_FaceImg2(number);
                return imageData != null && imageData.Length > 0;
            }
            catch (Exception e)
            {
                QRCodeResult = "Exception: " + e.Message;
                return false;
            }
        }

        private bool Check_GuestFaceImg2(string number)
        {
            try
            {
                MySQL_dll.Handler handler = new MySQL_dll.Handler();
                byte[] imageData = handler.Get_GuestFaceImg2(number);
                return imageData != null && imageData.Length > 0;
            }
            catch (Exception e)
            {
                QRCodeResult = "Exception: " + e.Message;
                return false;
            }
        }

        private async Task SendMessageToServerAsync(NetworkStream stream, string message)
        {
            try
            {
                Byte[] data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                QRCodeResult = "Exception: " + e.Message;
            }
        }

        private async Task<string> ReceiveMessageFromServerAsync(NetworkStream stream)
        {
            byte[] buffer = new byte[256];
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    return Encoding.ASCII.GetString(buffer, 0, bytesRead);
                }
                else
                {
                    return "x";
                }
            }
            catch (Exception e)
            {
                QRCodeResult = "Exception: " + e.Message;
                return string.Empty;
            }
        }

        private void DrawCorners(Mat frame, int centerX, int centerY, int rectWidth, int rectHeight, int lineLength, MCvScalar color, int thickness)
        {
            CvInvoke.Line(frame, new System.Drawing.Point(centerX - rectWidth / 2, centerY - rectHeight / 2),
                new System.Drawing.Point(centerX - rectWidth / 2 + lineLength, centerY - rectHeight / 2), color, thickness);
            CvInvoke.Line(frame, new System.Drawing.Point(centerX - rectWidth / 2, centerY - rectHeight / 2),
                new System.Drawing.Point(centerX - rectWidth / 2, centerY - rectHeight / 2 + lineLength), color, thickness);

            CvInvoke.Line(frame, new System.Drawing.Point(centerX + rectWidth / 2, centerY - rectHeight / 2),
                new System.Drawing.Point(centerX + rectWidth / 2 - lineLength, centerY - rectHeight / 2), color, thickness);
            CvInvoke.Line(frame, new System.Drawing.Point(centerX + rectWidth / 2, centerY - rectHeight / 2),
                new System.Drawing.Point(centerX + rectWidth / 2, centerY - rectHeight / 2 + lineLength), color, thickness);

            CvInvoke.Line(frame, new System.Drawing.Point(centerX - rectWidth / 2, centerY + rectHeight / 2),
                new System.Drawing.Point(centerX - rectWidth / 2 + lineLength, centerY + rectHeight / 2), color, thickness);
            CvInvoke.Line(frame, new System.Drawing.Point(centerX - rectWidth / 2, centerY + rectHeight / 2),
                new System.Drawing.Point(centerX - rectWidth / 2, centerY + rectHeight / 2 - lineLength), color, thickness);

            CvInvoke.Line(frame, new System.Drawing.Point(centerX + rectWidth / 2, centerY + rectHeight / 2),
                new System.Drawing.Point(centerX + rectWidth / 2 - lineLength, centerY + rectHeight / 2), color, thickness);
            CvInvoke.Line(frame, new System.Drawing.Point(centerX + rectWidth / 2, centerY + rectHeight / 2),
                new System.Drawing.Point(centerX + rectWidth / 2, centerY + rectHeight / 2 - lineLength), color, thickness);
        }

        public void StopGoWebcam()
        {
            _isRunning = false;
            _capture?.Dispose();
            _capture = null;
        }

        private Bitmap ConvertToAvaloniaBitmap(Mat frame)
        {
            using (var image = frame.ToImage<Bgra, byte>())
            {
                var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888);
                using (var skBitmap = new SKBitmap(info))
                {
                    var pixelData = image.Bytes;
                    Marshal.Copy(pixelData, 0, skBitmap.GetPixels(), pixelData.Length);

                    using (var skImage = SKImage.FromBitmap(skBitmap))
                    using (var data = skImage.Encode(SKEncodedImageFormat.Png, 100))
                    using (var stream = new MemoryStream())
                    {
                        data.SaveTo(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        return new Bitmap(stream);
                    }
                }
            }
        }

        public class QRData
        {
            public string Id { get; set; }
            public string QRSuccess { get; set; }
        }

        public class LogData
        {
            public string Id { get; set; }
            public string Time { get; set; }
            public string Commute { get; set; }
        }
    }
}
