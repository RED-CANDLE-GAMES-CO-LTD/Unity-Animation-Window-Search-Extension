# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0-preview] - 2025-08-08

### Added
- Experimental UI injection feature to integrate search directly into Animation Window
- Real-time search with dropdown results (limited to 10 items)
- Debug tools for diagnosing injection issues across Unity versions
- Automatic fallback to popup window when injection fails
- Support for Unity 2019.4+ with conditional compilation for newer features

### Changed
- Updated package version to preview status to indicate experimental features
- Enhanced Alt+S shortcut to try integrated search first, then fallback to popup

### Fixed
- Added conditional compilation for TextField.placeholder (Unity 2021.2+)
- Added missing .meta files for Unity asset recognition

### Technical
- Implemented UI Toolkit-based injection using rootVisualElement
- Added delayed injection to ensure window initialization
- Enhanced error logging with Unity version information

## [1.0.4] - 2025-08-08

### Fixed
- Added error handling for window creation to prevent ScriptableObject errors
- More graceful error messages when window creation fails
- Suggests remediation steps (reimport package or restart Unity)
- Fixed compilation error: missing selectedGO variable declaration in OnGUI

## [1.0.3] - 2025-08-08

### Changed
- Window positioning now centers on Animation Window for better visibility
  - Search window overlaps the center of Animation Window horizontally
  - Maintains same Y position (height alignment) as Animation Window
- Improved Override Controller clip handling
  - Non-overridden base animations now appear in search results by default
  - Only clips that are actually overridden are filtered when "Hide Overridden Clips" is checked
  - More accurate tracking of which specific clips have overrides

### Fixed
- Override Controller now correctly shows all available clips (base + overrides)
- Base animations without overrides are no longer incorrectly hidden
- Fixed duplicate clips appearing in search results
- Fixed "Hide Overridden Clips" checkbox not properly filtering clips
- Improved UI spacing to prevent elements from overlapping

### Improved
- "Hide Overridden Clips" checkbox now only appears when there are actually overridden clips
- Cleaner UI when using Override Controllers without any actual overrides

## [1.0.2] - 2025-08-08

### Added
- "Hide Overridden Clips" checkbox when using AnimatorOverrideController
  - By default, hides clips that have been overridden from the base controller
  - Can be unchecked to show all clips including the overridden ones
  - User preference is saved and persisted across sessions

### Changed
- Search window now tracks which clips are overridden when using Override Controllers
- Filter logic updated to respect the hide overridden clips preference

### Improved
- Clip Search window now opens aligned with the Animation Window
  - Attempts to position to the right of Animation Window if space permits
  - Falls back to left side or overlapping if screen space is limited
  - Centers on screen if Animation Window is not found

## [1.0.1] - 2025-08-08

### Added
- Support for AnimatorOverrideController
  - Now properly detects and handles override controllers
  - Shows both base controller clips and overridden clips
  - Displays "(Override)" suffix in search scope for clarity

### Fixed
- Animation clips from override controllers are now correctly discovered and searchable

### Technical
- Enhanced `RefreshClipList()` method to handle both AnimatorController and AnimatorOverrideController
- Updated search scope display to indicate when using override controllers

## [1.0.0] - 2025-08-08

### Added
- Initial release of Animation Clip Search Tool
- Quick search functionality for animation clips with Alt+S (Option+S on Mac)
- Smart context-aware clip discovery from current Animator Controller
- Support for multi-layer Animator Controllers
- Automatic filtering of internal Unity preview clips
- Keyboard navigation (Enter to select, Escape to close)
- Context menu integration in Project window
- Fallback to project-wide search when no controller is selected

### Features
- Centers search window on screen (300x400 pixels)
- Alphabetical sorting of search results
- Real-time filtering as you type
- Automatic focus on search field when window opens
- Attempts multiple methods to apply clips to Animation Window via reflection