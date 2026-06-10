---
title: "Phase 4: Testing & Verification"
description: "Test all draft upload flows and loading animation"
status: completed
priority: P1
created: 2026-05-20
---

# Phase 4: Testing & Verification

## Context Links
- Parent: [plan.md](../plan.md)
- Phase 1-3 complete before this phase

## Overview

**Priority:** P1 | **Status:** Pending

Comprehensive testing of all draft upload flows.

## Implementation Steps

### Test Cases

1. **Ctrl+V Paste Image**
   - [ ] Image appears in chat with purple dashed border
   - [ ] X button removes image
   - [ ] Enter uploads and shows ProgressBar

2. **+ Button Upload (File dialog)**
   - [ ] File appears in chat with purple dashed border (before Enter)
   - [ ] X button removes file
   - [ ] Enter uploads and shows ProgressBar

3. **Drag-Drop Upload**
   - [ ] File appears in chat with purple dashed border (before Enter)
   - [ ] X button removes file
   - [ ] Enter uploads and shows ProgressBar

4. **Multiple Files**
   - [ ] Up to 10 files can be staged
   - [ ] 11th file shows "Maximum 10 images" message

5. **Loading Animation**
   - [ ] ProgressBar shows during upload (0-100%)
   - [ ] ProgressBar hides when upload completes

6. **Mixed Content**
   - [ ] Text + images sent together on Enter
   - [ ] Each file gets its own draft item

## Todo List

- [ ] Test Ctrl+V paste
- [ ] Test + button upload
- [ ] Test drag-drop
- [ ] Test 10 file limit
- [ ] Test ProgressBar animation
- [ ] Test mixed text + images

## Success Criteria

All test cases pass. User can:
- Stage up to 10 images/files with visual feedback
- Remove any staged item before sending
- Press Enter to upload all with loading animation