# Bug Fix Plan — ChatBox UI — 2025-05-21

> **Scope:** `ChatBox.Client/MainWindow.xaml` · `ChatBox.Client/MainWindow.xaml.cs`  
> **Branch:** `feature/emoji-reactions`  
> **Strategy:** Fix per-bug → commit ngay sau mỗi fix

---

## Bug List & Root Causes

### Bug #1 — Hover Reaction Bar không hoạt động
**Triệu chứng:** Hover vào tin nhắn không có reaction bar, hoặc click emoji reaction không có hiệu ứng.  
**Root cause:** `QuickReactionBar` nằm ngoài scope `DataTemplate` → `btn.DataContext is ChatMessage msg` luôn `false` trong `ReactionButton_Click`.  
**Fix:** Bind `Tag="{Binding}"` vào mỗi `Button` trong `QuickReactionBar`; đọc `ChatMessage` từ `btn.Tag` thay vì `btn.DataContext`.

---

### Bug #2 — Tin nhắn không xuống dòng (text wrap)
**Triệu chứng:** Gõ tin nhắn dài, text chạy ngang không xuống dòng trong bubble.  
**Root cause:** `emoji:RichTextBox` không có `MaxHeight` giới hạn, và hàm lấy text đang dùng `txtInput.Text` không đúng với `RichTextBox`.  
**Fix:** Đặt `AcceptsReturn="False"` và sửa hàm lấy text dùng `TextRange(Document.ContentStart, Document.ContentEnd).Text`.

---

### Bug #3 — Cục xám kỳ cục gần scrollbar
**Triệu chứng:** Xuất hiện một khối màu xám lạ ở vùng scroll của chat.  
**Root cause:** `ScrollBar` global style render "thumb" kể cả khi không có nội dung cần scroll; `CanContentScroll="False"` kết hợp với style tùy chỉnh gây artifact.  
**Fix:** Bật `VirtualizingStackPanel` cho `lstChatMessages` và đặt `ScrollViewer.CanContentScroll="True"`.

---

### Bug #4 — Ảnh draft size bất thường khi gửi nhiều ảnh
**Triệu chứng:** Gửi 5 ảnh cùng lúc → preview trong chat to/nhỏ bất thường, méo.  
**Root cause:** `DraftStagingItems` template dùng `Stretch="Uniform"` cộng với `Width/Height` cố định `128×128` nhưng ảnh nhỏ bị kéo giãn.  
**Fix:** Bọc `Image` trong `Border` với `ClipToBounds="True"` và dùng `Stretch="UniformToFill"`.

---

### Bug #5 — Placeholder cần 2 ký tự mới ẩn
**Triệu chứng:** Gõ 1 ký tự vào input → placeholder chưa ẩn; gõ ký tự thứ 2 mới ẩn.  
**Root cause:** `emoji:RichTextBox.Text` trả về `\r\n` (nội dung trống của `FlowDocument`) ngay cả khi chưa gõ, nên lần đầu gõ text thực tế vẫn match `empty`.  
**Fix:** Kiểm tra `TextRange(Document.ContentStart, Document.ContentEnd).Text.Trim()` thay vì `txtInput.Text`.

---

### Bug #6 — AllChat không hiện đủ, cần lazy load
**Triệu chứng:** Chuyển channel hoặc kết nối → chỉ thấy một phần tin nhắn, scroll không tải thêm.  
**Root cause:** `RefreshMessageList()` gọi `Items.Clear()` rồi add lại toàn bộ — không có virtualization.  
**Fix:** Bật `VirtualizingStackPanel.IsVirtualizing="True"` + `VirtualizationMode="Recycling"` trên `lstChatMessages`; bind `ItemsSource` một lần thay vì clear/add.

---

### Bug #7 — Chat lag nặng khi gửi ảnh/file
**Triệu chứng:** Gửi 1 tin/ảnh → UI đơ vài giây.  
**Root causes:**
1. `RefreshMessageList()` clear+add toàn bộ `_allMessages` trên UI thread mỗi lần có tin mới.
2. `SendPendingImagesAsync()` await tuần tự từng ảnh (`foreach` + `await`).
3. `Window_Drop` await tuần tự từng file.

**Fix:**
- Đổi `_allMessages` → `ObservableCollection<ChatMessage>`, bind `ItemsSource` 1 lần → WPF tự update incremental.
- `SendPendingImagesAsync()` → `Task.WhenAll(toSend.Select(msg => UploadOneAsync(msg)))`.
- `Window_Drop` → `await Task.WhenAll(files.Select(f => UploadFileAsync(f)))`.

---

## Thứ tự thực thi & Commits

| Bước | Bug | Commit message |
|------|-----|----------------|
| 1 | #7 - Performance (ObservableCollection) | `perf(ui): replace List with ObservableCollection for message binding` |
| 2 | #7 - Parallel upload | `perf(ui): parallelize pending image and file drop uploads` |
| 3 | #5 - Placeholder | `fix(ui): use TextRange to detect empty RichTextBox for placeholder` |
| 4 | #2 - Text wrap | `fix(ui): fix text wrap in chat by reading RichTextBox content correctly` |
| 5 | #1 - Reaction bar | `fix(ui): fix reaction button DataContext by using Tag binding` |
| 6 | #3 - Scrollbar artifact | `fix(ui): enable VirtualizingStackPanel to remove scrollbar artifact` |
| 7 | #4 - Image size | `fix(ui): fix draft image preview stretch in staging panel` |
| 8 | #6 - Lazy load | `perf(ui): enable ListBox virtualization for lazy message rendering` |

---

## Files ảnh hưởng

- `ChatBox.Client/MainWindow.xaml` — Bug #1, #3, #4, #6
- `ChatBox.Client/MainWindow.xaml.cs` — Bug #1, #2, #5, #7
