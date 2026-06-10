---
title: "Phase 1: File Upload Draft Implementation"
description: "Modify upload code path to create draft items first instead of uploading immediately"
status: completed
priority: P1
created: 2026-05-20
---

# Phase 1: File Upload Draft Implementation

## Context Links
- Parent: [plan.md](../plan.md)
- Scout report: `scout/scout-01.md` (synthesized)

## Overview

**Priority:** P1 | **Status:** Pending

Modify the upload flow so files/images staged via + button, drag-drop, or paste create draft items first. User presses Enter to actually upload. This mirrors the existing paste draft behavior but for all upload methods.

## Key Insights

- `UploadFileAsync()` (lines 750-794) uploads immediately
- `HandleImagePaste()` (lines 1369-1425) correctly creates draft with `IsDraft=true`
- Need to create a `CreateDraftFileMessage()` method that creates the message in draft state (like paste)
- Both `BtnUpload_Click` and `Window_Drop` should call this draft method instead of `UploadFileAsync`
- `SendPendingImages()` (lines 1427-1454) handles the actual upload when Enter is pressed

## Requirements

### Functional
- When user clicks + button or drags file → create draft item in chat (purple dashed border)
- When user presses Enter → `SendPendingImages()` uploads all drafts
- Max 10 draft items enforced on all paths

### Non-functional
- Same UX for + button, drag-drop, and paste
- No change to existing server protocol

## Architecture

**Current flow (broken):**
```
+ Button / Drag-Drop → UploadFileAsync() → IsTransferring=true → upload starts immediately
```

**New flow (target):**
```
+ Button / Drag-Drop → CreateDraftFileMessage() → IsDraft=true → appears in chat
Enter pressed → SendPendingImages() → IsDraft=false → IsTransferring=true → upload starts
```

## Related Code Files

**Modify:**
- `ChatBox.Client/MainWindow.xaml.cs`
  - `BtnUpload_Click` (796-803): Call `CreateDraftFileMessage()` instead of `UploadFileAsync()`
  - `Window_Drop` (818-834): Call `CreateDraftFileMessage()` instead of `UploadFileAsync()`
  - Add `CreateDraftFileMessage()` method (new)
  - `SendPendingImages()` (1427-1454): Keep as-is - handles draft→upload transition

## Implementation Steps

1. **Add `CreateDraftFileMessage()` method** to `MainWindow.xaml.cs`:
   - Takes `filePath` string parameter
   - Checks `_pendingImages.Count >= 10`
   - Creates `ChatMessage` with `IsDraft=true, IsTransferring=false`
   - Adds to `_pendingImages` and `_allMessages`
   - Calls `RefreshMessageList()`
   - Returns the created `ChatMessage` or null if at limit

2. **Modify `BtnUpload_Click`**:
   - Remove `await UploadFileAsync(dialog.FileName)`
   - Call `CreateDraftFileMessage(dialog.FileName)` instead

3. **Modify `Window_Drop`**:
   - Remove `await UploadFileAsync(file)` call
   - Call `CreateDraftFileMessage(file)` instead
   - Keep the async/await structure but change method called

4. **Verify `HandleImagePaste()` already does the right thing** - it creates draft, nothing to change

## Todo List

- [ ] Add `CreateDraftFileMessage()` method
- [ ] Update `BtnUpload_Click` to use draft method
- [ ] Update `Window_Drop` to use draft method
- [ ] Test: + button creates draft with purple border
- [ ] Test: drag-drop creates draft with purple border
- [ ] Test: Enter sends all drafts

## Success Criteria

1. Files dropped or selected via + button appear in chat with purple dashed border (IsDraft=true) before Enter
2. Pressing Enter uploads all pending drafts and shows ProgressBar loading animation
3. Cannot stage more than 10 images (existing check works for all paths)
4. Existing paste (Ctrl+V) behavior unchanged

## Risk Assessment

- **Risk:** `_pendingImages.Clear()` in `SendPendingImages()` might cause issues if multiple upload paths add to it
- **Mitigation:** `_pendingImages` list is the correct staging area, clear is intentional before re-uploading
- **Risk:** `_currentTransferMessage` tracking in `UploadFileAsync` not used in draft path
- **Mitigation:** Draft path doesn't need `_currentTransferMessage` since upload happens in `SendPendingImages()`

## Security Considerations

- File validation already exists (checks `File.Exists`)
- No new security concerns - same file handling as before

## Next Steps

- Phase 2: Verify ProgressBar animation fires correctly during actual upload