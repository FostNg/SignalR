using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBox.Server
{
    public static class SignalRHost
    {
        private static WebApplication? _app;
        public static IHubContext<Hubs.ChatHub>? HubContext { get; private set; }

        public static async Task StartAsync(int port, CancellationToken token)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(port);
            });

            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB to allow large base64 avatars/images if needed
            });

            _app = builder.Build();
            HubContext = _app.Services.GetRequiredService<IHubContext<Hubs.ChatHub>>();
            _app.MapHub<Hubs.ChatHub>("/chat");

            await _app.RunAsync(token);
        }
    }
}
