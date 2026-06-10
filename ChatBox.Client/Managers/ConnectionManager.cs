using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LocalChat.Core.Contracts;

using Microsoft.AspNetCore.SignalR.Client;

namespace ChatBox.Client.Managers
{
    public class ConnectionManager
    {
        private HubConnection? _hubConnection;
        private readonly IFileClient _fileClient;
        private readonly MessageRouter _messageRouter;
        private readonly ChannelManager _channelManager;

        public string ServerIp { get; private set; } = "";
        public string UserId { get; private set; } = "";
        public string AvatarBase64 { get; private set; } = "";

        private CancellationTokenSource _cts = new();

        public event Action<string>? OnStatusChanged;
        public event Action<bool>? OnConnectionStateChanged;

        public ConnectionManager(
            IFileClient fileClient,
            MessageRouter messageRouter,
            ChannelManager channelManager)
        {
            _fileClient = fileClient;
            _messageRouter = messageRouter;
            _channelManager = channelManager;
        }

        public void SetUserIdentity(string userId, string avatarBase64)
        {
            UserId = userId;
            AvatarBase64 = avatarBase64;
        }

        public async Task ConnectAsync(string serverIp, string username)
        {
            ServerIp = serverIp;

            if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();

            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.DisposeAsync();
                }

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"http://{serverIp}:9999/chat")
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<string, string, string, string, string, string>("ReceiveMessage", (id, sender, content, avatar, time, reactions) =>
                {
                    _messageRouter.RouteIncomingMessage($"MSG|{sender}|{content}|{avatar}|{time}|{reactions}");
                });

                _hubConnection.On<string, string, long, string, string, string, string>("ReceiveFileReady", (fileId, fileName, size, sender, avatar, time, reactions) =>
                {
                    _messageRouter.RouteIncomingMessage($"FILE_READY|{fileId}|{fileName}|{size}|{sender}|{avatar}|{time}|{reactions}");
                });

                _hubConnection.On("ClearChat", () =>
                {
                    _messageRouter.RouteIncomingMessage("CLEAR_CHAT|");
                });

                _hubConnection.On<string, string>("ReceiveRoomInfo", (roomName, greeting) =>
                {
                    _messageRouter.RouteIncomingMessage($"ROOM_NAME|{roomName}");
                    _messageRouter.RouteIncomingMessage($"GREETING|{greeting}");
                });

                _hubConnection.On<string>("UpdateOnlineUsers", (users) =>
                {
                    _messageRouter.RouteIncomingMessage($"ONLINE_USERS|{users}");
                });

                _hubConnection.On<string>("UserTyping", (user) =>
                {
                    _messageRouter.RouteIncomingMessage($"TYPING|{user}");
                });

                await _hubConnection.StartAsync(_cts.Token);
                await _hubConnection.InvokeAsync("JoinChat", UserId, username, AvatarBase64);
                
                OnStatusChanged?.Invoke("Connected");
                OnConnectionStateChanged?.Invoke(true);
            }
            catch (OperationCanceledException)
            {
                OnStatusChanged?.Invoke("Cancelled");
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("Connection Failed");
                OnConnectionStateChanged?.Invoke(false);
                throw;
            }
        }

        public void CancelConnect()
        {
            _cts.Cancel();
            _ = _hubConnection?.StopAsync();
        }

        public void Disconnect()
        {
            _ = _hubConnection?.StopAsync();
            _cts.Cancel();
            _channelManager.AllMessages.Clear();
            _channelManager.RefreshMessageList();
            OnStatusChanged?.Invoke("Disconnected");
            OnConnectionStateChanged?.Invoke(false);
        }

        public async Task SendMessageAsync(string message)
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                // The old system used MSG|UserId|MsgId|Content
                // We'll parse it here and invoke the correct SignalR method
                var parts = message.Split('|');
                if (parts.Length > 0)
                {
                    if (parts[0] == "MSG" && parts.Length >= 4)
                    {
                        string userId = parts[1];
                        string clientMessageId = parts[2];
                        string content = string.Join("|", parts, 3, parts.Length - 3);
                        await _hubConnection.InvokeAsync("SendMessage", userId, clientMessageId, content);
                    }
                    else if (parts[0] == "TYPING")
                    {
                        await _hubConnection.InvokeAsync("SendTyping", parts[1]);
                    }
                    else if (parts[0] == "REACT" && parts.Length >= 4)
                    {
                        await _hubConnection.InvokeAsync("React", parts[1], parts[2], parts[3]);
                    }
                    else if (parts[0] == "UPDATE_PROFILE" && parts.Length >= 4)
                    {
                        await _hubConnection.InvokeAsync("UpdateProfile", parts[1], parts[2], parts[3]);
                    }
                }
            }
        }

        public void LoadUserConfig(string userId, string avatarBase64)
        {
            UserId = userId;
            AvatarBase64 = avatarBase64;
        }
    }
}