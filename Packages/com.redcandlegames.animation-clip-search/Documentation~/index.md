# Animation Clip Search Tool

## Overview

The Animation Clip Search Tool is a Unity Editor extension that adds powerful search functionality to Unity's Animation Window. It allows developers to quickly search and switch between animation clips using a keyboard shortcut, significantly improving workflow efficiency when working with animations.

## Features

- **Quick Access**: Press `Alt+S` (Windows/Linux) or `Option+S` (Mac) to open the search window
- **Smart Context**: Automatically searches clips from the current Animator Controller
- **Override Controller Support**: Properly handles AnimatorOverrideController, showing both base and overridden clips
- **Multi-Layer Support**: Searches through all layers in complex Animator Controllers
- **Keyboard Navigation**: Use Enter to select the first result, Escape to close
- **Filtered Results**: Automatically filters out internal Unity preview clips

## Installation

1. Open Unity Package Manager
2. Click the "+" button and select "Add package from git URL..."
3. Enter: `https://github.com/RED-CANDLE-GAMES-CO-LTD/Unity-Animation-Window-Search-Extension.git?path=Packages/com.redcandlegames.animation-clip-search`
4. Click "Add"

## Usage

### Basic Usage

1. Open the Animation Window in Unity
2. Select a GameObject with an Animator component
3. Press `Alt+S` (Windows/Linux) or `Option+S` (Mac)
4. Type to search for animation clips
5. Click on a clip or press Enter to apply it to the Animation Window

### Search Scope

The tool intelligently determines the search scope:

- **With Animator Controller**: Shows clips from the selected GameObject's Animator Controller
- **With Override Controller**: Shows both base controller clips and overridden clips (marked with "(Override)")
- **No Controller**: Falls back to searching all animation clips in the project

### Context Menu

You can also access the search tool by right-clicking in the Project window and selecting "Search Animation Clips".

## Technical Details

- **Minimum Unity Version**: 2019.4
- **Namespace**: `RedCandleGames.Editor`
- **Assembly**: Editor-only

## Troubleshooting

### Search window doesn't open

- Ensure the Animation Window is open and focused
- Check that you're using the correct keyboard shortcut for your platform

### Clips not appearing in search

- Verify that the selected GameObject has an Animator component
- Check that the Animator Controller is properly assigned
- For Override Controllers, ensure the base controller is valid

### Can't switch clips automatically

Some Unity versions may have different internal APIs. If automatic switching fails, the tool will select and ping the clip in the Project window for manual application.

## Support

For issues, feature requests, or contributions, please visit the [GitHub repository](https://github.com/RED-CANDLE-GAMES-CO-LTD/Unity-Animation-Window-Search-Extension).