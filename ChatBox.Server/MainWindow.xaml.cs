using LocalChat.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ChatBox.Server
{
    public partial class MainWindow : Window
    {
        private LocalChat.Core.Services.FileServer _fileServer;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
            
            // Khởi tạo Database PostgreSQL
            try
            {
                using (var db = new LocalChat.Core.Data.ChatDbContext())
                {
                    db.Database.EnsureCreated();
                    Log("PostgreSQL Database connected and ready.");
                }
            }
            catch (System.Exception ex)
            {
                Log("Lỗi kết nối DB: " + ex.Message);
            }

            // Create ServerStorage dir in Local AppData
            string storageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LANChatBoxServer", "ServerStorage");
            if (!Directory.Exists(storageDir))
            {
                Directory.CreateDirectory(storageDir);
            }
            _fileServer = new FileServer(storageDir);

            // Detect local IPs
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                var ips = host.AddressList
                    .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .ToList();
                lblHostingIps.Text = ips.Count > 0 ? string.Join("\n", ips) : "127.0.0.1";
            }
            catch (Exception)
            {
                lblHostingIps.Text = "127.0.0.1";
            }

            // Load saved room name & welcome greeting from AppData
            string configPath = GetServerConfigPath();
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<ServerConfig>(json);
                    if (config != null)
                    {
                        Hubs.ChatHub.RoomName = config.RoomName;
                        txtRoomName.Text = config.RoomName;

                        Hubs.ChatHub.Greeting = config.Greeting;
                        txtGreeting.Text = config.Greeting;
                    }
                }
                catch {}
            }

            Hubs.ChatHub.OnLog += Log;
            _fileServer.OnLog += Log;

            _ = SignalRHost.StartAsync(9999, _cts.Token);
            _ = _fileServer.StartListeningAsync(_cts.Token);
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                lstLogs.Items.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
                lstLogs.ScrollIntoView(lstLogs.Items[lstLogs.Items.Count - 1]);
            });
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all chat history in the database?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new LocalChat.Core.Data.ChatDbContext();
                    db.ChatMessages.RemoveRange(db.ChatMessages);
                    db.SaveChanges();

                    Log("Cleared all chat history in the database.");
                    
                    if (SignalRHost.HubContext != null)
                    {
                        _ = Hubs.ChatHub.ClearChatAsync(SignalRHost.HubContext);
                    }
                }
                catch (Exception ex)
                {
                    Log("Error clearing DB: " + ex.Message);
                }
            }
        }

        public class ServerConfig
        {
            public string RoomName { get; set; } = "LAN Global Chat";
            public string Greeting { get; set; } = "Welcome to the server!";
        }

        private string GetServerConfigPath()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LANChatBoxServer");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            return Path.Combine(appData, "server_config.json");
        }

        private void SaveConfig()
        {
            try
            {
                var config = new ServerConfig
                {
                    RoomName = txtRoomName.Text,
                    Greeting = txtGreeting.Text
                };
                string configPath = GetServerConfigPath();
                File.WriteAllText(configPath, JsonSerializer.Serialize(config));
            }
            catch {}
        }

        private async void BtnUpdateRoomName_Click(object sender, RoutedEventArgs e)
        {
            if (SignalRHost.HubContext != null)
            {
                await Hubs.ChatHub.SetRoomInfoAsync(SignalRHost.HubContext, txtRoomName.Text, txtGreeting.Text);
                Log($"Changed room name to: {txtRoomName.Text}");
                SaveConfig();
            }
        }

        private async void BtnUpdateGreeting_Click(object sender, RoutedEventArgs e)
        {
            if (SignalRHost.HubContext != null)
            {
                await Hubs.ChatHub.SetRoomInfoAsync(SignalRHost.HubContext, Hubs.ChatHub.RoomName, txtGreeting.Text);
                Log($"Changed welcome greeting to: {txtGreeting.Text}");
                SaveConfig();
            }
        }

        private void BtnCopyIp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(lblHostingIps.Text);
                MessageBox.Show("Copied Hosting IP(s) to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to copy IP: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        protected override void OnClosed(System.EventArgs e)
        {
            _cts.Cancel();
            base.OnClosed(e);
        }
    }
}