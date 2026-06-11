using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LocalChat.Core.Services
{
    public class FileServer
    {
        private const int DataPort = 10000;
        private string _storageDir;
        
        public event Action<string>? OnLog;

        public FileServer(string storageDir)
        {
            _storageDir = storageDir;
            Directory.CreateDirectory(_storageDir);
        }

        public async Task StartListeningAsync(CancellationToken token)
        {
            var listener = new TcpListener(IPAddress.Any, DataPort);
            listener.Start();
            OnLog?.Invoke($"Server File Data started on port {DataPort}");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClientAsync(client, token));
                }
            }
            finally { listener.Stop(); }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    // Header: Action(1 byte) + FileId(16 bytes) + Offset(8 bytes) + Length(4 bytes) + TotalSize(8 bytes) = 37 bytes
                    byte[] header = new byte[37];
                    await stream.ReadExactlyAsync(header, 0, 37, token);
                    
                    byte action = header[0];
                    Guid fileId = new Guid(new ReadOnlySpan<byte>(header, 1, 16));
                    long offset = BitConverter.ToInt64(header, 17);
                    int length = BitConverter.ToInt32(header, 25);
                    long totalSize = BitConverter.ToInt64(header, 29);

                    string filePath = Path.Combine(_storageDir, fileId.ToString() + ".dat");

                    if (action == 0) // UPLOAD (Client -> Server)
                    {
                        byte[] data = new byte[length];
                        await stream.ReadExactlyAsync(data, 0, length, token);

                        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 4096, true))
                        {
                            try { if (fs.Length < totalSize) fs.SetLength(totalSize); } catch { /* Ignore race conditions on SetLength */ }
                            fs.Seek(offset, SeekOrigin.Begin);
                            await fs.WriteAsync(data, token);
                        }
                        
                        // Send ACK so client knows chunk is safely on disk
                        await stream.WriteAsync(new byte[] { 1 }, 0, 1, token);
                    }
                    else if (action == 1) // DOWNLOAD (Server -> Client)
                    {
                        byte[] buffer = new byte[81920];
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true))
                        {
                            fs.Seek(offset, SeekOrigin.Begin);
                            int remaining = length;
                            while (remaining > 0)
                            {
                                int toRead = Math.Min(buffer.Length, remaining);
                                int bytesRead = await fs.ReadAsync(buffer, 0, toRead, token);
                                if (bytesRead == 0) break;
                                await stream.WriteAsync(buffer, 0, bytesRead, token);
                                remaining -= bytesRead;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[Server File Error] {ex.Message}");
                }
            }
        }
    }

    public class FileClient
    {
        private const int DataPort = 10000;
        private const int ChunkSize = 4 * 1024 * 1024;

        public event Action<double>? OnUploadProgress;
        public event Action<double>? OnDownloadProgress;

        public async Task UploadFileAsync(string serverIp, string filePath, Guid fileId, Action<double>? onProgress = null)
        {
            var fileInfo = new FileInfo(filePath);
            long totalSize = fileInfo.Length;
            int totalChunks = (int)Math.Ceiling((double)totalSize / ChunkSize);
            long uploadedBytes = 0;

            await Parallel.ForAsync(0, totalChunks, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (i, token) =>
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(serverIp, DataPort, token);
                    using var stream = client.GetStream();

                    long offset = (long)i * ChunkSize;
                    byte[] buffer = new byte[ChunkSize];

                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true))
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        
                        int expectedToRead = (int)Math.Min((long)ChunkSize, totalSize - offset);
                        int totalBytesRead = 0;
                        while (totalBytesRead < expectedToRead)
                        {
                            int bytesRead = await fs.ReadAsync(buffer, totalBytesRead, expectedToRead - totalBytesRead, token);
                            if (bytesRead == 0) break;
                            totalBytesRead += bytesRead;
                        }

                        // Build header
                        byte[] header = new byte[37];
                        header[0] = 0; // Upload
                        fileId.ToByteArray().CopyTo(header, 1);
                        BitConverter.GetBytes(offset).CopyTo(header, 17);
                        BitConverter.GetBytes(totalBytesRead).CopyTo(header, 25);
                        BitConverter.GetBytes(totalSize).CopyTo(header, 29);

                        await stream.WriteAsync(header, token);
                        await stream.WriteAsync(buffer.AsMemory(0, totalBytesRead), token);
                        
                        // Wait for server to finish writing to disk
                        byte[] ack = new byte[1];
                        await stream.ReadExactlyAsync(ack, 0, 1, token);

                        long currentUploaded = Interlocked.Add(ref uploadedBytes, totalBytesRead);
                        double pct = (double)currentUploaded / totalSize * 100;
                        OnUploadProgress?.Invoke(pct);
                        onProgress?.Invoke(pct);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Upload chunk error: {ex.Message}");
                    throw new Exception($"[Sender] Upload failed: {ex.Message}", ex);
                }
            });
        }

        public async Task DownloadFileAsync(string serverIp, string savePath, Guid fileId, long totalSize, Action<double>? onProgress = null)
        {
            int totalChunks = (int)Math.Ceiling((double)totalSize / ChunkSize);
            long downloadedBytes = 0;
            
            // Ensure file exists with correct size
            using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true))
            {
                fs.SetLength(totalSize);
            }

            await Parallel.ForAsync(0, totalChunks, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (i, token) =>
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(serverIp, DataPort, token);
                    using var stream = client.GetStream();

                    long offset = (long)i * ChunkSize;
                    int length = (int)Math.Min((long)ChunkSize, totalSize - offset);

                    // Build header
                    byte[] header = new byte[37];
                    header[0] = 1; // Download
                    fileId.ToByteArray().CopyTo(header, 1);
                    BitConverter.GetBytes(offset).CopyTo(header, 17);
                    BitConverter.GetBytes(length).CopyTo(header, 25);
                    BitConverter.GetBytes(totalSize).CopyTo(header, 29);

                    await stream.WriteAsync(header, token);

                    // Read response
                    byte[] data = new byte[length];
                    await stream.ReadExactlyAsync(data, 0, length, token);

                    using (var fs = new FileStream(savePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 4096, true))
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        await fs.WriteAsync(data, token);

                        long currentDownloaded = Interlocked.Add(ref downloadedBytes, length);
                        double pct = (double)currentDownloaded / totalSize * 100;
                        OnDownloadProgress?.Invoke(pct);
                        onProgress?.Invoke(pct);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Download chunk error: {ex.Message}");
                    throw new Exception($"[Receiver] Download failed: {ex.Message}", ex);
                }
            });
        }
    }
}
