# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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