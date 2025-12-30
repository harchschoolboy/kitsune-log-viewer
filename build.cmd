@echo off
REM Kitsune Viewer Build Script
REM Creates two versions: Portable (self-contained) and Slim (framework-dependent)

setlocal
cd /d "%~dp0"

echo.
echo ========================================
echo   Kitsune Viewer - Build Script
echo ========================================
echo.

REM Clean previous builds
echo [1/4] Cleaning previous builds...
if exist "publish" rmdir /s /q "publish"
if exist "src\KitsuneViewer\bin" rmdir /s /q "src\KitsuneViewer\bin"
if exist "src\KitsuneViewer\obj" rmdir /s /q "src\KitsuneViewer\obj"

REM Restore packages
echo [2/4] Restoring packages...
dotnet restore src\KitsuneViewer\KitsuneViewer.csproj
if errorlevel 1 goto error

REM Build Portable version (self-contained, single file)
echo [3/4] Building Portable version (self-contained)...
dotnet publish src\KitsuneViewer\KitsuneViewer.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "publish\portable"
if errorlevel 1 goto error

REM Build Slim version (framework-dependent)
echo [4/4] Building Slim version (framework-dependent)...
dotnet publish src\KitsuneViewer\KitsuneViewer.csproj ^
    -c Release ^
    --self-contained false ^
    -p:PublishSingleFile=false ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "publish\slim"
if errorlevel 1 goto error

REM Show results
echo.
echo ========================================
echo   Build completed successfully!
echo ========================================
echo.
echo Portable version (includes .NET runtime):
for %%I in ("publish\portable\KitsuneViewer.exe") do echo   publish\portable\KitsuneViewer.exe (%%~zI bytes)
echo.
echo Slim version (requires .NET 8 runtime):
for %%I in ("publish\slim\KitsuneViewer.exe") do echo   publish\slim\KitsuneViewer.exe (%%~zI bytes)
echo.
echo Portable: Copy single .exe anywhere, runs on any Windows x64
echo Slim: Smaller, but requires .NET 8 Desktop Runtime installed
echo   Download: https://dotnet.microsoft.com/download/dotnet/8.0
echo.

goto end

:error
echo.
echo ========================================
echo   Build FAILED!
echo ========================================
exit /b 1

:end
endlocal
