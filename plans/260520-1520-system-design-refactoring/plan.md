---
title: "System Design Refactoring"
description: "Refactor ChatBox project: split MainWindow, remove duplicates, implement proper MVVM, add DI container"
status: pending
priority: P1
effort: 24h
branch: feature/emoji-reactions
tags: [refactoring, mvvm, architecture]
created: 2026-05-20
---

# System Design Refactoring Plan

Split monolithic MainWindow.xaml.cs into proper MVVM architecture with CommunityToolkit.Mvvm.

## Phases

| Phase | Status | Description |
|-------|--------|-------------|
| [phase-01-split-mainwindow.md](./phase-01-split-mainwindow.md) | pending | Split MainWindow.xaml.cs into MainViewModel and supporting ViewModels |
| [phase-02-cleanup-duplicates.md](./phase-02-cleanup-duplicates.md) | pending | Remove duplicate ChatMessage classes, consolidate |
| [phase-03-mvvm-refactor.md](./phase-03-mvvm-refactor.md) | pending | Create proper ViewModels with RelayCommand |
| [phase-04-add-di-container.md](./phase-04-add-di-container.md) | pending | Add Microsoft.Extensions.DependencyInjection |
| [phase-05-create-views-controls.md](./phase-05-create-views-controls.md) | pending | Extract XAML user controls (MessageBubble, EmojiPicker, etc.) |
| [phase-06-refactor-core-services.md](./phase-06-refactor-core-services.md) | pending | Split large service files into individual classes |

## Key Dependencies

- Phase 1 → Phase 3 (ViewModel extraction)
- Phase 2 → Phase 3 (consolidated models)
- Phase 3 → Phase 4 (DI wiring)
- Phase 4 → Phase 5 (DI-based view construction)

## Success Criteria

- MainWindow.xaml.cs under 200 lines
- No duplicate model classes
- All UI logic uses RelayCommand pattern
- All services registered in DI container
- All XAML controls extracted to Views/Controls/

## Notes

- Keep existing functionality working after each phase
- Commit after each phase for easy rollback
- Use CommunityToolkit.Mvvm for ObservableObject and RelayCommand