using LocalChat.Core.Data;
using LocalChat.Core.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatBox.Server.Hubs
{
    public class ChatHub : Hub
    {
        // ConnectionId -> Username
        private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();
        public static string RoomName { get; set; } = "LAN Global Chat";
        public static string Greeting { get; set; } = "Welcome to the server!";

        public static event Action<string>? OnLog;

        public override async Task OnConnectedAsync()
        {
            OnLog?.Invoke($"Client connected via SignalR: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_onlineUsers.TryRemove(Context.ConnectionId, out string? username))
            {
                var msgId = Guid.NewGuid().ToString();
                var time = DateTime.UtcNow;
                await Clients.All.SendAsync("ReceiveMessage", msgId, "System", $"{username} has left the chat.", "", time.ToString("O"), "[]");
                await BroadcastOnlineUsersAsync();
            }
            OnLog?.Invoke($"Client disconnected via SignalR: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        private async Task BroadcastOnlineUsersAsync()
        {
            var users = string.Join(",", _onlineUsers.Values.Distinct());
            await Clients.All.SendAsync("UpdateOnlineUsers", users);
        }

        public async Task JoinChat(string userId, string username, string avatar)
        {
            using var db = new ChatDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user == null)
            {
                user = new User { Id = userId, Username = username, AvatarBase64 = avatar };
                db.Users.Add(user);
            }
            else
            {
                user.Username = username;
                user.AvatarBase64 = avatar;
                user.LastSeen = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();

            // Send history only to caller
            var history = await db.ChatMessages
                .Include(m => m.Sender)
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .ToListAsync();
            history.Reverse();

            var messageIds = history.Select(m => m.Id).ToList();
            var allReactions = await db.MessageReactions
                .Include(r => r.User)
                .Where(r => messageIds.Contains(r.MessageId))
                .ToListAsync();

            await Clients.Caller.SendAsync("ReceiveRoomInfo", RoomName, Greeting);

            foreach (var msg in history)
            {
                if (msg.Sender == null) continue;
                var msgReactions = allReactions.Where(r => r.MessageId == msg.Id).ToList();
                var reactionsJson = SerializeReactions(msgReactions);

                if (msg.IsFile)
                {
                    await Clients.Caller.SendAsync("ReceiveFileReady", msg.FileId, msg.Content, msg.FileSize, msg.Sender.Username, msg.Sender.AvatarBase64, msg.Timestamp.ToString("O"), reactionsJson);
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", msg.Id, msg.Sender.Username, msg.Content, msg.Sender.AvatarBase64, msg.Timestamp.ToString("O"), reactionsJson);
                }
            }

            // Broadcast join
            var msgId = Guid.NewGuid().ToString();
            var time = DateTime.UtcNow;
            await Clients.All.SendAsync("ReceiveMessage", msgId, "System", $"{username} has joined the chat.", "", time.ToString("O"), "[]");
            OnLog?.Invoke($"[JOIN] {username}");

            _onlineUsers[Context.ConnectionId] = username;
            await BroadcastOnlineUsersAsync();
        }

        public async Task SendMessage(string userId, string clientMessageId, string content)
        {
            using var db = new ChatDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                var msg = new ChatMessage { Id = clientMessageId, SenderId = userId, Content = content, IsFile = false, Timestamp = DateTime.UtcNow };
                db.ChatMessages.Add(msg);
                await db.SaveChangesAsync();

                await Clients.All.SendAsync("ReceiveMessage", msg.Id, user.Username, content, user.AvatarBase64, msg.Timestamp.ToString("O"), "[]");
                OnLog?.Invoke($"[MSG] {user.Username}: {content}");
            }
        }

        public async Task FileReady(string userId, string fileId, string fileName, long size)
        {
            using var db = new ChatDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                var msg = new ChatMessage { Id = fileId, SenderId = userId, Content = fileName, IsFile = true, FileId = fileId, FileSize = size, Timestamp = DateTime.UtcNow };
                db.ChatMessages.Add(msg);
                await db.SaveChangesAsync();

                await Clients.All.SendAsync("ReceiveFileReady", fileId, fileName, size, user.Username, user.AvatarBase64, msg.Timestamp.ToString("O"), "[]");
                OnLog?.Invoke($"[FILE] {user.Username}: {fileName}");
            }
        }

        public async Task UpdateProfile(string userId, string username, string avatar)
        {
            using var db = new ChatDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                string oldUsername = user.Username ?? "";
                user.Username = username;
                user.AvatarBase64 = avatar;
                await db.SaveChangesAsync();
                
                OnLog?.Invoke($"[PROFILE] {oldUsername} updated their profile to {username}.");
                _onlineUsers[Context.ConnectionId] = username;
                
                await Clients.All.SendAsync("ReceiveProfileUpdate", userId, username, avatar, oldUsername);
                await BroadcastOnlineUsersAsync();
            }
        }

        public async Task React(string userId, string messageId, string emoji)
        {
            if (string.IsNullOrWhiteSpace(emoji) || emoji.Length > 32) return;
            
            using var db = new ChatDbContext();
            var user = await db.Users.FindAsync(userId);
            var chatMessage = await db.ChatMessages.FindAsync(messageId);
            if (user == null || chatMessage == null) return;

            var existingReaction = await db.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji);

            if (existingReaction != null)
            {
                db.MessageReactions.Remove(existingReaction);
                await db.SaveChangesAsync();
                OnLog?.Invoke($"[REACT] {user.Username} removed reaction {emoji} from {messageId}");
            }
            else
            {
                db.MessageReactions.Add(new MessageReaction { MessageId = messageId, UserId = userId, Emoji = emoji, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync();
                OnLog?.Invoke($"[REACT] {user.Username} reacted {emoji} to {messageId}");
            }

            var reactions = await db.MessageReactions.Include(r => r.User).Where(r => r.MessageId == messageId).ToListAsync();
            await Clients.All.SendAsync("ReceiveReactionUpdate", messageId, SerializeReactions(reactions));
        }

        public async Task SendTyping(string username)
        {
            await Clients.Others.SendAsync("UserTyping", username);
        }
        
        public static async Task ClearChatAsync(IHubContext<ChatHub> context)
        {
            await context.Clients.All.SendAsync("ClearChat");
        }
        
        public static async Task SetRoomInfoAsync(IHubContext<ChatHub> context, string roomName, string greeting)
        {
            RoomName = roomName;
            Greeting = greeting;
            await context.Clients.All.SendAsync("ReceiveRoomInfo", roomName, greeting);
        }

        private string SerializeReactions(IEnumerable<MessageReaction> reactions)
        {
            var dtos = reactions.GroupBy(r => r.Emoji).Select(g => new
            {
                Emoji = g.Key,
                Count = g.Count(),
                UserNames = string.Join(", ", g.Where(r => r.User != null).Select(r => r.User!.Username))
            }).ToList();
            return JsonSerializer.Serialize(dtos);
        }
    }
}
