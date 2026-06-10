---
title: "Phase 3: Remove Button for Draft Items"
description: "Add X button to draft images/files so user can remove before sending"
status: completed
priority: P1
created: 2026-05-20
---

# Phase 3: Remove Button for Draft Items

## Context Links
- Parent: [plan.md](../plan.md)

## Overview

**Priority:** P1 | **Status:** Pending

Add a remove (X) button to draft images/files so users can remove them before pressing Enter to send.

## Key Insights

- `BtnRemovePending_Click` handler exists at lines 1561-1569 in `MainWindow.xaml.cs`
- No XAML button is currently bound to this handler (grep returned no matches)
- Need to add an X button overlay on draft image/file items

## Architecture

**Current state:**
- Draft images show with purple dashed border (via DataTrigger at lines 1137-1142)
- No visible way to remove them before sending

**Target state:**
- X button appears in top-right corner of draft items
- Clicking removes from `_pendingImages` and `_allMessages`

## Related Code Files

**Modify:**
- `ChatBox.Client/MainWindow.xaml` - Add remove button to draft image template
- `ChatBox.Client/MainWindow.xaml.cs` - `BtnRemovePending_Click` already exists

## Implementation Steps

1. **Add remove Button to ImageEmbedBorder** template:
   - Position absolute in top-right corner
   - Only visible when `IsDraft=True`
   - Style: small circular button with X or trash icon
   - Bind `Click="BtnRemovePending_Click"`
   - Bind `Tag="{Binding}"` to pass the message to handler

2. **Add remove Button to File attachment template**:
   - Similar approach - X button in top-right of file card
   - Only visible when `IsDraft=True`

3. **Test**: Draft items show X button, clicking removes from chat

## Todo List

- [ ] Add X button to draft image template (ImageEmbedBorder)
- [ ] Add X button to draft file template (FileAttachmentBorder)
- [ ] Test remove button on draft images
- [ ] Test remove button on draft files

## Success Criteria

1. Draft images show X button in top-right corner
2. Clicking X removes image from draft list
3. X button only visible for draft items (IsDraft=True)
4. Works for both images and file attachments

## Risk Assessment

- None identified - straightforward button binding

## Next Steps

- Phase 4: Testing & verification