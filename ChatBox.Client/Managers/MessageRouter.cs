using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using ChatBox.Client.ViewModels;
using LocalChat.Core.Contracts;

namespace ChatBox.Client.Managers
{
    public class MessageRouter
    {
        private readonly ChannelManager _channelManager;
        private readonly GalleryManager _galleryManager;
        private readonly LightboxManager _lightboxManager;
        private readonly Action<ChatMessage> _openLightbox;
        private readonly Action<string, string> _showMessage;

        private string _username = "";
        private readonly List<string> _onlineUsers = new();

        public event Action<List<string>>? OnOnlineUsersChanged;

        public MessageRouter(
            ChannelManager channelManager,
            GalleryManager galleryManager,
            LightboxManager lightboxManager,
            Action<ChatMessage> openLightbox,
            Action<string, string> showMessage)
        {
            _channelManager = channelManager;
            _galleryManager = galleryManager;
            _lightboxManager = lightboxManager;
            _openLightbox = openLightbox;
            _showMessage = showMessage;
        }

        public void SetUsername(string username)
        {
            _username = username;
        }

        public void RouteIncomingMessage(string rawMessage)
        {
            var parts = rawMessage.Split('|');
            if (parts.Length < 1) return;

            string type = parts[0];

            switch (type)
            {
                case "MSG":
                    HandleTextMessage(parts);
                    break;
                case "FILE_READY":
                    HandleFileReady(parts);
                    break;
                case "CLEAR_CHAT":
                    _channelManager.AllMessages.Clear();
                    _channelManager.RefreshMessageList();
                    break;
                case "ROOM_NAME":
                    // Handled by ConnectionManager
                    break;
                case "GREETING":
                    // Handled by ConnectionManager
                    break;
                case "ONLINE_USERS":
                    HandleOnlineUsers(parts);
                    break;
                case "UPDATE_PROFILE":
                    // Broadcast profile update, no action needed on client
                    break;
            }
        }

        private void HandleTextMessage(string[] parts)
        {
            if (parts.Length < 3) return;
            string sender = parts[1];
            string content = parts[2];
            string avatar = parts.Length > 3 ? parts[3] : "";
            string time = parts.Length > 4 ? FormatTimestamp(parts[4]) : FormatTimestamp(DateTime.UtcNow.ToString("O"));
            bool isMe = (sender == _username || sender == "Me");

            var chatMsg = new ChatMessage
            {
                Sender = sender,
                Content = content,
                IsFile = false,
                AvatarBase64 = avatar,
                IsMe = isMe,
                Timestamp = time,
                RawDate = parts.Length > 4 && DateTime.TryParse(parts[4], out DateTime rdt) ? rdt : DateTime.UtcNow
            };

            _channelManager.AllMessages.Add(chatMsg);

            if (_channelManager.IsMessageInCurrentChannel(chatMsg))
            {
                _channelManager.RefreshMessageList();
            }
        }

        private void HandleFileReady(string[] parts)
        {
            if (parts.Length < 6) return;
            string fileId = parts[1];
            string fileName = parts[2];
            long size = long.Parse(parts[3]);
            string sender = parts[4];
            string avatar = parts[5];
            string time = parts.Length > 6 ? FormatTimestamp(parts[6]) : FormatTimestamp(DateTime.UtcNow.ToString("O"));
            bool isMe = (sender == _username || sender == "Me");

            var fileMsg = new ChatMessage
            {
                Sender = sender,
                Content = fileName,
                IsFile = true,
                FileId = fileId,
                FileSize = size,
                AvatarBase64 = avatar,
                IsMe = isMe,
                Timestamp = time,
                IsInImageChannel = _channelManager.CurrentChannel == "images",
                RawDate = parts.Length > 6 && DateTime.TryParse(parts[6], out DateTime rdt2) ? rdt2 : DateTime.UtcNow
            };

            _channelManager.AllMessages.Add(fileMsg);

            if (fileMsg.IsImage && string.IsNullOrEmpty(fileMsg.LocalFilePath))
            {
                // Trigger auto-download (handled by MainWindow)
                _openLightbox?.Invoke(fileMsg);
            }

            if (_channelManager.CurrentChannel == "images")
            {
                _galleryManager.RefreshImageGallery(_channelManager.AllMessages);
            }
            else if (_channelManager.CurrentChannel == "files")
            {
                _galleryManager.RefreshFileGallery(_channelManager.AllMessages);
            }
            else
            {
                _channelManager.RefreshMessageList();
            }
        }

        private void HandleOnlineUsers(string[] parts)
        {
            if (parts.Length > 1)
            {
                var users = parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                for (int i = 0; i < users.Count; i++)
                {
                    if (users[i] == _username)
                        users[i] += " (You)";
                }
                _onlineUsers.Clear();
                _onlineUsers.AddRange(users);
                OnOnlineUsersChanged?.Invoke(_onlineUsers);
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