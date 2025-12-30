#!/usr/bin/env pwsh
# Kitsune Viewer Build Script
# Creates two versions: Portable (self-contained) and Slim (framework-dependent)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Kitsune Viewer - Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Go to script directory
Push-Location $PSScriptRoot

try {
    # Clean previous builds
    Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path "publish") { Remove-Item -Recurse -Force "publish" }
    if (Test-Path "src\KitsuneViewer\bin") { Remove-Item -Recurse -Force "src\KitsuneViewer\bin" }
    if (Test-Path "src\KitsuneViewer\obj") { Remove-Item -Recurse -Force "src\KitsuneViewer\obj" }

    # Restore packages
    Write-Host "[2/4] Restoring packages..." -ForegroundColor Yellow
    dotnet restore src\KitsuneViewer\KitsuneViewer.csproj
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

    # Build Portable version (self-contained, single file)
    Write-Host "[3/4] Building Portable version (self-contained)..." -ForegroundColor Yellow
    dotnet publish src\KitsuneViewer\KitsuneViewer.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -o "publish\portable"
    if ($LASTEXITCODE -ne 0) { throw "Portable build failed" }

    # Build Slim version (framework-dependent)
    Write-Host "[4/4] Building Slim version (framework-dependent)..." -ForegroundColor Yellow
    dotnet publish src\KitsuneViewer\KitsuneViewer.csproj `
        -c Release `
        --self-contained false `
        -p:PublishSingleFile=false `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -o "publish\slim"
    if ($LASTEXITCODE -ne 0) { throw "Slim build failed" }

    # Show results
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""

    $portableExe = Get-Item "publish\portable\KitsuneViewer.exe"
    $slimExe = Get-Item "publish\slim\KitsuneViewer.exe"

    Write-Host "Portable version (includes .NET runtime):" -ForegroundColor White
    Write-Host "  publish\portable\KitsuneViewer.exe ($([math]::Round($portableExe.Length / 1MB, 1)) MB)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Slim version (requires .NET 8 runtime):" -ForegroundColor White
    Write-Host "  publish\slim\KitsuneViewer.exe ($([math]::Round($slimExe.Length / 1KB, 0)) KB)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Portable: Copy single .exe anywhere, runs on any Windows x64" -ForegroundColor DarkGray
    Write-Host "Slim: Smaller, but requires .NET 8 Desktop Runtime installed" -ForegroundColor DarkGray
    Write-Host "  Download: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor DarkGray
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build FAILED: $_" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}
