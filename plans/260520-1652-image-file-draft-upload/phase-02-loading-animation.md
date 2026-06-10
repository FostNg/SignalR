---
title: "Phase 2: Loading Animation Verification"
description: "Verify ProgressBar shows during upload - fix if needed"
status: completed
priority: P1
created: 2026-05-20
---

# Phase 2: Loading Animation Verification

## Context Links
- Parent: [plan.md](../plan.md)
- Phase 1: [phase-01-file-upload-draft.md](./phase-01-file-upload-draft.md)

## Overview

**Priority:** P1 | **Status:** Pending

Verify the ProgressBar loading animation shows correctly during file upload. The ProgressBar bindings exist in XAML but we need to confirm they update during `SendPendingImages()`.

## Key Insights

- `ChatMessage.TransferProgress` property exists (lines 29-34 in ChatMessage.cs)
- `ChatMessage.IsTransferring` property exists (lines 36-41)
- XAML ProgressBar bindings at lines 1061, 1078, 1217, 1293 bind to these properties
- `FileClient.UploadFileAsync()` fires `OnUploadProgress` event with percentage
- `UpdateProgress()` handler at lines 872-880 updates UI thread

## Architecture

**Current flow in SendPendingImages:**
```csharp
msg.IsDraft = false;
msg.IsTransferring = true;  // Should show ProgressBar
RefreshMessageList();

await _fileClient.UploadFileAsync(...);  // Progress events fire

msg.IsTransferring = false;  // Should hide ProgressBar
```

The flow looks correct but we need to verify:
1. `UpdateProgress()` handler is wired to `OnUploadProgress` event
2. `TransferProgress` property setter calls `PropertyChanged` for UI binding

## Related Code Files

**Verify/Modify:**
- `LocalChat.Core/Services/FileTransferService.cs` - lines 93-135 (UploadFileAsync)
- `ChatBox.Client/ViewModels/ChatMessage.cs` - TransferProgress, IsTransferring properties
- `ChatBox.Client/MainWindow.xaml.cs` - UpdateProgress handler, event subscription

## Implementation Steps

1. **Verify event subscription**: Check if `OnUploadProgress` event is connected to `UpdateProgress` handler in `MainWindow.xaml.cs`
   - Look for `_fileClient.OnUploadProgress += UpdateProgress` or similar

2. **Verify PropertyChanged**: Check `ChatMessage.cs` that `TransferProgress` setter calls `OnPropertyChanged(nameof(TransferProgress))`

3. **If not wired**: Add event subscription in `MainWindow.xaml.cs` initialization

4. **Test**: Upload a file and verify ProgressBar animates from 0-100%

## Todo List

- [ ] Check event wiring for upload progress
- [ ] Check TransferProgress PropertyChanged
- [ ] Fix any broken wiring
- [ ] Test ProgressBar animation during upload

## Success Criteria

1. When upload starts, ProgressBar appears and animates 0→100%
2. ProgressBar disappears when upload completes
3. Animation is smooth (updates at least every few percent)

## Risk Assessment

- **Risk:** If event wiring is missing, progress won't update
- **Mitigation:** Simple wiring fix if needed

## Next Steps

- Phase 3: Add remove (X) button to draft items