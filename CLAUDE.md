# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity Editor extension that adds search functionality to Unity's Animation Window. The plugin allows users to quickly search and switch between animation clips using Alt+S (Option+S on Mac).

## Package Structure

The Unity package is located at:
- `Packages/com.redcandlegames.animation-clip-search/`

Key components:
- `AnimationClipSearchTool.cs` - Main search window implementation
- `AnimationWindowExtensions.cs` - Hooks into Unity's Animation Window for keyboard shortcuts
- `RedCandleGames.AnimationClipSearch.Editor.asmdef` - Assembly definition for the package

## Architecture Details

### AnimationClipSearchTool
- Uses Unity reflection to interact with the internal `AnimationWindow` class
- Attempts multiple approaches to set the active animation clip:
  1. Direct property access (`activeAnimationClip`)
  2. Internal method invocation (`SwitchClip`, `ChangeClip`, `SetClip`)
  3. Fallback to selection and ping
- Window positioning: Centers on screen with 300x400 pixel dimensions
- Implements keyboard navigation (Enter to select first result, Escape to close)

### AnimationWindowExtensions
- Uses `[InitializeOnLoad]` to hook into editor updates
- Monitors keyboard input during `EditorApplication.update`
- Only activates when Animation Window is focused
- Provides alternative menu items for opening search (though removed from main menu)
- Contains fallback methods for applying clips to both modern Animator and legacy Animation components

### Smart Clip Discovery
The search tool prioritizes clips from the current Animator Controller:
1. Searches for Animator in selected GameObject hierarchy (self, parents, children)
2. Extracts clips from all layers, states, and blend trees
3. Falls back to project-wide search if no controller is found
4. Filters out internal Unity clips (those starting with "__preview__")
5. Sorts results alphabetically for consistency

### Context Menu Integration
- Adds "Search Animation Clips" to Assets context menu
- Available via right-click in Project window

## Key Technical Considerations

- **Namespace**: `RedCandleGames.Editor`
- **Minimum Unity Version**: 2019.4
- **Editor-only**: All code runs in Editor assembly
- **No menu items**: Search is only accessible via Alt+S shortcut
- **Reflection-heavy**: Due to Unity's internal Animation Window API

## Common Tasks

There are no build, test, or lint commands for this Unity package. Development is done directly in Unity Editor.

To modify the package:
1. Edit files in `Packages/com.redcandlegames.animation-clip-search/Editor/`
2. Unity will automatically recompile on file changes
3. Test by opening Animation Window and pressing Alt+S

## Important Files Outside Package

- `/Editor/` folder contains the original source files (before package conversion)
- These files should be kept in sync with the package version or removed to avoid confusion

## Distribution

The package is distributed via git URL:
```
https://github.com/RED-CANDLE-GAMES-CO-LTD/Unity-Animation-Window-Search-Extension.git?path=Packages/com.redcandlegames.animation-clip-search
```