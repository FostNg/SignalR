namespace LocalChat.Core.Contracts
{
    public interface IFileClient
    {
        event Action<double>? OnUploadProgress;
        event Action<double>? OnDownloadProgress;
        Task UploadFileAsync(string serverIp, string filePath, Guid fileId);
        Task DownloadFileAsync(string serverIp, string savePath, Guid fileId, long totalSize);
    }
}