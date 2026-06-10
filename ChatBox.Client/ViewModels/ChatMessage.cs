using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace ChatBox.Client.ViewModels
{
    public class ChatMessage : INotifyPropertyChanged
    {
        public string Sender { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsFile { get; set; }
        public string FileId { get; set; } = "";
        public long FileSize { get; set; }
        public string AvatarBase64 { get; set; } = "";
        public string DisplayText => IsFile ? $"Attachment: {Content} ({(double)FileSize / (1024 * 1024):F2} MB)" : Content;

        public bool IsMe { get; set; }
        public string Timestamp { get; set; } = "";
        public DateTime RawDate { get; set; } = DateTime.UtcNow;

        private bool _isDraft;
        public bool IsDraft
        {
            get => _isDraft;
            set { _isDraft = value; OnPropertyChanged(nameof(IsDraft)); }
        }

        private double _transferProgress;
        public double TransferProgress
        {
            get => _transferProgress;
            set { _transferProgress = value; OnPropertyChanged(nameof(TransferProgress)); }
        }

        private bool _isTransferring;
        public bool IsTransferring
        {
            get => _isTransferring;
            set { _isTransferring = value; OnPropertyChanged(nameof(IsTransferring)); }
        }

        private string? _localFilePath;
        public string? LocalFilePath
        {
            get => _localFilePath;
            set
            {
                _localFilePath = value;
                OnPropertyChanged(nameof(LocalFilePath));
                OnPropertyChanged(nameof(ShowImageControl));
                OnPropertyChanged(nameof(ShowFileControl));
            }
        }

        public bool IsImage
        {
            get
            {
                if (!IsFile) return false;
                string ext = Path.GetExtension(Content).ToLower();
                return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
            }
        }

        public bool ShowImageControl => IsImage;
        public bool ShowFileControl => IsFile && !IsImage;

        private bool _isInImageChannel;
        public bool IsInImageChannel
        {
            get => _isInImageChannel;
            set { _isInImageChannel = value; OnPropertyChanged(nameof(IsInImageChannel)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}