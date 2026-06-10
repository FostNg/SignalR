# LAN ChatBox

A premium, highly responsive Local Area Network (LAN) chat application built with .NET 10.0 Windows Presentation Foundation (WPF). It features a modern, Discord-inspired user interface, raw TCP socket communication for real-time messaging, and Entity Framework Core with PostgreSQL for persistent history storage.

## Features

### Modern User Interface
- Discord-style aesthetics with custom window chromes, glassmorphism title bars, and modern scrollbars.
- Dynamic custom settings panel for profile and network configuration.
- Integrated Lightbox view for full-size image previews.
- Fully restricted input fields to prevent unauthorized history manipulation (blocked Alt+Backspace, Ctrl+Z).

### Messaging & File Transfer
- Real-time text messaging across the LAN via a dedicated TCP socket server (Port 9999).
- Background file transfer protocol (Port 10000) allowing seamless large file uploads and downloads.
- Drag-and-drop support: Drag files directly into the application window to instantly upload and send them to the channel.
- Automatic image preview rendering: Image attachments are downloaded and displayed in the chat feed automatically without manual user intervention.

### User Configuration & Portability
- Portable Executables: The application is designed to be packaged as a single-file, self-contained executable.
- Clean Environment: User configurations, chat history caches, and downloaded files are isolated and securely stored in Windows standard `%APPDATA%` and `%LOCALAPPDATA%` directories, ensuring no test data is shipped when distributing the application.
- Customizable Avatars: Users can upload custom profile pictures which are heavily optimized and securely transmitted to peers.

### Persistent Server Storage
- The Server application acts as a centralized hub, managing connected clients, broadcasting messages, and caching transferred files.
- PostgreSQL Database integration via Entity Framework Core.
- The server automatically executes schema migrations and creates the required database (`ChatBoxDb`) upon startup.

### Asynchronous & Parallel Programming
- **Non-blocking UI:** Extensive use of `async` and `await` ensures the WPF UI thread never freezes during long-running network or database operations.
- **Concurrent Server Handling:** The server utilizes `Task.Run` and independent task contexts to handle multiple TCP client connections concurrently, allowing dozens of users to chat simultaneously without bottlenecking.
- **Background File Processing:** File transfers (uploading, downloading, and image caching) are executed in parallel on separate background threads (`FileTransferService`). Files are transmitted in chunks to prevent memory spikes.
- **Cooperative Cancellation:** Implementing `CancellationTokenSource` allows users to safely abort connection attempts instantly without raising unhandled exception crashes.
- **Async Data Access:** Entity Framework Core operations (`AddAsync`, `SaveChangesAsync`, `ToListAsync`) are strictly asynchronous to maximize the server's throughput capacity.

## Architecture

The solution is divided into three core projects:
1. LocalChat.Core: A shared library containing data models (`User`, `ChatMessage`), database context (`ChatDbContext`), and core background transfer services.
2. ChatBox.Server: The backend WPF application that monitors TCP ports, relays messages, hosts the PostgreSQL connection, and stores uploaded files in `%LOCALAPPDATA%`.
3. ChatBox.Client: The front-end user application containing all interactive UI logic and local socket client operations.

## Installation & Setup

### Prerequisites
- Windows 10 or Windows 11.
- .NET 10.0 SDK.
- PostgreSQL installed and running on the server machine.

### Server Configuration
By default, the server expects a local PostgreSQL instance running with the following credentials:
- Host: localhost
- Port: 5432
- Username: postgres
- Password: 12345

If your PostgreSQL credentials differ, you must update the connection string located in `LocalChat.Core/Data/ChatDbContext.cs` before compiling the server.

### Building from Source
To build the project into clean, shareable `.exe` files, open a terminal in the solution directory and run the following commands:

```bash
# Build the Client
dotnet publish ChatBox.Client/ChatBox.Client.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish/Client

# Build the Server
dotnet publish ChatBox.Server/ChatBox.Server.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish/Server
```

The compiled applications will be available in the `publish` directory.

## Usage

1. Start the Server: Run `ChatBox.Server.exe` on the host machine. The server will display the local IPv4 addresses it is currently listening on.
2. Start the Client: Run `ChatBox.Client.exe` on any computer within the same network.
3. Connect: In the client settings overlay, enter your desired Display Name and input the IP address displayed by the Server application. Click "Connect to Server".
4. Chat & Share: Once connected, use the text field to chat or drag and drop files directly into the window.

## License

This project is provided for educational and internal network communication purposes.
