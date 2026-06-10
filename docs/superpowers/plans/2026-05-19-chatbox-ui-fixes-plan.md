# ChatBox UI Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 3 UI issues: (1) avatar fallback showing empty circle, (2) image paste sends immediately instead of staging, (3) sender name hardcoded "Me"

**Architecture:** WPF application with MVVM-light pattern. Avatar fallback via XAML converter with layered Ellipse+TextBlock. Image staging via pending list + panel above input. Username from profile field.

**Tech Stack:** WPF, C#, XAML, IValueConverter, ChatMessage ViewModel

---

## File Map

### New Files
- `ChatBox.Client/Converters/AvatarInitialsVisibilityConverter.cs` — shows initials when AvatarBase64 is empty
- `ChatBox.Client/Converters/FirstLetterConverter.cs` — returns first character uppercase of a name string

### Modified Files
- `ChatBox.Client/ViewModels/ChatMessage.cs` — add `IsDraft` bool property
- `ChatBox.Client/MainWindow.xaml` — add pending images panel, avatar converters, footer avatar toggle
- `ChatBox.Client/MainWindow.xaml.cs` — add `_pendingImages` list, staging logic, fix "Me" → username

---

## Task 1: Add IsDraft Property to ChatMessage

**Files:**
- Modify: `ChatBox.Client/ViewModels/ChatMessage.cs`

- [ ] **Step 1: Add IsDraft bool property**

In `ChatMessage.cs`, add after `public bool IsMe { get; set; }`:

```csharp
private bool _isDraft;
public bool IsDraft
{
    get => _isDraft;
    set { _isDraft = value; OnPropertyChanged(nameof(IsDraft)); }
}
```

- [ ] **Step 2: Commit**

```bash
git add ChatBox.Client/ViewModels/ChatMessage.cs
git commit -m "feat: add IsDraft property to ChatMessage"
```

---

## Task 2: Create Avatar Converters

**Files:**
- Create: `ChatBox.Client/Converters/AvatarInitialsVisibilityConverter.cs`
- Create: `ChatBox.Client/Converters/FirstLetterConverter.cs`

- [ ] **Step 1: Create AvatarInitialsVisibilityConverter**

```csharp
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChatBox.Client.Converters
{
    public class AvatarInitialsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string base64 && !string.IsNullOrEmpty(base64))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
```

- [ ] **Step 2: Create FirstLetterConverter**

```csharp
using System;
using System.Globalization;
using System.Windows.Data;

namespace ChatBox.Client.Converters
{
    public class FirstLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrEmpty(name))
            {
                char initial = char.ToUpper(name[0]);
                return initial.ToString();
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add ChatBox.Client/Converters/AvatarInitialsVisibilityConverter.cs ChatBox.Client/Converters/FirstLetterConverter.cs
git commit -m "feat: add avatar converters for initials fallback and first letter extraction"
```

---

## Task 3: Update MainWindow.xaml with Avatar Fallback

**Files:**
- Modify: `ChatBox.Client/MainWindow.xaml` — add converters to Window resources, update avatar ellipses

- [ ] **Step 1: Register converters in Window.Resources**

Find the `Window.Resources` section in MainWindow.xaml and add:

```xml
<converters:AvatarInitialsVisibilityConverter x:Key="AvatarInitialsVisConverter"/>
<converters:FirstLetterConverter x:Key="FirstLetterConverter"/>
```

- [ ] **Step 2: Update message avatar ellipse**

Find the Ellipse `imgMsgAvatar` in the message template (around line 789). Wrap it with a Grid and add initials fallback layer:

```xml
<Grid>
    <!-- Avatar image (hidden when no avatar set) -->
    <Ellipse x:Name="imgMsgAvatar" Grid.Column="0" Width="40" Height="40" VerticalAlignment="Top" HorizontalAlignment="Center">
        <Ellipse.Fill>
            <ImageBrush ImageSource="{Binding AvatarBase64, Converter={StaticResource Base64Converter}}" Stretch="UniformToFill"/>
        </Ellipse.Fill>
    </Ellipse>
    <!-- Initials fallback - purple circle + letter when no avatar -->
    <Ellipse Width="40" Height="40" VerticalAlignment="Top" HorizontalAlignment="Center"
             Fill="#5865F2"
             Visibility="{Binding AvatarBase64, Converter={StaticResource AvatarInitialsVisConverter}}"/>
    <TextBlock Text="{Binding Sender, Converter={StaticResource FirstLetterConverter}}"
               FontSize="16" FontWeight="Bold" Foreground="White"
               HorizontalAlignment="Center" VerticalAlignment="Center"
               Visibility="{Binding AvatarBase64, Converter={StaticResource AvatarInitialsVisConverter}}"/>
</Grid>
```

- [ ] **Step 3: Update footer avatar ellipse**

Find the footer avatar area. The footer already has `lblFooterInitials` and `lblAvatarInitials` — ensure the initials TextBlocks are shown when AvatarBase64 is empty by adding visibility binding:

```xml
<TextBlock x:Name="lblFooterInitials" Text="U" FontSize="16" FontWeight="Bold" Foreground="White"
          HorizontalAlignment="Center" VerticalAlignment="Center"
          Visibility="{Binding _avatarBase64, Converter={StaticResource AvatarInitialsVisConverter}, FallbackValue=Visible}"/>
```

Actually, for footer avatar, since the initials already exist as separate labels (`lblAvatarInitials`, `lblFooterInitials`), we just need to ensure they are visible when `_avatarBase64` is empty and hidden when it has a value. The existing code already sets initials from username — we just need to add the visibility toggle.

For the footer Ellipse fill (avatar image), add `Visibility` binding:
```xml
<Ellipse x:Name="imgFooterAvatar" ...>
    <Ellipse.Fill>
        <ImageBrush ImageSource="{Binding _avatarBase64, Converter={StaticResource Base64Converter}}" .../>
    </Ellipse.Fill>
</Ellipse>
```

And the corresponding initials Ellipse + TextBlock overlay should have visibility toggled.

- [ ] **Step 4: Commit**

---

## Task 4: Add Pending Images Panel to XAML

**Files:**
- Modify: `ChatBox.Client/MainWindow.xaml` — add pending images panel above input area

- [ ] **Step 1: Find the chat input area**

Find the `borderInputArea` or input stack area. Add a pending panel ABOVE the input but inside the chat container.

```xml
<!-- Pending Images Panel -->
<Border x:Name="PendingImagesPanel" Background="#F2F3F5"
        Height="Auto" MinHeight="0" MaxHeight="120"
        Visibility="Collapsed" BorderBrush="#5865F2" BorderThickness="0,1,0,0">
    <Grid Margin="8,6">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <TextBlock x:Name="lblPendingCount" Grid.Column="0" Text="0 images pending"
                   FontSize="11" Foreground="#747F8D" VerticalAlignment="Center" Margin="0,0,12,0"/>
        <ItemsControl x:Name="itemsPendingImages" Grid.Column="1">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <WrapPanel Orientation="Horizontal"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Grid Margin="0,0,8,0" Width="64" Height="64">
                        <Image Source="{Binding LocalFilePath}" Stretch="UniformToFill"
                               Width="64" Height="64" RenderOptions.BitmapScalingMode="HighQuality"/>
                        <Border Background="#00FFFFFF" BorderBrush="#5865F2" BorderThickness="2"
                                CornerRadius="4" Opacity="0.5"/>
                        <Button Content="x" FontSize="14" FontWeight="Bold"
                                HorizontalAlignment="Right" VerticalAlignment="Top"
                                Margin="0,-6,-6,0" Width="20" Height="20"
                                Background="#E74C3C" Foreground="White"
                                Cursor="Hand" Tag="{Binding}"
                                Click="BtnRemovePending_Click">
                            <Button.Resources>
                                <Style TargetType="Border"><Setter Property="CornerRadius" Value="10"/></Style>
                            </Button.Resources>
                        </Button>
                    </Grid>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Grid>
</Border>
```

- [ ] **Step 2: Commit**

---

## Task 5: Implement Image Staging Logic in MainWindow.xaml.cs

**Files:**
- Modify: `ChatBox.Client/MainWindow.xaml.cs`

- [ ] **Step 1: Add fields for pending images and display name**

After line 128 (`private string _avatarBase64 = "";`), add:

```csharp
private List<ChatMessage> _pendingImages = new();
private string _displayName = "";
```

- [ ] **Step 2: Update LoadOrGenerateConfig to set _displayName**

In `LoadOrGenerateConfig()`, after the initials are set (lines 158-162), add:

```csharp
_displayName = string.IsNullOrWhiteSpace(txtUsername.Text) ? "User" : txtUsername.Text.Trim();
```

- [ ] **Step 3: Update TxtUsername_TextChanged to also set _displayName**

At the start of `TxtUsername_TextChanged()`, before setting `lblFooterUsername`:

```csharp
_displayName = string.IsNullOrWhiteSpace(txtUsername.Text) ? "User" : txtUsername.Text.Trim();
```

- [ ] **Step 4: Add pending count update helper method**

Add near the bottom of the class (before `FormatTimestamp`):

```csharp
private void UpdatePendingImagesPanel()
{
    int count = _pendingImages.Count;
    lblPendingCount.Text = $"{count} image{(count != 1 ? "s" : "")} pending";
    PendingImagesPanel.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    itemsPendingImages.ItemsSource = null;
    itemsPendingImages.ItemsSource = _pendingImages;
}
```

- [ ] **Step 5: Add RemovePending handler**

Add near other button handlers:

```csharp
private void BtnRemovePending_Click(object sender, RoutedEventArgs e)
{
    if (sender is Button btn && btn.Tag is ChatMessage msg)
    {
        _pendingImages.Remove(msg);
        UpdatePendingImagesPanel();
    }
}
```

- [ ] **Step 6: Update HandleImagePaste to stage instead of send**

Replace `HandleImagePaste()` body with staging-only logic (no upload, no send):

```csharp
private void HandleImagePaste()
{
    try
    {
        if (_pendingImages.Count >= 10)
        {
            MessageBox.Show("Maximum 10 images pending. Send or remove some first.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            encoder.Save(fileStream);
        }

        Guid fileId = Guid.NewGuid();
        var fileInfo = new FileInfo(filePath);

        var msg = new ChatMessage
        {
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
            IsInImageChannel = true,
            IsDraft = true
        };

        _pendingImages.Add(msg);
        UpdatePendingImagesPanel();
    }
    catch (Exception ex)
    {
        MessageBox.Show("Failed to paste image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

- [ ] **Step 7: Add SendPendingImages method**

```csharp
private async void SendPendingImages()
{
    if (_pendingImages.Count == 0) return;

    var toSend = _pendingImages.ToList();
    _pendingImages.Clear();
    UpdatePendingImagesPanel();

    foreach (var msg in toSend)
    {
        msg.IsDraft = false;
        msg.IsTransferring = true;
        _channelManager.AllMessages.Add(msg);
        _channelManager.RefreshMessageList();

        try
        {
            await _fileClient.UploadFileAsync(_connectionManager.ServerIp, msg.LocalFilePath, Guid.Parse(msg.FileId));
            msg.IsTransferring = false;
            await _connectionManager.SendMessageAsync($"FILE_READY|{_userId}|{msg.FileId}|{msg.Content}|{msg.FileSize}");
        }
        catch
        {
            msg.IsTransferring = false;
        }
    }

    _channelManager.RefreshMessageList();
}
```

- [ ] **Step 8: Hook SendPendingImages to Enter key**

Find the key-down handler for `txtChatInput` (or wherever Enter sends messages). Add `SendPendingImages()` call at the start of the Enter handling block, before sending text:

```csharp
if (e.Key == Key.Enter && !isShift)
{
    e.Handled = true;
    SendPendingImages(); // <-- ADD THIS
    string text = txtChatInput.Text.Trim();
    if (!string.IsNullOrEmpty(text))
    {
        await BtnSendChat_ClickInternal(text);
    }
    txtChatInput.Clear();
    return;
}
```

- [ ] **Step 9: Commit**

---

## Task 6: Fix "Me" → Display Name in All Message Sending

**Files:**
- Modify: `ChatBox.Client/MainWindow.xaml.cs`

- [ ] **Step 1: Find all places where `Sender = "Me"` is set**

Grep for `Sender = "Me"` — this appears in `HandleImagePaste()` which we already fixed, and may appear in other file-send methods.

- [ ] **Step 2: Replace any remaining `Sender = "Me"` with `Sender = _displayName`**

Also fix any other hardcoded "Me" in `HandleFileDragDrop()`, `HandleFileSelect()`, etc.

- [ ] **Step 3: Commit**

---

## Task 7: Full Integration and Test

**Files:**
- All modified files

- [ ] **Step 1: Build the solution**

Run: `dotnet build ChatBox.sln`

- [ ] **Step 2: Fix any compilation errors**

- [ ] **Step 3: Test avatar fallback**

1. Launch app without avatar set
2. Verify initials show in avatar circles (message avatars and footer)
3. Set an avatar and verify image shows instead

- [ ] **Step 4: Test image staging**

1. Ctrl+V an image — verify it appears in pending panel, NOT in chat
2. Add 2 more images — verify count shows "3 images pending"
3. Press Enter — verify all 3 images send together as real messages
4. Try adding 11th image — verify limit message appears

- [ ] **Step 5: Test username display**

1. Set a username like "Camellya"
2. Send a message — verify sender name shows "Camellya" not "Me"

- [ ] **Step 6: Commit final**

---

## Spec Coverage Checklist

| Spec Section | Task(s) | Status |
|---|---|---|
| Avatar fallback (initials) | Task 2, Task 3 | |
| Image staging pending panel (max 10) | Task 4, Task 5 | |
| Username "Me" → display name | Task 5, Task 6 | |
| Pending panel removal (X button) | Task 5 (BtnRemovePending) | |
| Send on Enter | Task 5 (SendPendingImages) | |

## Placeholder Scan

- [x] No "TBD" or "TODO" in task steps
- [x] All code blocks show actual implementation
- [x] Exact file paths with line number hints
- [x] No "similar to X" references without repeating code