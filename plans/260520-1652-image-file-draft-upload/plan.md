---
title: "Image/File Draft Upload with Loading Animation"
description: "When uploading or Ctrl+V, images/files should draft in chat box for review before sending (Enter). Add loading animation during upload. Support up to 10 images."
status: done
priority: P1
effort: 6h
branch: feature/emoji-reactions
tags: [image-upload, draft, loading-animation, wpf]
created: 2026-05-20
---

# Feature Implementation Plan

## Overview

Implement draft behavior for image/file uploads: staged in chat with purple dashed border, removable before Enter. Add loading animation during upload. Max 10 images.

## Phases

| # | Phase | Status | Effort | Link |
|---|-------|--------|--------|------|
| 1 | Fix file upload to draft before sending | Pending | 2h | [phase-01](./phase-01-file-upload-draft.md) |
| 2 | Add loading animation during upload | Pending | 2h | [phase-02](./phase-02-loading-animation.md) |
| 3 | Wire up remove button on draft items | Pending | 1h | [phase-03](./phase-03-remove-button.md) |
| 4 | Testing & verification | Pending | 1h | [phase-04](./phase-04-testing.md) |

## Dependencies

- Existing: `_pendingImages` list, `IsDraft` flag, `IsTransferring` flag, `TransferProgress` property
- No new dependencies needed

## Key Problems Identified

1. **Upload via + button and drag-drop** directly calls `UploadFileAsync()` which starts upload immediately without drafting
2. **Paste (Ctrl+V)** correctly creates draft with `IsDraft=true` but `BtnRemovePending_Click` has no XAML binding
3. **Loading animation** exists (ProgressBar bound to `IsTransferring`/`TransferProgress`) but not triggered because upload starts immediately
4. **Remove button** `BtnRemovePending_Click` exists in code-behind but no XAML button binding found