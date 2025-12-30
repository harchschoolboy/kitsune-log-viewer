# Installation Guide

## System Requirements

- Windows 10/11
- For slim version: [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## Portable Version (Recommended)

1. Download `KitsuneViewer.exe` from [Releases](../../releases)
2. Run the executable — no installation needed
3. Single file (~150 MB), includes everything

## Slim Version

1. Install [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Download slim version from [Releases](../../releases)
3. Extract and run `KitsuneViewer.exe`
4. Smaller download (~2 MB), requires system .NET runtime

## Command Line Usage

```bash
# Open specific files
KitsuneViewer.exe file1.log file2.log file3.log

# Open all .log files in directory
KitsuneViewer.exe *.log
```