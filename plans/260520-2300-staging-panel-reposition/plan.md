# Plan: Reposition Draft Staging Panel Above Input Box

## Summary
Move the draft staging panel from its current floating/scattered position to a dedicated slot **right above the message input box**, as a persistent horizontal strip. This makes staged images/files visible and organized, directly adjacent to where the user types.

## Context
- **Parent:** `260520-1652-image-file-draft-upload/plan.md` (image/file draft upload feature)
- **Issue:** After the recent fix (removing inline chat insertion), pasted/dragged images only go to `DraftStagingPanel`. But the panel is not clearly positioned near the input area - user wants it "near chat, not random panel."

## Current Layout (Grid.Row assignments in pnlChat)

| Row | Height | Content |
|-----|--------|---------|
| 0 | 50 | Chat Header |
| 1 | * | Message List / Galleries |
| 2 | Auto | [EMPTY - was staging panel] |
| 3 | Auto | Draft Staging Panel (currently scattered) |
| 4 | Auto | Input Area |

**Problem:** `Grid.Row="3"` for staging panel - no explicit positioning makes it appear "random."

## Target Layout

| Row | Height | Content |
|-----|--------|---------|
| 0 | 50 | Chat Header |
| 1 | * | Message List / Galleries |
| 2 | Auto | **Draft Staging Panel** (repositioned here, always visible when items exist) |
| 3 | Auto | Input Area (tight margin above) |

**Key change:** Swap staging panel to `Grid.Row="2"`, adjust margins so it sits directly above input area.

## Requirements
1. Staging panel appears **immediately above the input box** when items are staged
2. Panel is a **horizontal strip** of thumbnails (same 256x256 size)
3. Small, unobtrusive - doesn't block the chat messages
4. When collapsed (no pending images), takes **zero vertical space**
5. When expanded, pushes input box down naturally

## Architecture

### Current Row Definitions:
```xml
<Grid.RowDefinitions>
    <RowDefinition Height="50"/>      <!-- Header -->
    <RowDefinition Height="*"/>       <!-- Messages -->
    <RowDefinition Height="Auto"/>    <!-- [empty?] -->
    <RowDefinition Height="Auto"/>    <!-- Staging Panel -->
    <RowDefinition Height="Auto"/>    <!-- Input Area -->
</Grid.RowDefinitions>
```

### Target Row Definitions:
```xml
<Grid.RowDefinitions>
    <RowDefinition Height="50"/>      <!-- Header -->
    <RowDefinition Height="*"/>       <!-- Messages -->
    <RowDefinition Height="Auto"/>    <!-- Staging Panel (directly above input) -->
    <RowDefinition Height="Auto"/>    <!-- Input Area -->
</Grid.RowDefinitions>
```

**Remove one empty row (Row 2 currently empty).**

### XAML Changes:
1. Move `DraftStagingPanel` from `Grid.Row="3"` to `Grid.Row="2"`
2. Remove the empty `RowDefinition Height="Auto"` at row 2
3. Adjust margins: panel's top margin from ~10 to something like 8-10px
4. The input area `Grid.Row="4"` becomes `Grid.Row="3"`
5. Update `UpdateDraftPanelVisibility()` to properly show/hide with correct row behavior

## Implementation Steps

### Step 1: Edit MainWindow.xaml
- Remove the empty `RowDefinition` between messages and staging panel
- Move `DraftStagingPanel` to `Grid.Row="2"` (was row 3)
- Shift input area `Grid.Row` from 4 to 3
- Adjust panel margins to be tight against input area

### Step 2: Verify Code-behind
- `UpdateDraftPanelVisibility()` uses `Visibility.Visible/Collapsed` - already correct
- No code changes needed in `.cs` for positioning

## Success Criteria
1. Draft staging panel appears as a **horizontal strip directly above** the message input box
2. Panel is positioned at `Grid.Row="2"` in the chat panel's grid
3. When no pending images: panel hidden, input box at normal position
4. When pending images exist: panel visible, input box pushed down
5. Thumbnail size remains 256x256px

## Risk Assessment
- **Low risk** - pure XAML layout adjustment
- No logic changes, no interaction with upload/send flow
- Should not affect any existing functionality

## Unresolved
None - layout change is straightforward.