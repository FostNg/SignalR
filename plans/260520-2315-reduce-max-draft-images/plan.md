# Plan: Reduce Max Draft Images & Thumbnail Size

## Summary
1. Change max staged images from **10 → 5**
2. Reduce staging panel thumbnail size from **256x256 → 128x128** so multiple fit horizontally

## Context
- User: staging panel shows only one image, needs to show more
- Solution: shrink thumbnails from 256px to 128px so more fit side-by-side
- Also reducing max limit to 5 per user request

## Context
- User wants to limit staged images to max 5 (currently 10)
- Simple one-line change in two places

## Thumbnail Size Change (MainWindow.xaml)

Lines ~1368-1370:
```xml
<!-- Before -->
<Grid Width="256" Height="256" ...>
    <Image ... Width="256" Height="256"/>

<!-- After -->
<Grid Width="128" Height="128" ...>
    <Image ... Width="128" Height="128"/>
```

## Changes Summary

| File | Line | Change |
|------|------|--------|
| `MainWindow.xaml.cs` | ~1374 | `Count >= 10` → `Count >= 5` |
| `MainWindow.xaml.cs` | ~1431 | `Count >= 10` → `Count >= 5` |
| `MainWindow.xaml` | ~1368 | `Width="256" Height="256"` → `128x128` |
| `MainWindow.xaml` | ~1370 | `Width="256" Height="256"` → `128x128` |

## Success Criteria
1. Max 5 images staged at once
2. Thumbnails 128x128px (smaller, more fit horizontally)
3. Scroll appears if more than can fit in panel width
4. Build succeeds