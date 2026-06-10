---
title: "Phase 6 - Refactor Core Services"
description: "Split ChatService.cs and FileTransferService.cs into individual classes"
status: pending
priority: P3
effort: 6h
branch: feature/emoji-reactions
tags: [refactoring, services, core]
created: 2026-05-20
---

# Phase 6: Refactor Core Services

Split large service files (containing 2+ classes each) into individual class files.

## Context Links

- [System Design](../system-design.md)
- [Plan Overview](../plan.md)
- [Phase 5](./phase-05-create-views-controls.md)

## Overview

Current service files:
- `ChatService.cs` contains `ChatServer` + `ChatClient` classes
- `FileTransferService.cs` contains `FileServer` + `FileClient` classes

Each file violates the 200-line rule. Split into individual files per class.

## Key Insights

- Each class already has distinct responsibility
- ChatServer handles server-side TCP messaging
- ChatClient handles client-side TCP messaging
- FileServer handles file upload reception
- FileClient handles file download
- Split is straightforward file organization

## Requirements

### Functional
- Split ChatService.cs into ChatServer.cs and ChatClient.cs
- Split FileTransferService.cs into FileServer.cs and FileClient.cs
- Update using statements in all referencing files
- Preserve all existing functionality

### Non-Functional
- No behavioral changes
- Same class public interfaces
- Same internal implementation

## Architecture

```
LocalChat.Core/Services/ (Before)
├── ChatService.cs           # Contains ChatServer + ChatClient (~500 lines)
└── FileTransferService.cs   # Contains FileServer + FileClient (~500 lines)

LocalChat.Core/Services/ (After)
├── ChatServer.cs            # Single class (~250 lines)
├── ChatClient.cs            # Single class (~250 lines)
├── FileServer.cs            # Single class (~250 lines)
└── FileClient.cs            # Single class (~250 lines)
```

## Related Code Files

### Files to Delete
- `LocalChat.Core/Services/ChatService.cs`
- `LocalChat.Core/Services/FileTransferService.cs`

### Files to Create
- `LocalChat.Core/Services/ChatServer.cs`
- `LocalChat.Core/Services/ChatClient.cs`
- `LocalChat.Core/Services/FileServer.cs`
- `LocalChat.Core/Services/FileClient.cs`

### Files to Modify
- Any files using `using LocalChat.Core.Services;`
- ChatBox.Client (references ChatClient, FileClient)
- ChatBox.Server (references ChatServer, FileServer)

## Implementation Steps

1. **Read ChatService.cs** to understand ChatServer and ChatClient classes
2. **Read FileTransferService.cs** to understand FileServer and FileClient classes
3. **Create ChatServer.cs** - Extract ChatServer class to new file
4. **Create ChatClient.cs** - Extract ChatClient class to new file
5. **Create FileServer.cs** - Extract FileServer class to new file
6. **Create FileClient.cs** - Extract FileClient class to new file
7. **Search for all files using ChatService.cs and FileTransferService.cs**
8. **Update using statements** in all referencing files
9. **Delete original files**
10. **Compile and verify**

## Todo List

- [ ] Read ChatService.cs to identify class boundaries
- [ ] Read FileTransferService.cs to identify class boundaries
- [ ] Create ChatServer.cs
- [ ] Create ChatClient.cs
- [ ] Create FileServer.cs
- [ ] Create FileClient.cs
- [ ] Search for all usages of original files
- [ ] Update using statements in ChatBox.Client
- [ ] Update using statements in ChatBox.Server
- [ ] Delete ChatService.cs
- [ ] Delete FileTransferService.cs
- [ ] Compile and verify

## Success Criteria

- All services in individual files under 200 lines
- No compile errors from reference changes
- All functionality preserved

## Risk Assessment

- **Risk**: Breaking namespace or using statements
- **Mitigation**: Copy exact namespace, update using statements
- **Risk**: Partial class issues
- **Mitigation**: Verify no partial class usage

## Security Considerations

- No security changes in this phase
- Refactoring only

## Next Steps

- Post-refactor: Add unit tests
- Post-refactor: Consider adding integration tests with test container