using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;

namespace AvaloniaApplication0_basic.Views
{
    public partial class QrWindow : Window
    {
        public QrWindow()
        {
            InitializeComponent();
        }

        public void SetImage(string imagePath)
        {
            if (System.IO.File.Exists(imagePath))
            {
                QrImage.Source = new Bitmap(imagePath);
            }
            else
            {
                throw new ArgumentException("Image file not found.", nameof(imagePath));
            }
        }
    }
}
