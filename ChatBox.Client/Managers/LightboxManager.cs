using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChatBox.Client.ViewModels;

namespace ChatBox.Client.Managers
{
    public class LightboxManager
    {
        private readonly Grid _lightboxOverlay;
        private readonly Image _imgLightboxLarge;
        private readonly ImageBrush _imgLightboxAvatar;
        private readonly TextBlock _lblLightboxSender;
        private readonly TextBlock _lblLightboxTime;
        private readonly Button _btnLightboxDownload;

        private ChatMessage? _currentMessage;
        private readonly Converters.Base64ImageConverter? _avatarConverter;

        public LightboxManager(
            Grid lightboxOverlay,
            Image imgLightboxLarge,
            ImageBrush imgLightboxAvatar,
            TextBlock lblLightboxSender,
            TextBlock lblLightboxTime,
            Button btnLightboxDownload)
        {
            _lightboxOverlay = lightboxOverlay;
            _imgLightboxLarge = imgLightboxLarge;
            _imgLightboxAvatar = imgLightboxAvatar;
            _lblLightboxSender = lblLightboxSender;
            _lblLightboxTime = lblLightboxTime;
            _btnLightboxDownload = btnLightboxDownload;

            _avatarConverter = new Converters.Base64ImageConverter();
        }

        public void Open(ChatMessage msg, string currentUsername)
        {
            if (string.IsNullOrEmpty(msg.LocalFilePath) || !File.Exists(msg.LocalFilePath))
                return;

            _currentMessage = msg;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(msg.LocalFilePath);
                bitmap.EndInit();
                _imgLightboxLarge.Source = bitmap;
            }
            catch
            {
                _imgLightboxLarge.Source = null;
            }

            try
            {
                _imgLightboxAvatar.ImageSource = (ImageSource)_avatarConverter!.Convert(
                    msg.AvatarBase64, typeof(ImageSource), null, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                _imgLightboxAvatar.ImageSource = null;
            }

            _lblLightboxSender.Text = msg.Sender == "Me"
                ? (string.IsNullOrWhiteSpace(currentUsername) ? "User" : currentUsername)
                : msg.Sender;
            _lblLightboxTime.Text = msg.Timestamp;

            _lightboxOverlay.Visibility = Visibility.Visible;
        }

        public void Close()
        {
            _lightboxOverlay.Visibility = Visibility.Collapsed;
            _currentMessage = null;
        }

        public void Download(Action<string, string> showMessage)
        {
            if (_currentMessage == null || string.IsNullOrEmpty(_currentMessage.LocalFilePath)) return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = _currentMessage.Content,
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All Files|*.*"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_currentMessage.LocalFilePath, saveDialog.FileName, true);
                    showMessage("Downloaded successfully!", "Success");
                }
                catch (Exception ex)
                {
                    showMessage("Download failed: " + ex.Message, "Error");
                }
            }
        }
    }
}