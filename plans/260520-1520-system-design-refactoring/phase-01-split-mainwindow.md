---
title: "Phase 1 - Split MainWindow"
description: "Split MainWindow.xaml.cs (1534 lines) into MainViewModel and supporting classes"
status: pending
priority: P1
effort: 4h
branch: feature/emoji-reactions
tags: [refactoring, mvvm, mainwindow]
created: 2026-05-20
---

# Phase 1: Split MainWindow into ViewModels

Extract code-behind logic from MainWindow.xaml.cs into MainViewModel and supporting classes.

## Context Links

- [System Design](../system-design.md)
- [Plan Overview](../plan.md)

## Overview

MainWindow.xaml.cs (1534 lines) contains UI logic, socket handling, clipboard, emoji, gallery, file transfer, and connection management all in one file. This violates the 200-line rule and makes the code hard to maintain.

## Key Insights

- MainWindow.xaml.cs handles: socket messages, UI state, clipboard paste, emoji selection, lightbox, file transfer, gallery management
- Code uses _prefix for private fields and direct UI element access (this.messageList)
- Current architecture uses "Manager" classes for some concerns but still keeps too much logic in code-behind
- Need to extract into: MainViewModel, MessageListViewModel, InputAreaViewModel, StatusBarViewModel

## Requirements

### Functional
- Extract message handling logic to MessageListViewModel
- Extract input area logic (text, clipboard, send) to InputAreaViewModel
- Extract connection status to StatusBarViewModel
- Keep MainWindow.xaml.cs minimal (~200 lines)

### Non-Functional
- No runtime behavior changes
- All existing keyboard shortcuts and interactions preserved
- Maintain current color scheme and layout

## Architecture

```
MainWindow.xaml.cs (reduced)
├── MainViewModel
│   ├── MessageListViewModel
│   │   ├── Messages: ObservableCollection<MessageViewModel>
│   │   ├── SelectedMessage: MessageViewModel
│   │   └── AddMessage(), RemoveMessage()
│   ├── InputAreaViewModel
│   │   ├── InputText: string
│   │   ├── SendCommand: ICommand
│   │   └── PasteImageCommand: ICommand
│   └── StatusBarViewModel
│       ├── Username: string
│       ├── AvatarInitials: string
│       └── IsOnline: bool
```

## Related Code Files

### Files to Modify
- `ChatBox.Client/MainWindow.xaml.cs`
- `ChatBox.Client/MainWindow.xaml`

### Files to Create
- `ChatBox.Client/ViewModels/MainViewModel.cs`
- `ChatBox.Client/ViewModels/MessageListViewModel.cs`
- `ChatBox.Client/ViewModels/InputAreaViewModel.cs`
- `ChatBox.Client/ViewModels/StatusBarViewModel.cs`

## Implementation Steps

1. **Read MainWindow.xaml.cs** to understand current structure and identify extraction points
2. **Create folder structure** - `ChatBox.Client/ViewModels/`
3. **Create MainViewModel.cs** - Extract MainWindow field declarations and initialization
4. **Create MessageListViewModel.cs** - Extract message list management (AddMessage, RemoveMessage, messages collection)
5. **Create InputAreaViewModel.cs** - Extract input text, send command, paste handling
6. **Create StatusBarViewModel.cs** - Extract username, avatar, online status properties
7. **Wire up MainWindow.xaml** - DataContext = MainViewModel, update bindings
8. **Update MainWindow.xaml.cs** - Remove extracted logic, keep window management only
9. **Compile and verify** - Ensure no breaking changes

## Todo List

- [ ] Read MainWindow.xaml.cs to identify extraction points
- [ ] Create ViewModels folder
- [ ] Create MainViewModel.cs
- [ ] Create MessageListViewModel.cs
- [ ] Create InputAreaViewModel.cs
- [ ] Create StatusBarViewModel.cs
- [ ] Wire up MainWindow.xaml DataContext
- [ ] Reduce MainWindow.xaml.cs to ~200 lines
- [ ] Compile and verify existing functionality

## Success Criteria

- MainWindow.xaml.cs under 200 lines
- All extracted ViewModels compile without errors
- MainWindow DataContext bound to MainViewModel
- All existing functionality preserved (send message, receive message, clipboard paste)

## Risk Assessment

- **Risk**: Breaking existing message flow during extraction
- **Mitigation**: Extract one concern at a time, compile after each, test manually
- **Risk**: Large number of UI element references in code-behind
- **Mitigation**: Use x:Name bindings to ViewModel properties, remove direct element access

## Security Considerations

- No new network access or data handling in this phase
- Preserve existing input validation

## Next Steps

- [Phase 2](./phase-02-cleanup-duplicates.md) - Remove duplicate ChatMessage classes
- [Phase 3](./phase-03-mvvm-refactor.md) - Create proper ViewModels with RelayCommand