---
title: "Phase 3 - MVVM Refactor"
description: "Create proper ViewModels with RelayCommand using CommunityToolkit.Mvvm"
status: pending
priority: P1
effort: 4h
branch: feature/emoji-reactions
tags: [refactoring, mvvm, community-toolkit]
created: 2026-05-20
---

# Phase 3: MVVM Refactor with RelayCommand

Convert all button click handlers and UI logic to proper MVVM with CommunityToolkit.Mvvm.

## Context Links

- [System Design](../system-design.md)
- [Plan Overview](../plan.md)
- [Phase 1](./phase-01-split-mainwindow.md)
- [Phase 2](./phase-02-cleanup-duplicates.md)

## Overview

Current implementation uses code-behind event handlers (e.g., `SendButton_Click`, `EmojiPickerButton_Click`). Need to convert to `ICommand` pattern using `RelayCommand` from CommunityToolkit.Mvvm.

## Key Insights

- CommunityToolkit.Mvvm already in tech stack but not fully utilized
- `RelayCommand` handles CanExecute for button enable/disable
- `ObservableObject` provides `PropertyChanged` notification
- Convert all event handlers in MainWindow.xaml.cs to commands

## Requirements

### Functional
- Convert SendButton_Click to SendCommand
- Convert EmojiPickerButton_Click to ToggleEmojiPickerCommand
- Convert ReactionButton_Click to ShowReactionPopupCommand
- Convert InfoButton_Click to ShowSettingsCommand
- Convert Channel selection to SelectChannelCommand
- Convert Image thumbnail click to OpenLightboxCommand
- Convert File thumbnail click to OpenFileCommand

### Non-Functional
- All existing keyboard shortcuts preserved
- Button enable/disable states work correctly

## Architecture

```
MainViewModel (with CommunityToolkit.Mvvm)
├── [ObservableProperty] Username
├── [ObservableProperty] InputText
├── [ObservableProperty] IsEmojiPickerOpen
├── [RelayCommand] SendMessage
├── [RelayCommand] ToggleEmojiPicker
├── [RelayCommand] ShowReactionPopup
├── [RelayCommand] ShowSettings
├── [RelayCommand] SelectChannel
└── [RelayCommand] OpenLightbox
```

## Related Code Files

### Files to Modify
- `ChatBox.Client/ViewModels/MainViewModel.cs`
- `ChatBox.Client/ViewModels/MessageListViewModel.cs`
- `ChatBox.Client/ViewModels/InputAreaViewModel.cs`
- `ChatBox.Client/MainWindow.xaml` - Update binding syntax

### Files to Check
- Any Manager classes that handle button clicks

## Implementation Steps

1. **Check if CommunityToolkit.Mvvm is installed** in ChatBox.Client csproj
2. **Read existing ViewModel files** to understand current structure
3. **Update MainViewModel.cs** - Add ObservableProperty attributes and RelayCommand attributes
4. **Update MessageListViewModel.cs** - Add message selection commands
5. **Update InputAreaViewModel.cs** - Add send command with CanExecute
6. **Update MainWindow.xaml** - Change event handlers to Command bindings
7. **Remove event handlers** from MainWindow.xaml.cs
8. **Compile and verify** - Ensure commands work

## Todo List

- [ ] Verify CommunityToolkit.Mvvm package in ChatBox.Client
- [ ] Read existing ViewModels to understand structure
- [ ] Update MainViewModel with [ObservableProperty] and [RelayCommand]
- [ ] Update MessageListViewModel with selection commands
- [ ] Update InputAreaViewModel with send command
- [ ] Update MainWindow.xaml Command bindings
- [ ] Remove event handler code from MainWindow.xaml.cs
- [ ] Compile and verify all commands work

## Success Criteria

- All button clicks use RelayCommand
- Keyboard shortcuts work (Enter to send, Ctrl+V to paste)
- No event handlers in MainWindow.xaml.cs
- All buttons enable/disable based on CanExecute

## Risk Assessment

- **Risk**: CommunityToolkit.Mvvm version mismatch
- **Mitigation**: Check csproj for version, update if needed
- **Risk**: Complex commands with parameters
- **Mitigation**: Use CommandParameter binding in XAML

## Security Considerations

- No security changes in this phase
- Preserve existing input validation

## Next Steps

- [Phase 4](./phase-04-add-di-container.md) - Add DI container
- [Phase 5](./phase-05-create-views-controls.md) - Extract XAML controls