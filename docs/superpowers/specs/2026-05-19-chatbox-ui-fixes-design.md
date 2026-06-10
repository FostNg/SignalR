# ChatBox UI Fixes Design Spec
**Date:** 2026-05-19
**Topic:** Avatar fallback, Image staging, Username display, Message alignment

---

## 1. Issue Summary

| # | Issue | Current | Desired |
|---|-------|---------|---------|
| 1 | Avatar fallback missing | Empty circle when no avatar uploaded | Show first letter of display name as initials |
| 2 | Image paste sends immediately | Ctrl+V uploads and sends right away | Stage up to 10 images inline as "drafts" until Enter is pressed |
| 3 | Sender name hardcoded as "Me" | Messages from self show "Me" | Show user's actual display name from profile |

---

## 2. Avatar Fallback

### Current State
- `AvatarBase64` is empty string when no avatar uploaded
- XAML binds `ImageBrush` to `AvatarBase64` → shows nothing when empty
- No fallback UI element for initials

### Design
- Use `Ellipse` with solid color fill `#5865F2` as base layer
- Overlay `TextBlock` with first letter of display name (uppercase, white, bold)
- When `AvatarBase64` is non-empty → hide initials, show image instead
- Logic via `IValueConverter` or XAML triggers

### Implementation
- Create `AvatarInitialsConverter : IValueConverter` that:
  - Input: `AvatarBase64` string
  - Output: `Visibility` → `Collapsed` if Base64 non-empty, `Visible` if empty
- Also need a converter for the image visibility (inverse)
- Initials text bound to sender name's first letter

---

## 3. Image Staging (Pending Panel Approach)

### Current State
- `HandleImagePaste()` in `MainWindow.xaml.cs` (line 651):
  1. Saves image to TempPaste folder
  2. Creates `ChatMessage` immediately
  3. Calls `_fileClient.UploadFileAsync()` immediately
  4. Calls `_connectionManager.SendMessageAsync("FILE_READY|...")` immediately

### Design (Approach A - Pending Panel)
- **Pending panel**: A dedicated panel at the bottom of chat area, above the message input
- **Draft list**: `_pendingImages : List<ChatMessage>` (max 10)
- When Ctrl+V image detected:
  1. Save image to TempPaste (same as before)
  2. Create `ChatMessage` with `IsDraft = true` and `Sender = _displayName`
  3. Add to `_pendingImages` list and display in pending panel (with preview thumbnails)
  4. **DO NOT** upload or send yet
- When Enter pressed in message input:
  1. If `_pendingImages.Count > 0`, iterate and upload+send each
  2. Clear `_pendingImages` after sending
- Pending panel UI:
  - Horizontal `WrapPanel` with small thumbnail previews (48x48 or 64x64)
  - Small "X" button on each thumbnail to remove from queue
  - Count indicator: "3 images pending"
  - Panel background: subtle gray `#F2F3F5`
  - Panel height: auto, max ~100px with scroll if needed

### Max 10 Images
- Before adding new draft, check `_pendingImages.Count >= 10`
- If full, show toast: "Maximum 10 images pending. Send or remove some first."

### Visual Difference: Draft vs Sent
| State | Border | Opacity | Progress | Send Indicator |
|-------|--------|---------|----------|---------------|
| Draft (thumbnail) | Solid purple | 1.0 | None | Small pending icon |
| Uploading | Solid | 1.0 | ProgressBar on thumbnail | Spinner overlay |
| Sent | Normal | 1.0 | Hidden | Normal message in chat |

### Pending Panel Layout (XAML concept)
```
<Border x:Name="PendingImagesPanel" Background="#F2F3F5"
        Height="Auto" MinHeight="60" MaxHeight="120"
        Visibility="{Binding HasPendingImages, Converter=BoolToVis}">
  <WrapPanel Orientation="Horizontal" Margin="8">
    <!-- Thumbnail items with remove button -->
  </WrapPanel>
</Border>
```

### Draft Thumbnail Item
- Small `Image` (64x64) with rounded corners
- Purple border when draft
- Red "X" button overlay top-right
- Progress ring when uploading
- Tooltip: filename

---

## 4. Username Display

### Current State
- `HandleImagePaste()` line 677: `Sender = "Me"` hardcoded
- `txtUsername` TextBox exists with default "User"
- No persistent user profile with display name

### Design
- Store display name from `txtUsername.Text` when it changes
- Use stored display name for all new messages (`Sender = _displayName`)
- `HandleImagePaste()`: change `Sender = "Me"` → `Sender = _displayName`
- For incoming messages: use sender name from server packet

### Implementation
- `_displayName : string` field in MainWindow
- `TxtUsername_TextChanged()`: update `_displayName`
- `HandleImagePaste()`: use `_displayName` instead of "Me"
- `BtnSendChat_Click()`: already uses bound data, just needs `_displayName` for avatar context

---

## 5. File Changes

### Files to Modify
| File | Changes |
|------|---------|
| `ChatBox.Client/MainWindow.xaml.cs` | Image staging logic, username field, HandleImagePaste update |
| `ChatBox.Client/MainWindow.xaml` | Alignment triggers, avatar fallback, draft visual style |
| `ChatBox.Client/ViewModels/ChatMessage.cs` | Add `IsDraft` property, maybe `PendingIndex` |
| `ChatBox.Client/Converters/Base64ImageConverter.cs` | May need to add/extend converters for avatar fallback |

### New Files
| File | Purpose |
|------|---------|
| `ChatBox.Client/Converters/AvatarInitialsVisibilityConverter.cs` | Show initials when no avatar |
| `ChatBox.Client/Converters/FirstLetterConverter.cs` | Extract first letter from name for initials |

---

## 6. Dependencies
- No database changes
- No network protocol changes (image staging is client-side only)
- Backward compatible with existing messages