---
title: "Phase 5 - Create Views Controls"
description: "Extract XAML user controls: MessageBubble, EmojiPicker, ReactionPopup"
status: pending
priority: P2
effort: 6h
branch: feature/emoji-reactions
tags: [refactoring, xaml, user-controls]
created: 2026-05-20
---

# Phase 5: Create Views/Controls

Extract XAML into reusable user controls following the folder structure from system design.

## Context Links

- [System Design](../system-design.md)
- [Plan Overview](../plan.md)
- [Phase 4](./phase-04-add-di-container.md)

## Overview

Extract inline XAML into reusable controls:
1. MessageBubble - message display with avatar, reactions
2. UserAvatar - avatar with fallback initials
3. EmojiPicker - emoji selection grid popup
4. ReactionPopup - quick emoji reaction buttons

## Key Insights

- Current message items are defined inline in MessageList DataTemplate
- Emoji picker is a popup overlay defined in MainWindow
- Reaction buttons are inline in message template
- Extraction enables reuse and simplifies MainWindow.xaml

## Requirements

### Functional
- Create MessageBubble.xaml with sender, timestamp, content, reactions
- Create UserAvatar.xaml with initials fallback
- Create EmojiPicker.xaml with category tabs and emoji grid
- Create ReactionPopup.xaml with quick reaction buttons
- Create MessageList.xaml as composite component

### Non-Functional
- Maintain exact same visual appearance
- All bindings work correctly
- No duplicate visual elements

## Architecture

```
ChatBox.Client/Views/
├── Controls/
│   ├── MessageBubble.xaml + MessageBubble.xaml.cs
│   ├── UserAvatar.xaml + UserAvatar.xaml.cs
│   ├── EmojiPicker.xaml + EmojiPicker.xaml.cs
│   └── ReactionPopup.xaml + ReactionPopup.xaml.cs
├── Components/
│   ├── MessageList.xaml + MessageList.xaml.cs
│   ├── ImageGallery.xaml + ImageGallery.xaml.cs
│   ├── FileGallery.xaml + FileGallery.xaml.cs
│   ├── OnlineUsersPanel.xaml + OnlineUsersPanel.xaml.cs
│   └── ChannelSidebar.xaml + ChannelSidebar.xaml.cs
└── Overlays/
    ├── LightboxOverlay.xaml + LightboxOverlay.xaml.cs
    └── SettingsOverlay.xaml + SettingsOverlay.xaml.cs
```

## Related Code Files

### Files to Create
- `ChatBox.Client/Views/Controls/MessageBubble.xaml`
- `ChatBox.Client/Views/Controls/MessageBubble.xaml.cs`
- `ChatBox.Client/Views/Controls/UserAvatar.xaml`
- `ChatBox.Client/Views/Controls/UserAvatar.xaml.cs`
- `ChatBox.Client/Views/Controls/EmojiPicker.xaml`
- `ChatBox.Client/Views/Controls/EmojiPicker.xaml.cs`
- `ChatBox.Client/Views/Controls/ReactionPopup.xaml`
- `ChatBox.Client/Views/Controls/ReactionPopup.xaml.cs`

### Files to Modify
- `ChatBox.Client/MainWindow.xaml` - Use extracted controls
- `ChatBox.Client/MainWindow.xaml.cs` - Update event handlers to commands

## Implementation Steps

1. **Create folder structure** - Views/Controls, Views/Components, Views/Overlays
2. **Create UserAvatar.xaml** - Simplest control, start here
3. **Create MessageBubble.xaml** - Message display with bindings
4. **Create ReactionPopup.xaml** - Quick reaction overlay
5. **Create EmojiPicker.xaml** - Category tabs and emoji grid
6. **Update MainWindow.xaml** - Replace inline XAML with control references
7. **Update code-behind** - Convert to commands
8. **Compile and verify** - Visual inspection

## Todo List

- [ ] Create Views/Controls folder structure
- [ ] Create UserAvatar.xaml + code-behind
- [ ] Create MessageBubble.xaml + code-behind
- [ ] Create ReactionPopup.xaml + code-behind
- [ ] Create EmojiPicker.xaml + code-behind
- [ ] Create MessageList component
- [ ] Update MainWindow.xaml to use controls
- [ ] Compile and visual verify

## Success Criteria

- MainWindow.xaml simplified with control references
- All controls visually identical to previous inline version
- Bindings work correctly
- No duplicate XAML

## Risk Assessment

- **Risk**: Breaking existing bindings
- **Mitigation**: Test each control individually
- **Risk**: Style differences
- **Mitigation**: Copy exact styles from existing MainWindow.xaml

## Security Considerations

- No security changes in this phase
- All content display only

## Next Steps

- [Phase 6](./phase-06-refactor-core-services.md) - Split core service files
- Post-refactor: Add unit tests