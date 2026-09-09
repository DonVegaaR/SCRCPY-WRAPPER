# Scrcpy Launcher

A Windows GUI launcher for `scrcpy`. It wraps `scrcpy.exe`, maps common settings to CLI flags, and captures output in a WPF window.

## What It Does

- Auto-detects a `scrcpy` tools folder
- Lists connected Android devices via `adb devices -l`
- Saves named presets (settings profiles)
- Builds and previews scrcpy command-line arguments
- Launches and stops `scrcpy.exe`
- Streams stdout and stderr into a UI log

## Prerequisites

- Windows 10 or later
- .NET 8 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- A portable `scrcpy` folder containing:
  - `scrcpy.exe`
  - `scrcpy-server`
  - `adb.exe`

## Install

1. Download or clone this repository.
2. Navigate to the `launcher` folder.
3. Point the launcher at your portable `scrcpy` folder (it auto-detects if `scrcpy` is nearby).

No system-wide installation is required. The launcher runs as a standalone WPF app.

## Build

```powershell
Set-Location launcher
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
$env:NUGET_PACKAGES = "$PWD\.nuget\packages"
dotnet restore .\ScrcpyLauncher.sln --configfile .\NuGet.Config
dotnet build .\ScrcpyLauncher.sln --no-restore
```

The built executable is at:
`src\ScrcpyLauncher.App\bin\Debug\net8.0-windows\ScrcpyLauncher.App.exe`

## Use

1. Launch the app (`ScrcpyLauncher.App.exe`).
2. On first run, it auto-detects a nearby `scrcpy` folder. Use **Tools > Browse** or **Auto-detect** if needed.
3. Select your Android device from the **Device** page.
4. Configure flags on the **Launch** page (video, audio, resolution, FPS, etc.).
5. Optionally save your settings as a preset on the **Profile** page.
6. Click **Launch** to start scrcpy, **Stop** to end it.
7. View the generated command or log output via **View command** / **View logs** in the footer.

## Presets

Presets are saved to:
```
%LocalAppData%\ScrcpyLauncher\presets.json
```

You can create, save, and delete named presets to switch between configurations quickly.

## Notes

- The launcher does not modify the core scrcpy source code.
- For embedding the scrcpy mirror window directly into the launcher UI, the native SDL side would need deeper integration.
