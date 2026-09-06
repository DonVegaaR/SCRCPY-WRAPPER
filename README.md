# Scrcpy Launcher

This folder contains a separate Windows launcher for `scrcpy`. It does not
modify the existing `app/` or `server/` code paths. Instead, it wraps
`scrcpy.exe`, maps common settings to CLI flags, and captures output in a WPF
window.

## What It Does

- Detects a `scrcpy` tools folder
- Lists devices via `adb devices -l`
- Saves named presets
- Builds and previews scrcpy arguments
- Launches and stops `scrcpy.exe`
- Streams stdout and stderr into the UI log

## Expected Folder Layout

The launcher works best when pointed at a portable Windows `scrcpy` folder that
contains:

- `scrcpy.exe`
- `scrcpy-server`
- `adb.exe`

That matches the final packaged Windows release layout.

## Build

```powershell
Set-Location launcher
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
$env:NUGET_PACKAGES = "$PWD\.nuget\packages"
dotnet restore .\ScrcpyLauncher.sln --configfile .\NuGet.Config
dotnet build .\ScrcpyLauncher.sln --no-restore
```

## Notes

- Presets are stored under `%LocalAppData%\ScrcpyLauncher\presets.json`.
- For a deeper integration like embedding the mirror window into the launcher,
  the native SDL side of `scrcpy` would need a more invasive rework.
