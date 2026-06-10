using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ChatBox.Client.ViewModels;
using LocalChat.Core.Contracts;

namespace ChatBox.Client.Managers
{
    public class FileTransferManager
    {
        private readonly IFileClient _fileClient;
        private readonly ChannelManager _channelManager;

        public event Action<double>? OnProgress;

        private ChatMessage? _currentTransferMessage;

        public FileTransferManager(IFileClient fileClient, ChannelManager channelManager)
        {
            _fileClient = fileClient;
            _channelManager = channelManager;
        }

        public async Task UploadFileAsync(string serverIp, string filePath, Guid fileId)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

            var fileInfo = new FileInfo(filePath);

            var msg = new ChatMessage
            {
                Sender = "Me",
                Content = fileInfo.Name,
                IsFile = true,
                FileId = fileId.ToString(),
                FileSize = fileInfo.Length,
                IsTransferring = true,
                TransferProgress = 0,
                IsMe = true,
                Timestamp = FormatTimestamp(DateTime.UtcNow.ToString("O")),
                LocalFilePath = filePath,
                IsInImageChannel = _channelManager.CurrentChannel == "images"
            };

            _channelManager.AllMessages.Add(msg);
            _channelManager.RefreshMessageList();
            _currentTransferMessage = msg;

            _fileClient.OnUploadProgress += HandleUploadProgress;

            try
            {
                await _fileClient.UploadFileAsync(serverIp, filePath, fileId);
            }
            finally
            {
                _fileClient.OnUploadProgress -= HandleUploadProgress;
            }

            msg.IsTransferring = false;
            _currentTransferMessage = null;
            _channelManager.RefreshMessageList();
        }

        public async Task DownloadFileAsync(string serverIp, string savePath, Guid fileId, long totalSize)
        {
            _currentTransferMessage = null;
            _fileClient.OnDownloadProgress += HandleDownloadProgress;

            try
            {
                await _fileClient.DownloadFileAsync(serverIp, savePath, fileId, totalSize);
            }
            finally
            {
                _fileClient.OnDownloadProgress -= HandleDownloadProgress;
            }
        }

        private void HandleUploadProgress(double percent)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_currentTransferMessage != null)
                {
                    _currentTransferMessage.TransferProgress = percent;
                }
                OnProgress?.Invoke(percent);
            });
        }

        private void HandleDownloadProgress(double percent)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_currentTransferMessage != null)
                {
                    _currentTransferMessage.TransferProgress = percent;
                }
                OnProgress?.Invoke(percent);
            });
        }

        public void SetTransferringMessage(ChatMessage? msg)
        {
            _currentTransferMessage = msg;
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