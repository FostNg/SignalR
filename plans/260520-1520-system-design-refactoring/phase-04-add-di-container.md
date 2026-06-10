---
title: "Phase 4 - Add DI Container"
description: "Add Microsoft.Extensions.DependencyInjection for service registration"
status: pending
priority: P2
effort: 2h
branch: feature/emoji-reactions
tags: [refactoring, di, dependency-injection]
created: 2026-05-20
---

# Phase 4: Add DI Container

Add Microsoft.Extensions.DependencyInjection for loose coupling and testability.

## Context Links

- [System Design](../system-design.md)
- [Plan Overview](../plan.md)
- [Phase 3](./phase-03-mvvm-refactor.md)

## Overview

Services are currently created manually in MainWindow.xaml.cs. Adding DI container will:
1. Enable constructor injection
2. Improve testability (can substitute mocks)
3. Follow best practices for .NET applications
4. Reduce tight coupling between components

## Key Insights

- Services currently created as singletons in MainWindow constructor
- DI container will manage service lifetimes
- ViewModels can receive services via constructor
- Need to register: ChatClient, FileClient, Managers

## Requirements

### Functional
- Add Microsoft.Extensions.DependencyInjection NuGet package
- Register all services in DI container
- Update MainWindow to resolve services from container
- Update ViewModels to receive services via constructor

### Non-Functional
- Preserve existing service lifetimes (singleton for most)
- No breaking changes to existing code paths

## Architecture

```
App.xaml.cs
├── ConfigureServices()
│   ├── services.AddSingleton<IChatServer, ChatServer>()
│   ├── services.AddSingleton<IFileServer, FileServer>()
│   ├── services.AddSingleton<ConnectionManager>()
│   └── services.AddTransient<MainViewModel>()
└── serviceProvider (static)

MainWindow.xaml.cs
├── MainViewModel vm (resolved from container)
└── DataContext = vm
```

## Related Code Files

### Files to Modify
- `ChatBox.Client/App.xaml.cs`
- `ChatBox.Client/MainWindow.xaml.cs`
- `ChatBox.Client/ViewModels/MainViewModel.cs`
- `ChatBox.Client/ViewModels/MessageListViewModel.cs`

### Files to Create
- `ChatBox.Client/Services/ServiceConfiguration.cs` (optional, for organization)

## Implementation Steps

1. **Add NuGet package** - Microsoft.Extensions.DependencyInjection
2. **Read App.xaml.cs** to understand current startup
3. **Create DI configuration** in App.xaml.cs
4. **Register ChatServer, FileServer** as singletons
5. **Register Managers** as singletons
6. **Register ViewModels** as transient
7. **Update MainWindow.xaml.cs** - Remove manual service creation
8. **Update ViewModels** - Add constructor parameters
9. **Compile and verify**

## Todo List

- [ ] Add Microsoft.Extensions.DependencyInjection NuGet package
- [ ] Read App.xaml.cs to understand startup
- [ ] Create ConfigureServices method in App.xaml.cs
- [ ] Register ChatServer (IChatServer)
- [ ] Register FileServer (IFileServer)
- [ ] Register ConnectionManager
- [ ] Register GalleryManager
- [ ] Register LightboxManager
- [ ] Register other Managers
- [ ] Register ViewModels as transient
- [ ] Update MainWindow.xaml.cs to resolve from container
- [ ] Update ViewModels with constructor injection
- [ ] Compile and verify

## Success Criteria

- All services resolved via DI container
- No manual `new SomeManager()` calls in code
- ViewModels receive services via constructor
- Application starts and works correctly

## Risk Assessment

- **Risk**: Breaking existing singleton behavior
- **Mitigation**: Register as singleton explicitly
- **Risk**: Circular dependency
- **Mitigation**: Restructure if needed, use interface abstractions

## Security Considerations

- No security changes in this phase
- Container itself is secure

## Next Steps

- [Phase 5](./phase-05-create-views-controls.md) - Extract XAML controls
- [Phase 6](./phase-06-refactor-core-services.md) - Split core service files