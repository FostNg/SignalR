using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using ChatBox.Client.ViewModels;

namespace ChatBox.Client.Managers
{
    public class ClipboardPasteHandler
    {
        private readonly FileTransferManager _fileTransferManager;
        private readonly ChannelManager _channelManager;
        private readonly string _serverIp;
        private readonly string _userId;
        private readonly string _avatarBase64;

        public event Action<ChatMessage>? OnMessageAdded;

        public ClipboardPasteHandler(
            FileTransferManager fileTransferManager,
            ChannelManager channelManager,
            string serverIp,
            string userId,
            string avatarBase64)
        {
            _fileTransferManager = fileTransferManager;
            _channelManager = channelManager;
            _serverIp = serverIp;
            _userId = userId;
            _avatarBase64 = avatarBase64;
        }

        public async Task HandleImagePasteAsync()
        {
            try
            {
                var image = Clipboard.GetImage();
                if (image == null) return;

                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempPaste");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                string fileName = $"ClipboardImage_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(tempDir, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                    encoder.Save(fileStream);
                }

                Guid fileId = Guid.NewGuid();
                var fileInfo = new FileInfo(filePath);

                var msg = new ChatMessage
                {
                    Sender = "Me",
                    Content = fileInfo.Name,
                    IsFile = true,
                    FileId = fileId.ToString(),
                    FileSize = fileInfo.Length,
                    AvatarBase64 = _avatarBase64,
                    IsTransferring = true,
                    TransferProgress = 0,
                    IsMe = true,
                    Timestamp = FormatTimestamp(DateTime.UtcNow.ToString("O")),
                    LocalFilePath = filePath,
                    IsInImageChannel = _channelManager.CurrentChannel == "images"
                };

                _channelManager.AllMessages.Add(msg);

                if (_channelManager.CurrentChannel == "images")
                {
                    // refresh gallery
                }
                else
                {
                    _channelManager.RefreshMessageList();
                }

                await _fileTransferManager.UploadFileAsync(_serverIp, filePath, fileId);

                msg.IsTransferring = false;
                OnMessageAdded?.Invoke(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to paste image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string FormatTimestamp(string timeString)
        {
            if (DateTime.TryParse(timeString, out DateTime dt))
            {
                var local = dt.ToLocalTime();
                if (local.Date == DateTime.Now.Date)
                    return local.ToString("HH:mm");
                if (local.Year == DateTime.Now.Year)
                    return local.ToString("dd/MM HH:mm");
                return local.ToString("dd/MM/yyyy HH:mm");
            }
            return timeString;
        }
    }
}