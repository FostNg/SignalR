using LocalChat.Core.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChatBox.Client
{
    public class ChatMessage : System.ComponentModel.INotifyPropertyChanged
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
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
                string ext = System.IO.Path.GetExtension(Content).ToLower();
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

        private bool _isDraft;
        public bool IsDraft
        {
            get => _isDraft;
            set { _isDraft = value; OnPropertyChanged(nameof(IsDraft)); }
        }

        private System.Collections.Generic.List<Reaction> _reactions = new System.Collections.Generic.List<Reaction>();
        public System.Collections.Generic.List<Reaction> Reactions
        {
            get => _reactions;
            set { _reactions = value; OnPropertyChanged(nameof(Reactions)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

        public void RefreshReactions() => OnPropertyChanged(nameof(Reactions));
    }

    public class Reaction
    {
        public string Emoji { get; set; } = "";
        public int Count { get; set; }
        public string UserNames { get; set; } = "";
    }

    public partial class MainWindow : Window
    {
        private Microsoft.AspNetCore.SignalR.Client.HubConnection? _hubConnection;
        private FileClient _fileClient = new FileClient();
        private string _serverIp = "";
        private string _userId = "";
        private string _avatarBase64 = "";
        private string _displayName = "";
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ChatMessage? _currentTransferMessage;
        private ObservableCollection<ChatMessage> _pendingImages = new ObservableCollection<ChatMessage>();

        private System.Timers.Timer? _typingDebounceTimer;
        private System.Timers.Timer? _typingHideTimer;

        private ObservableCollection<ChatMessage> _allMessages = new ObservableCollection<ChatMessage>();
        private System.Collections.ObjectModel.ObservableCollection<ChatMessage> _chatViewMessages = new ObservableCollection<ChatMessage>();
        private string _currentChannel = "chat";

        public MainWindow()
        {
            InitializeComponent();
            LoadOrGenerateConfig();
            InitializeEmojis();

            // Bind staging panel to pending images list
            DraftStagingItems.ItemsSource = _pendingImages;
            UpdateDraftPanelVisibility();

            // Bind chat list once — ObservableCollection handles incremental UI updates
            lstChatMessages.ItemsSource = _chatViewMessages;

            _fileClient.OnUploadProgress += UpdateProgress;
            _fileClient.OnDownloadProgress += UpdateProgress;
        }

        public class UserConfig
        {
            public string UserId { get; set; } = "";
            public string Username { get; set; } = "";
            public string AvatarBase64 { get; set; } = "";
            public List<string> SavedIps { get; set; } = new List<string> { "127.0.0.1" };
        }

        private string GetConfigPath()
        {
            // Use %AppData% for config so it persists across app updates
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LANChatBox");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            return Path.Combine(appData, "user_config.json");
        }

        private void LoadOrGenerateConfig()
        {
            string configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                try 
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<UserConfig>(json);
                    if (config != null)
                    {
                        _userId = config.UserId;
                        txtUsername.Text = config.Username;
                        _avatarBase64 = config.AvatarBase64;

                        if (!string.IsNullOrEmpty(_avatarBase64))
                        {
                            var converter = new Converters.Base64ImageConverter();
                            var imgSource = (System.Windows.Media.ImageSource)converter.Convert(_avatarBase64, null, null, null);
                            imgAvatar.ImageSource = imgSource;
                            imgFooterAvatar.ImageSource = imgSource;
                        }
                        
                        if (config.SavedIps != null && config.SavedIps.Count > 0)
                        {
                            cboServerIp.ItemsSource = config.SavedIps;
                            cboServerIp.Text = config.SavedIps.First();
                        }

                        // Set footer details & initials fallback
                        string name = string.IsNullOrWhiteSpace(txtUsername.Text) ? "User" : txtUsername.Text.Trim();
                        lblFooterUsername.Text = name;
                        _displayName = name;
                        char initial = name.Length > 0 ? char.ToUpper(name[0]) : 'U';
                        lblAvatarInitials.Text = initial.ToString();
                        lblFooterInitials.Text = initial.ToString();
                        return;
                    }
                } 
                catch { }
            }
            
            _userId = Guid.NewGuid().ToString();
            SaveConfig();

            // Set default fallbacks for new user
            lblFooterUsername.Text = "User";
            lblAvatarInitials.Text = "U";
            lblFooterInitials.Text = "U";
        }

        private void SaveConfig()
        {
            var config = new UserConfig { 
                UserId = _userId, 
                Username = txtUsername.Text, 
                AvatarBase64 = _avatarBase64,
                SavedIps = (cboServerIp.ItemsSource as List<string>) ?? new List<string> { cboServerIp.Text }
            };
            File.WriteAllText(GetConfigPath(), JsonSerializer.Serialize(config));
        }

        private async void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Resize image to small base64 to avoid TCP buffer issues
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(dialog.FileName);
                    bitmap.DecodePixelWidth = 100; // Nhỏ gọn
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var ms = new MemoryStream();
                    encoder.Save(ms);
                    _avatarBase64 = Convert.ToBase64String(ms.ToArray());

                    var converter = new Converters.Base64ImageConverter();
                    var imgSource = (System.Windows.Media.ImageSource)converter.Convert(_avatarBase64, null, null, null);
                    imgAvatar.ImageSource = imgSource;
                    imgFooterAvatar.ImageSource = imgSource;

                    SaveConfig();
                    
                    if (pnlChat.IsEnabled && _hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected) // Nếu đang trong phòng chat
                    {
                        await _hubConnection.InvokeAsync("UpdateProfile", _userId, txtUsername.Text, _avatarBase64);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to change avatar! Please choose a valid and non-corrupted image.\nError: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cboServerIp.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter Server IP and Display Name!");
                return;
            }

            _serverIp = cboServerIp.Text.Trim();
            
            var ips = cboServerIp.ItemsSource as List<string> ?? new List<string> { "127.0.0.1" };
            if (!ips.Contains(_serverIp))
            {
                ips.Insert(0, _serverIp);
                cboServerIp.ItemsSource = null;
                cboServerIp.ItemsSource = ips;
                cboServerIp.Text = _serverIp;
            }

            SaveConfig();

            btnConnect.Visibility = Visibility.Collapsed;
            btnCancelConnect.Visibility = Visibility.Visible;
            btnDisconnect.Visibility = Visibility.Collapsed;

            lblStatus.Text = "Connecting...";
            lblStatus.Foreground = System.Windows.Media.Brushes.Orange;

            try
            {
                _allMessages.Clear();
                _chatViewMessages.Clear();
                if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();
                
                if (_hubConnection != null)
                {
                    await _hubConnection.DisposeAsync();
                }

                _hubConnection = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
                    .WithUrl($"http://{_serverIp}:9999/chat")
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<string, string, string, string, string, string>("ReceiveMessage", (id, sender, content, avatar, time, reactions) =>
                {
                    HandleIncomingMessage($"MSG|{id}|{sender}|{content}|{avatar}|{time}|{reactions}");
                });

                _hubConnection.On<string, string, long, string, string, string, string>("ReceiveFileReady", (fileId, fileName, size, sender, avatar, time, reactions) =>
                {
                    HandleIncomingMessage($"FILE_READY|{fileId}|{fileName}|{size}|{sender}|{avatar}|{time}|{reactions}");
                });

                _hubConnection.On("ClearChat", () =>
                {
                    HandleIncomingMessage("CLEAR_CHAT|");
                });

                _hubConnection.On<string, string>("ReceiveRoomInfo", (roomName, greeting) =>
                {
                    HandleIncomingMessage($"ROOM_NAME|{roomName}");
                    HandleIncomingMessage($"GREETING|{greeting}");
                });

                _hubConnection.On<string>("UpdateOnlineUsers", (users) =>
                {
                    HandleIncomingMessage($"ONLINE_USERS|{users}");
                });

                _hubConnection.On<string, string, string, string>("ReceiveProfileUpdate", (uid, newName, newAvatar, oldName) =>
                {
                    HandleIncomingMessage($"UPDATE_PROFILE|{uid}|{newName}|{newAvatar}|{oldName}");
                });

                _hubConnection.On<string, string>("ReceiveReactionUpdate", (msgId, reactionsJson) =>
                {
                    HandleIncomingMessage($"REACTION_UPDATE|{msgId}|{reactionsJson}");
                });

                _hubConnection.On<string>("UserTyping", (username) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        lblTypingIndicator.Text = $"{username} is typing...";
                        lblTypingIndicator.Visibility = Visibility.Visible;
                        
                        _typingHideTimer?.Stop();
                        _typingHideTimer = new System.Timers.Timer(2500);
                        _typingHideTimer.Elapsed += (s, ev) => 
                        {
                            Dispatcher.Invoke(() => lblTypingIndicator.Visibility = Visibility.Collapsed);
                        };
                        _typingHideTimer.AutoReset = false;
                        _typingHideTimer.Start();
                    });
                });

                await _hubConnection.StartAsync(_cts.Token);
                
                pnlChat.IsEnabled = true;
                lblStatus.Text = "Connected";
                lblStatus.Foreground = System.Windows.Media.Brushes.Black;
                
                // Active status indicators (Discord Green)
                var greenBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#23A55A"));
                statusIndicator.Fill = greenBrush;
                footerStatusDot.Fill = greenBrush;
                lblFooterStatus.Text = "Online";

                // Close settings overlay upon successful connection
                settingsOverlay.Visibility = Visibility.Collapsed;

                btnCancelConnect.Visibility = Visibility.Collapsed;
                btnDisconnect.Visibility = Visibility.Visible;

                // Gửi JOIN
                await _hubConnection.InvokeAsync("JoinChat", _userId, txtUsername.Text, _avatarBase64);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Cancelled";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F5660"));
                btnCancelConnect.Visibility = Visibility.Collapsed;
                btnConnect.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Connection Failed";
                lblStatus.Foreground = System.Windows.Media.Brushes.Red;
                var redBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ED4245"));
                statusIndicator.Fill = redBrush;
                footerStatusDot.Fill = redBrush;
                lblFooterStatus.Text = "Offline";

                btnCancelConnect.Visibility = Visibility.Collapsed;
                btnConnect.Visibility = Visibility.Visible;
                btnDisconnect.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Connection failed! The IP address '{_serverIp}' does not match any active server, or the server is not hosted on port 9999.\n\nError details: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelConnect_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            _ = _hubConnection?.StopAsync();
            
            btnCancelConnect.Visibility = Visibility.Collapsed;
            btnConnect.Visibility = Visibility.Visible;
            lblStatus.Text = "Disconnected";
            lblStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F5660"));
        }

        private void BtnClearIps_Click(object sender, RoutedEventArgs e)
        {
            cboServerIp.ItemsSource = new List<string> { "127.0.0.1" };
            cboServerIp.Text = "127.0.0.1";
            SaveConfig();
            MessageBox.Show("Saved IP history has been successfully cleared!", "History Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _ = _hubConnection?.StopAsync();
            _cts.Cancel();
            
            pnlChat.IsEnabled = false;
            _allMessages.Clear();
            _chatViewMessages.Clear();
            lstOnlineUsers.ItemsSource = null;
            
            lblStatus.Text = "Disconnected";
            lblStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F5660"));
            var grayBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#747F8D"));
            statusIndicator.Fill = grayBrush;
            footerStatusDot.Fill = grayBrush;
            lblFooterStatus.Text = "Offline";
            
            btnDisconnect.Visibility = Visibility.Collapsed;
            btnConnect.Visibility = Visibility.Visible;
        }

        private string FormatTimestamp(string timeString)
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

        private void HandleIncomingMessage(string rawMessage)
        {
            Dispatcher.Invoke(() =>
            {
                var parts = rawMessage.Split('|'); 
                if (parts.Length < 1) return;

                string type = parts[0];
                
                if (type == "MSG") // MSG|MessageId|Sender|Content|AvatarBase64|Timestamp|ReactionsJson
                {
                    if (parts.Length < 5) return;
                    string messageId = parts[1];
                    if (_allMessages.Any(m => m.MessageId == messageId)) return;
                    string sender = parts[2];
                    string content = parts[3];
                    string avatar = parts[4];
                    string time = parts.Length > 5 ? FormatTimestamp(parts[5]) : FormatTimestamp(DateTime.UtcNow.ToString("O"));
                    string reactionsJson = parts.Length > 6 ? parts[6] : "[]";
                    bool isMe = (sender == txtUsername.Text || sender == _displayName);
                    var chatMsg = new ChatMessage
                    {
                        MessageId = messageId,
                        Sender = sender,
                        Content = content,
                        IsFile = false,
                        AvatarBase64 = avatar,
                        IsMe = isMe,
                        Timestamp = time,
                        RawDate = DateTime.TryParse(parts[5], out DateTime rdt) ? rdt : DateTime.UtcNow
                    };
                    ParseReactionsToMessage(chatMsg, reactionsJson);
                    _allMessages.Add(chatMsg);

                    if (IsMessageInCurrentChannel(chatMsg))
                    {
                        _chatViewMessages.Add(chatMsg);
                        lstChatMessages.ScrollIntoView(chatMsg);
                    }
                }
                else if (type == "FILE_READY") // FILE_READY|FileId|FileName|Size|Sender|AvatarBase64|Timestamp|ReactionsJson
                {
                    if (parts.Length < 7) return;
                    string fileId = parts[1];
                    if (_allMessages.Any(m => m.MessageId == fileId)) return;
                    string fileName = parts[2];
                    long size = long.Parse(parts[3]);
                    string sender = parts[4];
                    string avatar = parts[5];
                    string time = parts.Length > 6 ? FormatTimestamp(parts[6]) : FormatTimestamp(DateTime.UtcNow.ToString("O"));
                    string reactionsJson = parts.Length > 7 ? parts[7] : "[]";
                    bool isMe = (sender == txtUsername.Text || sender == _displayName);

                    var fileMsg = new ChatMessage
                    {
                        MessageId = fileId,
                        Sender = sender,
                        Content = fileName,
                        IsFile = true,
                        FileId = fileId,
                        FileSize = size,
                        AvatarBase64 = avatar,
                        IsMe = isMe,
                        Timestamp = time,
                        IsInImageChannel = (_currentChannel == "images"),
                        RawDate = DateTime.TryParse(parts[6], out DateTime rdt2) ? rdt2 : DateTime.UtcNow
                    };
                    ParseReactionsToMessage(fileMsg, reactionsJson);
                    _allMessages.Add(fileMsg);

                    if (fileMsg.IsImage)
                    {
                        _ = AutoDownloadImageAsync(fileMsg);
                    }

                    if (IsMessageInCurrentChannel(fileMsg))
                    {
                        if (_currentChannel == "images")
                        {
                            RefreshImageGallery();
                        }
                        else if (_currentChannel == "files")
                        {
                            RefreshFileGallery();
                        }
                        else
                        {
                            _chatViewMessages.Add(fileMsg);
                            lstChatMessages.ScrollIntoView(fileMsg);
                        }
                    }
                }
                else if (type == "CLEAR_CHAT")
                {
                    _allMessages.Clear();
                    _chatViewMessages.Clear();
                }
                else if (type == "ROOM_NAME")
                {
                    if (parts.Length > 1)
                    {
                        string cleanRoomName = parts[1].ToLower().Replace(" ", "-");
                        lblRoomName.Text = "# " + cleanRoomName;
                        txtChanChat.Text = cleanRoomName;
                    }
                }
                else if (type == "GREETING")
                {
                    if (parts.Length > 1)
                    {
                        lblGreeting.Text = parts[1];
                    }
                }
                else if (type == "ONLINE_USERS")
                {
                    if (parts.Length > 1)
                    {
                        var users = parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        for (int i = 0; i < users.Count; i++)
                        {
                            if (users[i] == txtUsername.Text)
                                users[i] += " (You)";
                        }
                        lstOnlineUsers.ItemsSource = users;
                    }
                }
                else if (type == "UPDATE_PROFILE")
                {
                    // UPDATE_PROFILE|UserId|Username|AvatarBase64|OldUsername
                    // When another user updates their profile, update all their messages
                    if (parts.Length >= 4)
                    {
                        string updatedUserId = parts[1];
                        string newUsername = parts[2];
                        string newAvatar = parts[3];
                        string oldUsername = parts.Length >= 5 ? parts[4] : newUsername;

                        // Update all messages from this user using their old username
                        foreach (var msg in _allMessages.Where(m => m.Sender == oldUsername))
                        {
                            msg.Sender = newUsername;
                            msg.AvatarBase64 = newAvatar;
                        }

                        // Refresh UI if in chat channel
                        if (_currentChannel == "chat")
                        {
                            RefreshMessageList();
                        }
                    }
                }
                else if (type == "REACTION_UPDATE")
                {
                    // REACTION_UPDATE|MessageId|ReactionsJson
                    if (parts.Length >= 3)
                    {
                        string messageId = parts[1];
                        string reactionsJson = parts[2];

                        var targetMsg = _allMessages.FirstOrDefault(m => m.MessageId == messageId);
                        if (targetMsg != null)
                        {
                            try
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var reactionsList = JsonSerializer.Deserialize<System.Collections.Generic.List<Reaction>>(reactionsJson, options);
                                if (reactionsList != null)
                                {
                                    targetMsg.Reactions = reactionsList.Where(r => r.Count > 0).ToList();
                                    targetMsg.RefreshReactions();

                                    // Refresh UI if message is in current channel
                                    if (IsMessageInCurrentChannel(targetMsg))
                                    {
                                        RefreshMessageList();
                                    }
                                }
                            }
                            catch
                            {
                                // Invalid JSON, ignore
                            }
                        }
                    }
                }
            });
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            popupInfo.IsOpen = !popupInfo.IsOpen;
        }

        private void BtnEmoji_Click(object sender, RoutedEventArgs e)
        {
            popupEmoji.IsOpen = !popupEmoji.IsOpen;
        }

        private void InitializeEmojis()
        {
            string[] smileys = { 
                "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇", 
                "🙂", "🙃", "😉", "😌", "😍", "🥰", "😘", "😗", "😙", "😚", 
                "😋", "😛", "😝", "😜", "🤪", "🤨", "🧐", "🤓", "😎", "🥸", 
                "🤩", "🥳", "😏", "😒", "😞", "😔", "😟", "😕", "🙁", "☹️", 
                "😣", "😖", "😫", "😩", "🥺", "😢", "😭", "😤", "😠", "😡", 
                "🤬", "🤯", "😳", "🥵", "🥶", "😱", "😨", "😰", "😥", "😓", 
                "🤗", "🤔", "🫣", "🤭", "🤫", "🤥", "😶", "😐", "😑", "😬", 
                "🫠", "🫥", "😴", "🥱", "🤢", "🤮", "🤧", "😷", "🤒", "🤕", 
                "😈", "👿", "👹", "👺", "💀", "☠️", "👻", "👽", "👾", "🤖", 
                "💩", "👋", "🤚", "🖐️", "✋", "🖖", "👌", "🤌", "🤏", "✌️", 
                "🤞", "🫰", "🤟", "🤘", "🤙", "👈", "👉", "👆", "🖕", "👇", 
                "☝️", "👍", "👎", "✊", "👊", "🤛", "🤜", "👏", "🙌", "👐", 
                "🫶", "🤲", "🤝", "🙏", "✍️", "💅", "🤳", "💪", "🧠", "🫀", 
                "🫁", "🦷", "🦴", "👀", "👁️", "👅", "👄", "💋", "❤️", "🧡", 
                "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💔", "💖", "💗", 
                "💓", "💞", "💕", "💟", "❣️", "💘", "💝"
            };

            string[] animals = {
                "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯", 
                "🦁", "🐮", "🐷", "🐽", "🐸", "🐵", "🙈", "🙉", "🙊", "🐒", 
                "🐔", "🐧", "🐦", "🐤", "🐣", "🐥", "🦆", "🦅", "🦉", "🦇", 
                "🐺", "🐗", "🐴", "🦄", "🐝", "🪱", "🐛", "🦋", " snail", "🐌", 
                "🐞", "🐜", "🪰", "🪲", "🪳", "🦂", "🕸️", "🕷️", "🐢", "🐍", 
                "🦎", "🐙", "🦑", "lobster", "🦞", "🦀", "🐡", "🐠", "🐟", "🐬", 
                "🐳", "🐋", "🦈", "🐊", "🐅", "🐆", "🦓", "🦍", "🦧", "elephant", 
                "🐘", "🦛", "🦏", "🐪", "🐫", "🦒", "🦘", "🦬", "🐃", "🐂", 
                "🐄", "🐎", "🐖", "🐏", "🐑", "🦙", "🐐", "🦌", "🐕", "🐩", 
                "🐈", "🐈‍⬛", "🐇", "🐿️", "🦫", "🦔", "🦦", "🦥", "🦡", "🍁", 
                "🍂", "🍃", "🍄", "🌸", "💮", " Lotus", "🪷", "🌹", "🥀", "🌺", 
                "🌻", "🌼", "🌷", "🌱", "🪴", "🌲", "🌳", "🌴", "🌵", "🌾", 
                "🌿", "🍀"
            };

            string[] food = {
                "🍏", "🍎", "🍐", "🍊", "🍋", "🍌", "🍉", "🍇", "🍓", "🫐", 
                "🍒", "🍑", "🥭", "🍍", "🥥", "🥝", "🍅", "🍆", "🥑", "🥦", 
                "🥬", "🥒", "🌶️", "🫑", "🌽", "🥕", "🫒", "🧄", "🧅", "🥔", 
                "🍠", "🥐", "🥯", "🍞", "🥖", "🥨", "🧀", "🥚", "🍳", "🧈", 
                "🥞", "🧇", "🥓", "🥩", "🍗", "🍖", "🌭", "🍔", "🍟", "🍕", 
                "🥪", "🌮", "🌯", "🍲", "🥘", "🥣", "🥗", " popcorn", "🍿", "🧂", 
                "🥫", " Bento", "🍱", "🍘", "🍙", "🍚", "🍛", " noodles", "🍜", "🍝", 
                "🍢", "🍣", "🍤", "🍥", "🥮", "🍡", "🥟", "🥠", "🥡", "🍦", 
                "🍧", "🍨", "🍩", "🍪", "🎂", "🍰", "🧁", "🥧", "🍫", "🍬", 
                "🍭", "🍮", "🍯", "🍼", "🥛", "☕", "🍵", "🍶", "🍾", "🍷", 
                "🍸", "🍹", "🍺", "🍻", "🥂", "🥃"
            };

            string[] activities = {
                "⚽", "🏀", "🏈", "⚾", "🥎", "🎾", " volleyball", " volleyball", "🏐", "🏉", "🥏", "🎱", 
                "🪀", "🏓", "🏸", " Hockey", "🏒", "🏑", "🥍", " archery", "🏹", " Fishing", "🎣", "🤿", " boxing", "🥊", 
                "🥋", "🎽", " skateboard", "🛹", " roller", "🛼", " sled", "🛷", " ice", "⛸️", "🥌", " ski", "🎿", " snowboard", "🏂", "🪂", 
                "🏋️", " wrestlers", "🤼", " gymnastics", "🤸", " basketballer", "⛹️", " fencer", " fencing", "🤺", " handballer", "🤾", " golfer", "🏌️", " jockey", "🏇", " yoga", "🧘", " surfer", "🏄", 
                " swimmer", "🏊", " water", "🤽", " rower", "🚣", " climber", "🧗", " cyclist", "🚴", " biker", "🚵", "🏆", "🥇", "🥈", "🥉", 
                "🏅", "🎖️", "🎫", "🎟️", "🎭", "🎨", "🎬", " microphone", "🎤", " headphone", "🎧", "🎼", 
                "🎹", " drum", "🥁", "🪘", " sax", "🎷", " trumpet", "🎺", " guitar", "🎸", "🪕", " violin", "🎻", " dice", "🎲", " puzzle", "🧩", 
                " bowling", "🎳", "🎯", "🎮", "🕹️", "🎰", "👾", " chess", "♟️", " kite", " kite", "🪁", " Castle", "🏰", " Volcano", "🌋"
            };

            string[] travel = {
                "🚗", "🚕", "🚙", "🚌", "🚎", "🏎️", "🚓", "🚑", "🚒", "🚐", 
                "🛻", "🚚", "🚛", "🚜", "🛵", "🏍️", "🛺", "🚲", "🛴", "🚏", 
                "🛤️", "⚓", "⛵", "🛶", "🚤", "🛳️", "⛴️", "🚢", "✈️", "🛩️", 
                "🛫", "🛬", "🚡", "🚠", "🚟", "🚀", "🛸", "🚁", "🌍", "🌎", 
                "🌏", "🌐", "🗺️", "🗾", "🧭", "🏔️", "⛰️", "🗻", "🏕️", "🏖️", 
                "🏜️", "🏝️", "🏞️", "🏟️", "🏛️", "🏗️", "🧱", "🪨", "🪵", "🏠", 
                "🏡", "🏢", "🏣", "🏤", "🏥", "🏦", "🏨", "🏩", "🏪", "🏫", 
                "🏬", "🏭", "🏯", "💒", "🗼", "🗽", "⛩️", "🕋", "🕌", "🛕", 
                "🕍", "🛰️", "🇻🇳", "🇺🇸", "🇬🇧", "🇯🇵", "🇰🇷", "🇨🇳", "🇨🇦", "🚩"
            };

            string[] objects = {
                "💡", " flashlight", "🔦", " candle", "🕯️", "🪔", " plug", "🔌", " battery", "🔋", " laptop", "💻", "🖥️", " printer", "🖨️", " keyboard", "⌨️", 
                " mouse", "🖱️", " trackball", "🖲️", " minidisc", "💽", " floppy", "💾", " disc", "💿", " dvd", "📀", " abacus", "🧮", " movie", "🎥", " film", "🎞️", " projector", "📽️", 
                " tv", "📺", " camera", "📷", "📸", " video", "📹", " VHS", " VHS", "📼", " magnifying", "🔍", "🔎", " microscope", "🔬", " telescope", "🔭", " satellite", "📡", 
                " envelope", "✉️", "📩", "📨", "📧", "📥", "📤", " package", "📦", " tag", "🏷️", " ID", "🪪", " post", "📯", " postbox", "📮", " ballot", " ballot", "🗳️", " pencil", "✏️", " pen", "✒️", "🖋️", "🖊️", " paintbrush", "🖌️", " crayon", "🖍️", 
                " memo", "📝", " folder", "📁", "📂", " calendar", "📅", " calendar", "📆", " notepad", "🗒️", "🗓️", " bar", "📊", " chart", "📈", " chart", "📉", 
                " clipboard", "📋", " pin", "📌", " pin", "📍", " paperclip", "📎", " paperclips", "🖇️", " ruler", "📏", " triangle", "📐", " scissors", "✂️", " box", "🗃️", " cabinet", "🗄️", 
                " wastebasket", "🗑️", " lock", "🔒", " unlock", "🔓", " pen", "🔏", " key", "🔐", " key", "🔑", " key", "🗝️", " hammer", "🔨", " ax", "🪓", " pick", "⛏️", 
                " tools", "⚒️", " tools", "🛠️", " dagger", "🗡️", " swords", "⚔️", " pistol", "🔫", " shield", "🛡️", " wrench", "🔧", " screwdriver", "🪛", " gear", "⚙️", " clamp", "🗜️", 
                " scales", "⚖️", " link", "🔗", " chains", "⛓️", " crystal", "🔮", " rosary", "📿", " eye", "🧿", " bell", "敲钟", "bell", "🔔", "🔕", " bandage", "🩹",
                " DNA", "🧬", " thermometer", "🌡️", " test", "🧪", " petri", "🧫", " star", "⭐", " star", "🌟", " sparkles", "✨", " explosion", "💥"
            };

            AddEmojisToPanel(pnlSmileys, smileys);
            AddEmojisToPanel(pnlAnimals, animals);
            AddEmojisToPanel(pnlFood, food);
            AddEmojisToPanel(pnlActivities, activities);
            AddEmojisToPanel(pnlTravel, travel);
            AddEmojisToPanel(pnlObjects, objects);
        }

        private void AddEmojisToPanel(WrapPanel panel, string[] emojis)
        {
            panel.Children.Clear();
            foreach (var emoji in emojis)
            {
                // Clean up helper words that might have slipped into arrays
                string trimmed = emoji.Trim();
                if (trimmed.Length == 0 || trimmed.Any(c => char.IsLetterOrDigit(c))) continue;

                var btn = new Button
                {
                    Content = new Emoji.Wpf.TextBlock { Text = trimmed, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Width = 38,
                    Height = 38
                };
                
                btn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                    @"<ControlTemplate TargetType='Button' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                        <Border Background='{TemplateBinding Background}' CornerRadius='4'>
                            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                        </Border>
                      </ControlTemplate>");

                btn.Click += (s, e) =>
                {
                    // Append emoji text to RichTextBox properly
                    var range = new System.Windows.Documents.TextRange(
                        txtInput.Document.ContentEnd,
                        txtInput.Document.ContentEnd);
                    range.Text = trimmed;
                    txtInput.CaretPosition = txtInput.Document.ContentEnd;
                    if (lblInputPlaceholder != null)
                        lblInputPlaceholder.Visibility = Visibility.Collapsed;
                };
                panel.Children.Add(btn);
            }
        }

        private async void BtnSendChat_Click(object sender, RoutedEventArgs e)
        {
            string cleanText = GetInputText();

            // Send pending images first if any
            if (_pendingImages.Count > 0)
            {
                await SendPendingImagesAsync();
            }

            // Then send text message if there's text
            if (!string.IsNullOrWhiteSpace(cleanText))
            {
                string text = cleanText.Trim();
                if (txtInput is Emoji.Wpf.RichTextBox emojiBox) {
                    emojiBox.Text = "";
                } else {
                    txtInput.Document.Blocks.Clear();
                    txtInput.Document.Blocks.Add(new System.Windows.Documents.Paragraph());
                }

                var newMsg = new ChatMessage { MessageId = Guid.NewGuid().ToString(), Sender = string.IsNullOrWhiteSpace(_displayName) ? "User" : _displayName, Content = text, AvatarBase64 = _avatarBase64, IsMe = true, Timestamp = FormatTimestamp(DateTime.UtcNow.ToString("O")) };
                _allMessages.Add(newMsg);
                if (IsMessageInCurrentChannel(newMsg))
                {
                    _chatViewMessages.Add(newMsg);
                    lstChatMessages.ScrollIntoView(newMsg);
                }

                try
                {
                    if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                    {
                        await _hubConnection.InvokeAsync("SendMessage", _userId, newMsg.MessageId, text);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Lost connection to server! Your message could not be sent.");
                    BtnDisconnect_Click(null, null);
                }
            }
        }

        private async Task UploadFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

            var fileInfo = new FileInfo(filePath);
            Guid fileId = Guid.NewGuid();

            try
            {
                var msg = new ChatMessage
                {
                    MessageId = fileId.ToString(), // Match server message ID
                    Sender = string.IsNullOrWhiteSpace(_displayName) ? "User" : _displayName,
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
                    IsInImageChannel = (_currentChannel == "images")
                };
                _allMessages.Add(msg);
                if (IsMessageInCurrentChannel(msg))
                    _chatViewMessages.Add(msg);
                _currentTransferMessage = msg;

                if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("UpdateUploadStatus", _displayName, fileInfo.Name, "Uploading");
                }

                await _fileClient.UploadFileAsync(_serverIp, filePath, fileId);

                msg.IsTransferring = false;
                _currentTransferMessage = null;

                if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("UpdateUploadStatus", _displayName, fileInfo.Name, "Uploaded");
                    await _hubConnection.InvokeAsync("FileReady", _userId, fileId.ToString(), fileInfo.Name, fileInfo.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Upload error: " + ex.Message);
                if (_currentTransferMessage != null) _currentTransferMessage.IsTransferring = false;
            }
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                await UploadFileAsync(dialog.FileName);
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (pnlChat.IsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (pnlChat.IsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    // Upload all dropped files in parallel
                    await Task.WhenAll(files.Where(File.Exists).Select(f => UploadFileAsync(f)));
                }
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChatMessage msg)
            {
                string ext = System.IO.Path.GetExtension(msg.Content);
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = msg.Content,
                    DefaultExt = ext,
                    Filter = string.IsNullOrEmpty(ext) ? "All files (*.*)|*.*" : $"File type ({ext})|*{ext}|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    msg.IsTransferring = true;
                    msg.TransferProgress = 0;
                    _currentTransferMessage = msg;

                    try
                    {
                        if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync("UpdateDownloadStatus", _displayName, msg.Content, "Downloading");
                        }

                        await _fileClient.DownloadFileAsync(_serverIp, dialog.FileName, Guid.Parse(msg.FileId), msg.FileSize);
                        
                        if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync("UpdateDownloadStatus", _displayName, msg.Content, "Downloaded");
                        }
                        MessageBox.Show("Download complete!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Download error: " + ex.Message);
                    }
                    finally
                    {
                        msg.IsTransferring = false;
                        _currentTransferMessage = null;
                    }
                }
            }
        }

        private void UpdateProgress(double percent)
        {
            Dispatcher.Invoke(() => {
                if (_currentTransferMessage != null)
                {
                    _currentTransferMessage.TransferProgress = percent;
                }
            });
        }

        private void BtnShowSettings_Click(object sender, RoutedEventArgs e)
        {
            settingsOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseSettings_Click(object sender, RoutedEventArgs e)
        {
            settingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            _displayName = string.IsNullOrWhiteSpace(txtUsername.Text) ? "User" : txtUsername.Text.Trim();
            if (lblFooterUsername != null)
            {
                string name = _displayName;
                lblFooterUsername.Text = name;

                char initial = name.Length > 0 ? char.ToUpper(name[0]) : 'U';
                if (lblAvatarInitials != null) lblAvatarInitials.Text = initial.ToString();
                if (lblFooterInitials != null) lblFooterInitials.Text = initial.ToString();
            }
        }

        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lblInputPlaceholder != null)
            {
                // RichTextBox.Text includes \r\n even when empty — use TextRange instead
                string cleanText = GetInputText();
                lblInputPlaceholder.Visibility = string.IsNullOrEmpty(cleanText) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
            {
                if (_typingDebounceTimer == null)
                {
                    _typingDebounceTimer = new System.Timers.Timer(1000);
                    _typingDebounceTimer.AutoReset = false;
                    _typingDebounceTimer.Elapsed += (s, ev) => 
                    {
                        _typingDebounceTimer = null;
                    };
                    
                    _ = _hubConnection.InvokeAsync("SendTyping", _displayName);
                    _typingDebounceTimer.Start();
                }
            }
        }

        /// <summary>Get trimmed plain text from the emoji RichTextBox input.</summary>
        private string GetInputText()
        {
            try
            {
                var range = new System.Windows.Documents.TextRange(
                    txtInput.Document.ContentStart,
                    txtInput.Document.ContentEnd);
                return range.Text.Replace("\r", "").Replace("\n", "");
            }
            catch
            {
                return "";
            }
        }

        private void SelectChannel(string channelName)
        {
            _currentChannel = channelName;

            var activeBg = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E3E5E8"));
            var inactiveBg = System.Windows.Media.Brushes.Transparent;

            var activeText = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#060607"));
            var inactiveText = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F5660"));

            borderChanChat.Background = (channelName == "chat") ? activeBg : inactiveBg;
            txtChanChat.Foreground = (channelName == "chat") ? activeText : inactiveText;
            txtChanChat.FontWeight = (channelName == "chat") ? FontWeights.Bold : FontWeights.Normal;

            borderChanImages.Background = (channelName == "images") ? activeBg : inactiveBg;
            txtChanImages.Foreground = (channelName == "images") ? activeText : inactiveText;
            txtChanImages.FontWeight = (channelName == "images") ? FontWeights.Bold : FontWeights.Normal;

            borderChanFiles.Background = (channelName == "files") ? activeBg : inactiveBg;
            txtChanFiles.Foreground = (channelName == "files") ? activeText : inactiveText;
            txtChanFiles.FontWeight = (channelName == "files") ? FontWeights.Bold : FontWeights.Normal;

            foreach (var msg in _allMessages)
            {
                msg.IsInImageChannel = (channelName == "images");
            }

            // Clear input text on channel change to avoid drafts in non-interactive channels
            if (txtInput != null) txtInput.Text = "";

            if (channelName == "chat")
            {
                if (scrollImageGallery != null) scrollImageGallery.Visibility = Visibility.Collapsed;
                if (scrollFileGallery != null) scrollFileGallery.Visibility = Visibility.Collapsed;
                if (lstChatMessages != null) lstChatMessages.Visibility = Visibility.Visible;
                if (borderInputArea != null)
                {
                    borderInputArea.IsEnabled = true;
                    borderInputArea.Opacity = 1.0;
                }
                if (lblInputPlaceholder != null)
                {
                    lblInputPlaceholder.Text = "Message #lan-global-chat";
                    lblInputPlaceholder.Visibility = Visibility.Visible;
                }
                RefreshMessageList();
            }
            else if (channelName == "images")
            {
                if (scrollImageGallery != null) scrollImageGallery.Visibility = Visibility.Visible;
                if (scrollFileGallery != null) scrollFileGallery.Visibility = Visibility.Collapsed;
                if (lstChatMessages != null) lstChatMessages.Visibility = Visibility.Collapsed;
                if (borderInputArea != null)
                {
                    borderInputArea.IsEnabled = false;
                    borderInputArea.Opacity = 0.55;
                }
                if (lblInputPlaceholder != null)
                {
                    lblInputPlaceholder.Text = "Only images can be viewed in this channel";
                    lblInputPlaceholder.Visibility = Visibility.Visible;
                }
                RefreshImageGallery();
            }
            else if (channelName == "files")
            {
                if (scrollImageGallery != null) scrollImageGallery.Visibility = Visibility.Collapsed;
                if (scrollFileGallery != null) scrollFileGallery.Visibility = Visibility.Visible;
                if (lstChatMessages != null) lstChatMessages.Visibility = Visibility.Collapsed;
                if (borderInputArea != null)
                {
                    borderInputArea.IsEnabled = false;
                    borderInputArea.Opacity = 0.55;
                }
                if (lblInputPlaceholder != null)
                {
                    lblInputPlaceholder.Text = "Only files can be viewed in this channel";
                    lblInputPlaceholder.Visibility = Visibility.Visible;
                }
                RefreshFileGallery();
            }
        }

        private void ChanChat_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectChannel("chat");
        }

        private void ChanImages_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectChannel("images");
        }

        private void ChanFiles_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectChannel("files");
        }

        private void UpdateDraftPanelVisibility()
        {
            DraftStagingPanel.Visibility = _pendingImages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshMessageList()
        {
            _chatViewMessages.Clear();
            foreach (var msg in _allMessages)
            {
                if (IsMessageInCurrentChannel(msg))
                    _chatViewMessages.Add(msg);
            }
            if (_chatViewMessages.Count > 0)
                lstChatMessages.ScrollIntoView(_chatViewMessages[_chatViewMessages.Count - 1]);
        }

        private bool IsMessageInCurrentChannel(ChatMessage msg)
        {
            if (_currentChannel == "chat")
            {
                return true;
            }
            else if (_currentChannel == "images")
            {
                if (!msg.IsFile) return false;
                string ext = System.IO.Path.GetExtension(msg.Content).ToLower();
                return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
            }
            else if (_currentChannel == "files")
            {
                if (!msg.IsFile) return false;
                string ext = System.IO.Path.GetExtension(msg.Content).ToLower();
                bool isImg = ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
                return !isImg;
            }
            return false;
        }

        private async Task AutoDownloadImageAsync(ChatMessage fileMsg)
        {
            try
            {
                string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LANChatBox", "ClientCache");
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                string cachedPath = Path.Combine(cacheDir, fileMsg.FileId + "_" + fileMsg.Content);
                if (File.Exists(cachedPath))
                {
                    fileMsg.LocalFilePath = cachedPath;
                    return;
                }

                fileMsg.IsTransferring = true;
                fileMsg.TransferProgress = 0;

                var tempClient = new FileClient();
                tempClient.OnDownloadProgress += (pct) => 
                {
                    Dispatcher.Invoke(() => {
                        fileMsg.TransferProgress = pct;
                    });
                };

                if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("UpdateDownloadStatus", _displayName, fileMsg.Content, "Downloading");
                }

                await tempClient.DownloadFileAsync(_serverIp, cachedPath, Guid.Parse(fileMsg.FileId), fileMsg.FileSize);
                fileMsg.LocalFilePath = cachedPath;

                if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("UpdateDownloadStatus", _displayName, fileMsg.Content, "Downloaded");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Background auto-download error: " + ex.Message);
            }
            finally
            {
                fileMsg.IsTransferring = false;
            }
        }

        public class ImageGroup
        {
            public string DateHeader { get; set; } = "";
            public System.Collections.ObjectModel.ObservableCollection<ChatMessage> Images { get; set; } = new();
        }

        public class FileGroup
        {
            public string DateHeader { get; set; } = "";
            public System.Collections.ObjectModel.ObservableCollection<ChatMessage> Files { get; set; } = new();
        }

        private void RefreshImageGallery()
        {
            var imageMessages = _allMessages.Where(m => m.IsImage).ToList();
            
            // Group by local date
            var groups = imageMessages.GroupBy(m => m.RawDate.ToLocalTime().Date)
                                      .OrderByDescending(g => g.Key);

            var list = new System.Collections.ObjectModel.ObservableCollection<ImageGroup>();
            foreach (var g in groups)
            {
                string header = "";
                if (g.Key == DateTime.Today)
                {
                    header = "Today";
                }
                else if (g.Key == DateTime.Today.AddDays(-1))
                {
                    header = "Yesterday";
                }
                else
                {
                    header = g.Key.ToString("MMMM dd, yyyy");
                }

                var imgGroup = new ImageGroup { DateHeader = header };
                foreach (var m in g.OrderBy(msg => msg.RawDate))
                {
                    imgGroup.Images.Add(m);
                }
                list.Add(imgGroup);
            }

            if (itemsImageGallery != null)
            {
                itemsImageGallery.ItemsSource = list;
            }
        }

        private void RefreshFileGallery()
        {
            var fileMessages = _allMessages.Where(m => m.IsFile && !m.IsImage).ToList();
            
            // Group by local date
            var groups = fileMessages.GroupBy(m => m.RawDate.ToLocalTime().Date)
                                     .OrderByDescending(g => g.Key);

            var list = new System.Collections.ObjectModel.ObservableCollection<FileGroup>();
            foreach (var g in groups)
            {
                string header = "";
                if (g.Key == DateTime.Today)
                {
                    header = "Today";
                }
                else if (g.Key == DateTime.Today.AddDays(-1))
                {
                    header = "Yesterday";
                }
                else
                {
                    header = g.Key.ToString("MMMM dd, yyyy");
                }

                var fg = new FileGroup { DateHeader = header };
                foreach (var m in g.OrderBy(msg => msg.RawDate))
                {
                    fg.Files.Add(m);
                }
                list.Add(fg);
            }

            if (itemsFileGallery != null)
            {
                itemsFileGallery.ItemsSource = list;
            }
        }

        private ChatMessage? _lightboxCurrentMessage;

        private void ChatImage_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ChatMessage msg)
            {
                OpenLightbox(msg);
            }
        }

        private void GalleryImage_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ChatMessage msg)
            {
                OpenLightbox(msg);
            }
        }

        private void OpenLightbox(ChatMessage msg)
        {
            if (string.IsNullOrEmpty(msg.LocalFilePath) || !File.Exists(msg.LocalFilePath))
            {
                return;
            }

            _lightboxCurrentMessage = msg;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(msg.LocalFilePath);
                bitmap.EndInit();
                imgLightboxLarge.Source = bitmap;
            }
            catch
            {
                imgLightboxLarge.Source = null;
            }

            try
            {
                var conv = new Converters.Base64ImageConverter();
                imgLightboxAvatar.ImageSource = (System.Windows.Media.ImageSource)conv.Convert(msg.AvatarBase64, typeof(System.Windows.Media.ImageSource), null, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                imgLightboxAvatar.ImageSource = null;
            }

            lblLightboxSender.Text = msg.Sender == "Me" ? (string.IsNullOrWhiteSpace(txtUsername.Text) ? "User" : txtUsername.Text) : msg.Sender;
            lblLightboxTime.Text = msg.Timestamp;

            lightboxOverlay.Visibility = Visibility.Visible;
        }

        private void CloseLightbox_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            lightboxOverlay.Visibility = Visibility.Collapsed;
        }

        private void CloseLightbox_Click(object sender, RoutedEventArgs e)
        {
            lightboxOverlay.Visibility = Visibility.Collapsed;
        }

        private async void LightboxDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_lightboxCurrentMessage == null || string.IsNullOrEmpty(_lightboxCurrentMessage.LocalFilePath)) return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.FileName = _lightboxCurrentMessage.Content;
            saveDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All Files|*.*";

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                    {
                        await _hubConnection.InvokeAsync("UpdateDownloadStatus", _displayName, _lightboxCurrentMessage.Content, "Downloading");
                    }

                    File.Copy(_lightboxCurrentMessage.LocalFilePath, saveDialog.FileName, true);

                    if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                    {
                        await _hubConnection.InvokeAsync("UpdateDownloadStatus", _displayName, _lightboxCurrentMessage.Content, "Downloaded");
                    }

                    MessageBox.Show("Downloaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Download failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SmoothScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is DependencyObject dobj)
            {
                var scrollViewer = dobj as ScrollViewer;
                if (scrollViewer == null)
                {
                    scrollViewer = FindVisualChild<ScrollViewer>(dobj);
                }

                if (scrollViewer != null)
                {
                    // If CanContentScroll is true, we are scrolling by item index, not pixels.
                    // So we must use a small step like 1 or 2 items, rather than 38.
                    double step = scrollViewer.CanContentScroll ? 1.0 : 38.0;
                    double targetOffset = scrollViewer.VerticalOffset - (Math.Sign(e.Delta) * step);
                    
                    if (targetOffset < 0) targetOffset = 0;
                    if (targetOffset > scrollViewer.ScrollableHeight) targetOffset = scrollViewer.ScrollableHeight;

                    // Immediately perform a clean pixel-by-pixel or item-by-item scrolling transition
                    scrollViewer.ScrollToVerticalOffset(targetOffset);
                    e.Handled = true;
                }
            }
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t)
                {
                    return t;
                }
                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private void GalleryFile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ChatMessage msg)
            {
                var dummyButton = new Button { Tag = msg };
                BtnDownload_Click(dummyButton, new RoutedEventArgs());
            }
        }

        private async void TxtInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var isControl = System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control);
            var isAlt = System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt);

            if (isControl || isAlt)
            {
                bool isAllowed = false;
                if (isControl && !isAlt)
                {
                    if (e.Key == System.Windows.Input.Key.C ||
                        e.Key == System.Windows.Input.Key.V ||
                        e.Key == System.Windows.Input.Key.A ||
                        e.Key == System.Windows.Input.Key.X)
                    {
                        isAllowed = true;
                    }
                }

                if (!isAllowed)
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                {
                    return;
                }
                e.Handled = true;
                await SendPendingImagesAsync();
                BtnSendChat_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == System.Windows.Input.Key.V && isControl)
            {
                if (Clipboard.ContainsImage())
                {
                    e.Handled = true;
                    HandleImagePaste();
                }
            }
        }

        private void HandleImagePaste()
        {
            try
            {
                if (_pendingImages.Count >= 5)
                {
                    MessageBox.Show("Maximum 5 images pending. Send or remove some first.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var image = Clipboard.GetImage();
                if (image == null) return;

                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempPaste");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                string fileName = $"ClipboardImage_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(tempDir, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fileStream);
                }

                Guid fileId = Guid.NewGuid();
                var fileInfo = new FileInfo(filePath);

                var msg = new ChatMessage
                {
                    MessageId = fileId.ToString(), // Use same ID as FileId so server round-trip matches
                    Sender = _displayName,
                    Content = fileInfo.Name,
                    IsFile = true,
                    FileId = fileId.ToString(),
                    FileSize = fileInfo.Length,
                    AvatarBase64 = _avatarBase64,
                    IsTransferring = false,
                    IsMe = true,
                    Timestamp = FormatTimestamp(DateTime.UtcNow.ToString("O")),
                    LocalFilePath = filePath,
                    IsInImageChannel = (_currentChannel == "images"),
                    IsDraft = true
                };

                // Add to staging panel only (not message list yet)
                _pendingImages.Add(msg);
                UpdateDraftPanelVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to paste image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool _isSendingPendingImages = false;

        private async Task SendPendingImagesAsync()
        {
            if (_pendingImages.Count == 0 || _isSendingPendingImages) return;

            _isSendingPendingImages = true;
            try
            {
                var toSend = _pendingImages.ToList();
                _pendingImages.Clear();
                UpdateDraftPanelVisibility();

                // Mark all as in-flight and add to view immediately (Optimistic UI)
                foreach (var msg in toSend)
                {
                    msg.IsDraft = false;
                    msg.IsTransferring = true;
                    _allMessages.Add(msg);
                    if (IsMessageInCurrentChannel(msg))
                        _chatViewMessages.Add(msg);
                }

                // Upload all in parallel — no more sequential blocking
                await Task.WhenAll(toSend.Select(async msg =>
                {
                    try
                    {
                        await _fileClient.UploadFileAsync(_serverIp, msg.LocalFilePath, Guid.Parse(msg.FileId));
                        msg.IsTransferring = false;
                        if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync("FileReady", _userId, msg.FileId, msg.Content, msg.FileSize);
                        }
                    }
                    catch (Exception ex)
                    {
                        msg.IsTransferring = false;
                        Dispatcher.Invoke(() => MessageBox.Show($"Failed to upload image {msg.Content}: {ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                }));
            }
            finally
            {
                _isSendingPendingImages = false;
            }
        }

        private void Topbar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximize();
                }
                else
                {
                    this.DragMove();
                }
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void ReactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string emoji)
            {
                // QuickReactionBar is inside the ListBoxItem ControlTemplate,
                // so btn.DataContext is NOT the ChatMessage. Walk up to find ListBoxItem.
                ChatMessage? msg = null;
                DependencyObject? current = btn;
                while (current != null)
                {
                    current = VisualTreeHelper.GetParent(current);
                    if (current is ListBoxItem lbi && lbi.DataContext is ChatMessage m)
                    {
                        msg = m;
                        break;
                    }
                }

                if (msg == null) return;

                // Toggle reaction - add or remove
                var existingReaction = msg.Reactions.FirstOrDefault(r => r.Emoji == emoji);
                if (existingReaction != null)
                {
                    existingReaction.Count--;
                    existingReaction.UserNames = existingReaction.UserNames.Replace(_displayName, "").Trim();
                    if (existingReaction.Count <= 0)
                    {
                        msg.Reactions.Remove(existingReaction);
                    }
                }
                else
                {
                    msg.Reactions.Add(new Reaction { Emoji = emoji, Count = 1, UserNames = _displayName });
                }
                msg.Reactions = msg.Reactions.ToList(); // Recreate list to trigger ItemsControl refresh
                msg.RefreshReactions();

                // Send REACT message to server
                try
                {
                    if (_hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                    {
                        await _hubConnection.InvokeAsync("React", _userId, msg.MessageId, emoji);
                    }
                }
                catch
                {
                    // Silently fail - local state already updated
                }
            }
        }

        private void ParseReactionsToMessage(ChatMessage msg, string reactionsJson)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var reactionsList = JsonSerializer.Deserialize<System.Collections.Generic.List<Reaction>>(reactionsJson, options);
                if (reactionsList != null)
                {
                    msg.Reactions = reactionsList.Where(r => r.Count > 0).ToList();
                }
            }
            catch
            {
                // Invalid JSON, ignore
            }
        }

        private void ReactionTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ChatMessage msg)
            {
                // Find the popup in the visual tree and open it
                var parent = btn.Parent;
                while (parent != null && !(parent is Grid))
                {
                    parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
                }
                if (parent is Grid grid)
                {
                    var popup = grid.FindName("ReactionPopup") as Popup;
                    if (popup != null)
                    {
                        popup.DataContext = msg;
                        popup.IsOpen = true;
                    }
                }
            }
        }

        private void BtnRemovePending_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChatMessage msg)
            {
                _pendingImages.Remove(msg);
                UpdateDraftPanelVisibility();
            }
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts.Cancel();
            base.OnClosed(e);
        }
    }
}