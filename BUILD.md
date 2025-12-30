# Build Instructions

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11
- Git

## Quick Build

```powershell
# Clone repository
git clone https://github.com/yourusername/kitsune-viewer.git
cd kitsune-viewer

# Build both versions using PowerShell script
.\build.ps1

# Or use batch script
build.cmd
```

## Output

- `publish/portable/KitsuneViewer.exe` — Self-contained (~150 MB)
- `publish/slim/` — Framework-dependent (~2 MB + .NET 8 requirement)

## Manual Build Commands

### Development Build
```bash
# Restore packages
dotnet restore

# Build debug
dotnet build

# Run in development
dotnet run --project src/KitsuneViewer/KitsuneViewer.csproj
```

### Release Build
```bash
# Portable (self-contained)
dotnet publish src/KitsuneViewer/KitsuneViewer.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/portable

# Slim (framework-dependent)
dotnet publish src/KitsuneViewer/KitsuneViewer.csproj -c Release -o publish/slim
```

## Build Scripts

### PowerShell (`build.ps1`)
- Colored output
- Shows file sizes
- Error handling
- Automatic cleanup

### Batch (`build.cmd`)
- Windows batch compatible
- Simple output
- Cross-compatible