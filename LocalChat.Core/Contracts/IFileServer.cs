namespace LocalChat.Core.Contracts
{
    public interface IFileServer
    {
        event Action<string>? OnLog;
        Task StartListeningAsync(CancellationToken token);
    }
}