---
title: "Phase 2 - Cleanup Duplicates"
description: "Remove duplicate ChatMessage classes, consolidate into LocalChat.Core/Models"
status: pending
priority: P1
effort: 2h
branch: feature/emoji-reactions
tags: [refactoring, models, cleanup]
created: 2026-05-20
---

# Phase 2: Cleanup Duplicate Classes

Remove duplicate ChatMessage class definitions and consolidate into single location.

## Context Links

- [System Design](../system-design.md)
- [Plan Overview](../plan.md)
- [Phase 1](./phase-01-split-mainwindow.md)

## Overview

ChatMessage exists in two locations:
1. `LocalChat.Core/Models/ChatMessage.cs` (canonical, used by EF Core)
2. `ChatBox.Client/ViewModels/ChatMessage.cs` (duplicate)
3. Inline in MainWindow.xaml.cs (duplicate fields)

This duplication causes maintenance issues and confusion about which class to use.

## Key Insights

- Core/Models/ChatMessage.cs is used by EF Core for database operations
- Client/ViewModels/ChatMessage.cs is a lighter-weight client-side model
- MainWindow.xaml.cs inline defines sender ID, content, timestamp fields
- Need to decide: use Core model client-side OR create shared client model

## Requirements

### Functional
- Remove duplicate ChatMessage from ViewModels folder
- Remove inline ChatMessage definition from MainWindow.xaml.cs
- Ensure all usages reference LocalChat.Core/Models/ChatMessage
- Add any client-specific properties as partial/extension

### Non-Functional
- No runtime behavior changes
- All message serialization/deserialization preserved

## Architecture

```
Before (Duplicate):
├── LocalChat.Core/Models/ChatMessage.cs  ← Entity for EF Core
├── ChatBox.Client/ViewModels/ChatMessage.cs  ← Duplicate
└── MainWindow.xaml.cs inline fields  ← Duplicate

After (Consolidated):
└── LocalChat.Core/Models/ChatMessage.cs  ← Single source of truth
```

## Related Code Files

### Files to Modify
- `ChatBox.Client/MainWindow.xaml.cs` - Remove inline Message class
- `ChatBox.Client/ViewModels/ChatMessage.cs` - Delete file

### Files to Check for Usages
- `ChatBox.Client/MainWindow.xaml.cs`
- `ChatBox.Client/ViewModels/` (any files referencing ChatMessage)
- `ChatBox.Client/Managers/` (any managers using ChatMessage)

## Implementation Steps

1. **Search for all ChatMessage usages** in ChatBox.Client
2. **Read LocalChat.Core/Models/ChatMessage.cs** to understand structure
3. **Read ChatBox.Client/ViewModels/ChatMessage.cs** to see differences
4. **Check if client has extra properties** not in Core model
5. **If client has extra properties**, extend Core model or create LocalChatMessage client model
6. **Update all references** to use Core model
7. **Delete duplicate files**
8. **Compile and verify**

## Todo List

- [ ] Search for all ChatMessage usages in ChatBox.Client
- [ ] Read existing ChatMessage classes in both locations
- [ ] Identify any client-specific properties missing from Core model
- [ ] Extend Core model if needed (add client properties)
- [ ] Update all references in MainWindow.xaml.cs
- [ ] Update all references in ViewModels
- [ ] Update all references in Managers
- [ ] Delete ChatBox.Client/ViewModels/ChatMessage.cs
- [ ] Delete any inline Message class in MainWindow.xaml.cs
- [ ] Compile and verify

## Success Criteria

- Only one ChatMessage definition exists (in LocalChat.Core/Models/)
- No compile errors from reference changes
- All message handling uses consolidated model

## Risk Assessment

- **Risk**: Client needs properties not in Core model
- **Mitigation**: Add missing properties to Core model (they may be useful for server too)
- **Risk**: Breaking message serialization
- **Mitigation**: Verify message format matches protocol spec

## Security Considerations

- No security changes in this phase
- Ensure no sensitive data handling changes

## Next Steps

- [Phase 3](./phase-03-mvvm-refactor.md) - Create proper ViewModels with RelayCommand
- [Phase 1](./phase-01-split-mainwindow.md) - Can run in parallel if duplicate removal is simple