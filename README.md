# Volume Capper

A lightweight Windows system tray app that prevents your volume from exceeding a set limit.

## Features
- Sits silently in the system tray
- Snaps volume back down whenever it exceeds your cap
- Balloon tip notification when a cap is enforced (throttled to once per 5s)
- Toggle the cap on/off from the tray menu
- Settings window with a slider to adjust the cap level
- Optional run-at-startup via Windows registry

## Requirements
- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Build & Run

### Option A – Visual Studio
1. Open `VolumeCapper.sln`
2. Press F5 or Build → Start

### Option B – Command Line
```bash
cd VolumeCapper
dotnet run
```

### Option C – Publish a single .exe
```bash
cd VolumeCapper
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
The .exe will be in `bin/Release/net8.0-windows/win-x64/publish/`.

## Usage
- The app starts in the **system tray** (bottom-right, you may need to expand the tray arrow).
- **Double-click** the tray icon or right-click → **Settings…** to change the cap level.
- Right-click → **Cap Enabled** to toggle it on/off without opening settings.
- Right-click → **Exit** to quit.

## Settings are saved to
`%AppData%\VolumeCapper\settings.json`
