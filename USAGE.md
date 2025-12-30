# Usage Guide

## Opening Files

### Methods
1. **Menu**: File → Open Files (`Ctrl+O`)
2. **Drag & Drop**: Drag log files directly onto the window
3. **Command Line**: `KitsuneViewer.exe file1.log file2.log file3.log`

### Dock Positioning
When dropping files with logs already open, visual indicators appear:
- **Left/Right/Top/Bottom**: Split view
- **Center**: Add as tab

## Toolbar Controls

### Main Toolbar
| Button | Function |
|--------|----------|
| 📁 Open | Open files dialog |
| 🔄 Time Sync | Global timestamp synchronization |
| 🎨 Theme | Cycle through themes |

### Per-Panel Toolbar
| Button | Description |
|--------|-------------|
| 📜 Follow | Auto-scroll to new content |
| ⏸ Pause | Pause log updates |
| 🔗 Sync | Enable timestamp sync for this panel |
| ↩ Wrap | Toggle word wrap |
| Filter | Real-time text filtering |
| ⇉ All | Apply filter to all tabs |
| 🗑 Clear | Clear log content |
| 📋 Copy | Copy all to clipboard |

## Timestamp Synchronization

### Setup
1. Enable **Time Sync** in main toolbar (global)
2. Enable **🔗 Sync** on panels you want to sync
3. Click any log entry with timestamp
4. All synced logs scroll to matching time

### Supported Formats
- ISO 8601: `2024-01-15T14:30:45.123Z`
- Standard: `2024-01-15 14:30:45.123`
- Bracketed: `[2024-01-15 14:30:45]`
- Log4j: `15 Jan 2024 14:30:45,123`
- Unix timestamps (seconds/milliseconds)

## Log Level Highlighting

Automatic coloring for:
- **ERROR** → Red
- **WARN** → Yellow
- **INFO** → Blue
- **DEBUG** → Gray
- **TRACE** → Light gray

## Sessions

### Auto-Save
- Layout and open files saved on exit
- Restored automatically on startup

### Named Sessions
- **File → Save Session**: Save with custom name
- **File → Sessions**: Load saved session
- Restores both files and panel layout

## Themes

Available themes (View → Theme):
- **Dark (VS Code)** — default
- **Monokai** — green/yellow syntax
- **Dracula** — purple/pink theme
- **Light** — white background
- **High Contrast** — accessibility

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open files |
| `Ctrl+W` | Close current tab |
| `Ctrl+Q` | Exit application |

## Filtering

### Per-Panel Filtering
1. Type in Filter textbox
2. Applies automatically as you type
3. Case-insensitive text search

### Apply to All
1. Set filter on any panel
2. Click **⇉ All** button
3. Same filter applied to all open tabs

## Panel Management

### Docking
- Drag panel tabs to reposition
- Drop zones show available positions
- Support for split views and tabs

### Word Wrap
- Toggle per panel with **↩ Wrap** button
- Useful for long log lines
- Setting persists in sessions