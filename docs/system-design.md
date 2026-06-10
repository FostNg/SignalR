# ChatBox System Design

**Project:** LAN Chat Application (WPF/.NET 10)
**Date:** 2026-05-20
**Status:** Analysis Complete

---

## 1. Current Architecture

### Solution Structure
```
ChatBox.slnx (3 projects)
├── ChatBox.Client/     # WPF client (UI + socket client)
├── ChatBox.Server/     # WPF server (TCP listener + PostgreSQL)
└── LocalChat.Core/     # Shared library (models, services, contracts)
```

### Technology Stack
| Component | Technology |
|-----------|------------|
| Framework | .NET 10.0-windows, WPF |
| MVVM | CommunityToolkit.Mvvm |
| Database | PostgreSQL via EF Core + Npgsql |
| Networking | TCP Sockets (port 9999 chat, 10000 file transfer) |
| Emoji | Emoji.Wpf |
| Image Handling | Base64 encoding for avatars, chunked file transfer |

---

## 2. Current Folder Structure

### ChatBox.Client/
```
ChatBox.Client/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs  ⚠️ 1534 lines - TOO LARGE
├── Behaviors/
│   └── SmoothScrollBehavior.cs
├── Converters/
│   ├── AvatarInitialsVisibilityConverter.cs
│   ├── Base64ImageConverter.cs
│   └── FirstLetterConverter.cs
├── Managers/
│   ├── ChannelManager.cs
│   ├── ClipboardPasteHandler.cs
│   ├── ConnectionManager.cs
│   ├── EmojiManager.cs
│   ├── FileTransferManager.cs
│   ├── GalleryManager.cs
│   ├── LightboxManager.cs
│   ├── MessageRouter.cs
│   └── WindowChromeManager.cs
└── ViewModels/
    └── ChatMessage.cs  ⚠️ DUPLICATE - exists also in MainWindow.xaml.cs
```

### LocalChat.Core/
```
LocalChat.Core/
├── Contracts/
│   ├── IChatClient.cs
│   ├── IChatServer.cs
│   ├── IFileClient.cs
│   └── IFileServer.cs
├── Data/
│   └── ChatDbContext.cs
├── Models/
│   ├── ChatMessage.cs
│   ├── MessageReaction.cs
│   └── User.cs
└── Services/
    ├── ChatService.cs        # Contains ChatServer + ChatClient classes
    └── FileTransferService.cs  # Contains FileServer + FileClient classes
```

---

## 3. Architecture Patterns Observed

### Current Patterns
| Pattern | Implementation |
|---------|---------------|
| MVVM-light | ViewModels folder exists, but Client uses code-behind heavily |
| Manager Pattern | 9 managers in Client for UI concerns |
| Service Layer | Business logic in LocalChat.Core/Services |
| Contract/Interface | Interfaces in LocalChat.Core/Contracts |
| Converter Pattern | Value converters in Converters folder |
| Behavior Pattern | WPF attached behaviors in Behaviors folder |

### Issues Identified
1. **Code-behind heavy** - MainWindow.xaml.cs (1534 lines) contains UI logic
2. **Duplicate classes** - ChatMessage exists in two locations
3. **No DI container** - Manual constructor injection only
4. **Large files** - Violates "keep files under 200 lines" rule

---

## 4. Recommended Folder Structure

### Clean Architecture Proposal

```
ChatBox/
├── ChatBox.Client/
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs          # Keep minimal
│   ├── Views/
│   │   ├── Controls/
│   │   │   ├── MessageBubble.xaml
│   │   │   ├── UserAvatar.xaml
│   │   │   ├── EmojiPicker.xaml
│   │   │   └── ReactionPopup.xaml
│   │   ├── Overlays/
│   │   │   ├── LightboxOverlay.xaml
│   │   │   └── SettingsOverlay.xaml
│   │   └── Components/
│   │       ├── ChatMessageList.xaml
│   │       ├── ImageGallery.xaml
│   │       ├── FileGallery.xaml
│   │       ├── OnlineUsersPanel.xaml
│   │       └── ChannelSidebar.xaml
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── ChatViewModel.cs
│   │   ├── MessageViewModel.cs
│   │   ├── SettingsViewModel.cs
│   │   └── GalleryViewModel.cs
│   ├── Models/                    # Client-side models
│   │   └── LocalChatMessage.cs
│   ├── Services/                   # Client services (if needed)
│   ├── Behaviors/
│   ├── Converters/
│   └── Managers/                   # UI-specific managers
│       ├── ConnectionManager.cs
│       ├── ImageGalleryManager.cs
│       └── LightboxManager.cs
│
├── ChatBox.Server/
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   ├── Views/
│   │   └── ServerDashboard.xaml
│   └── ViewModels/
│       └── ServerViewModel.cs
│
└── LocalChat.Core/
    ├── Contracts/
    ├── Models/
    ├── Data/
    └── Services/
        ├── ChatServer.cs
        ├── ChatClient.cs
        ├── FileServer.cs
        └── FileClient.cs
```

### Key Changes
1. **Views/Controls/** - Reusable XAML user controls (MessageBubble, UserAvatar, etc.)
2. **Views/Components/** - Composite components (MessageList, Gallery, etc.)
3. **ViewModels/** - Formal MVVM ViewModels with commands and properties
4. **Models/** - Client-side models separate from Core models

---

## 5. UI/UX Design

### Color Palette (Current Discord-Inspired)
| Element | Color | Hex |
|---------|-------|-----|
| Background Primary | Dark | `#060607` |
| Background Secondary | Dark Gray | `#1E1F22` |
| Background Tertiary | Medium Gray | `#2B2D31` |
| Text Primary | White | `#FFFFFF` |
| Text Secondary | Light Gray | `#B5BAC1` |
| Text Muted | Gray | `#6D6F78` |
| Online Status | Green | `#23A55A` |
| Offline Status | Gray | `#747F8D` |
| Error/Disconnect | Red | `#ED4245` |
| Accent Active | Light Gray | `#E3E5E8` |

### Layout Structure

```
┌─────────────────────────────────────────────────────────────────┐
│  Title Bar (Custom Chrome - Drag, Min/Max/Close)                 │
├─────────────┬───────────────────────────────────────────────────┤
│             │  Room Name Header              [Info] [Emoji]       │
│  Channels   ├───────────────────────────────────────────────────┤
│  ─────────  │                                                   │
│  # chat     │  Message List (Virtualized)                       │
│  # images   │  ┌─────────────────────────────────────────────┐  │
│  # files    │  │ [Avatar] Username          timestamp         │  │
│             │  │ Message content here...                     │  │
│  Online     │  │ [Reactions] [React]                          │  │
│  ─────────  │  └─────────────────────────────────────────────┘  │
│  User1      │                                                   │
│  User2 (You)│  ┌─────────────────────────────────────────────┐  │
│             │  │ My messages aligned right                   │  │
│             │  └─────────────────────────────────────────────┘  │
├─────────────┴───────────────────────────────────────────────────┤
│  Input Area (TextBox + Upload + Send)                           │
├─────────────────────────────────────────────────────────────────┤
│  Footer: Avatar Initials | Username | Online Status               │
└─────────────────────────────────────────────────────────────────┘
```

### UI Components

| Component | Description |
|-----------|-------------|
| MessageBubble | Rounded rect with avatar, sender name, timestamp, content, reactions |
| EmojiPicker | 6-category emoji grid popup (Smileys, Animals, Food, Activities, Travel, Objects) |
| ReactionPopup | Quick emoji reaction buttons on message hover |
| LightboxOverlay | Full-screen image viewer with download option |
| SettingsOverlay | Username/avatar/IP configuration panel |

### Interaction Patterns

| Action | Behavior |
|--------|----------|
| Send Message | Enter key or Send button |
| Paste Image | Ctrl+V with image in clipboard |
| Drag & Drop | Files to upload |
| Emoji Reaction | Click reaction button on message |
| View Image | Click thumbnail to open lightbox |
| Channel Switch | Click channel in sidebar |
| Edit Profile | Info button → Settings overlay |

---

## 6. Data Flow

### Message Flow
```
[Client]                    [Server]                     [Database]
   │                           │                              │
   │──── JOIN ─────────────────>│                              │
   │                           │──── Save User ──────────────>│
   │                           │<─── Load History ────────────│
   │<─── History + Room ───────│                              │
   │                           │                              │
   │──── MSG ─────────────────>│                              │
   │                           │──── Save Message ───────────>│
   │                           │<─────────────────────────────│
   │<─── Broadcast ────────────│                              │
   │                           │                              │
   │──── REACT ───────────────>│                              │
   │                           │──── Save/Remove Reaction ───>│
   │<─── REACTION_UPDATE ──────│                              │
```

### File Transfer Flow
```
[Client]                  [Server]                    [Storage]
   │                         │                           │
   │──── FILE_READY ───────>│                           │
   │<─── FILE_READY ────────│ (broadcast to others)     │
   │                         │                           │
   │==== Chunk 1 upload =====>│                           │
   │==== Chunk 2 upload =====>│                           │
   │==== Chunk N upload =====>│                           │
   │                         │==== Save to disk ─────────>│
```

---

## 7. Network Protocol

### Message Types
| Type | Format | Direction |
|------|--------|-----------|
| JOIN | `JOIN\|UserId\|Username\|AvatarBase64` | C→S |
| MSG | `MSG\|UserId\|Content` | C→S |
| FILE_READY | `FILE_READY\|UserId\|FileId\|FileName\|Size` | C→S |
| REACT | `REACT\|UserId\|MessageId\|Emoji` | C→S |
| UPDATE_PROFILE | `UPDATE_PROFILE\|UserId\|Username\|AvatarBase64` | C→S |
| ROOM_NAME | `ROOM_NAME\|RoomName` | S→C |
| GREETING | `GREETING\|Message` | S→C |
| ONLINE_USERS | `ONLINE_USERS\|User1,User2,...` | S→C |
| REACTION_UPDATE | `REACTION_UPDATE\|MessageId\|ReactionsJson` | S→C |

### File Transfer Protocol
- **Port:** 10000
- **Header:** 29 bytes (Action + FileId + Offset + Length)
- **Chunk Size:** 4MB
- **Parallel:** 4 concurrent chunks

---

## 8. Database Schema

### Entities
```
User
├── Id (PK, string)
├── Username (string, max 100)
├── AvatarBase64 (string, nullable)
└── LastSeen (datetime)

ChatMessage
├── Id (PK, string)
├── SenderId (FK → User)
├── Content (string)
├── Timestamp (datetime)
├── IsFile (bool)
├── FileId (string, nullable)
├── FileSize (long)
└── Reactions (collection)

MessageReaction
├── Id (PK, string)
├── MessageId (FK → ChatMessage)
├── UserId (FK → User)
├── Emoji (string, max 50)
├── CreatedAt (datetime)
└── Unique index on (MessageId, UserId, Emoji)
```

---

## 9. Key Issues & Recommendations

### Critical Issues
1. **MainWindow.xaml.cs too large** (1534 lines) → Split into ViewModels + Views
2. **Duplicate ChatMessage class** → Consolidate into single location
3. **No DI container** → Consider Microsoft.Extensions.DependencyInjection
4. **No proper MVVM** → Create ViewModels with RelayCommand from CommunityToolkit.Mvvm

### Recommendations
1. Extract UI logic from MainWindow.xaml.cs into MainViewModel
2. Create reusable XAML user controls for MessageBubble, EmojiPicker, ReactionPopup
3. Implement ICommand pattern for all button clicks
4. Add VirtualizingStackPanel for message list performance
5. Consider adding unit tests

---

## 10. Unresolved Questions

1. Should client have local SQLite cache for offline message history?
2. Do you want to add typing indicators?
3. Should there be message search functionality?
4. Any planned features like DMs, channels, roles?
5. Should file transfer support pause/resume?

---

*Document generated from codebase analysis. To implement recommended changes, see phase plan.*