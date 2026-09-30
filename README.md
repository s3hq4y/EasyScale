# EasyScale

A small utility to change the Windows **Settings → System → Display → Scale** setting quickly.
It lives in the system tray, opens a Fluent UI window on click, and supports named scale presets.

[简体中文](README.zh-CN.md)

## Features

- **Instant effect** — changing scale needs neither sign-out nor administrator rights.
- **Fluent UI** — built on [WPF-UI](https://wpfui.lepo.co/), follows the system theme.
- **Tray resident** — left-click restores the main window; right-click opens a menu (Show / Presets / Settings / Exit). Closing the window minimizes to tray.
- **Named presets** — save a monitor's scale as a preset and re-apply it in one click.
- **Multi-language** — Chinese (default) / English, switchable at runtime without restart.
- **Single-file portable build** — self-contained, config stored next to the executable.

## Tech stack

- .NET 8 + WPF
- WPF-UI 4.x (Fluent 2 controls + tray)
- CommunityToolkit.Mvvm (MVVM source generators)
- Core: `user32.dll` `DisplayConfigGetDeviceInfo` / `DisplayConfigSetDeviceInfo`

> Note: DPI scale read/write uses two **undocumented** info types (`-3` GET / `-4` SET).
> Both were verified on Windows 26H1 (build 28000): they take effect immediately and
> require no elevation. See [`docs/可行性评估.md`](docs/可行性评估.md) for the measurements.
> To guard against the interface changing across Windows versions, scale I/O is abstracted
> behind `IDpiScaleApplier`, so the implementation can be swapped wholesale.

## Project layout

```
src/
  EasyScale.Core/     # UI-agnostic core: interop, monitor enumeration, scale I/O
  EasyScale.App/      # WPF app: tray, Fluent UI, i18n, settings and presets
  EasyScale.Probe/    # Console verifier (cross-checks scale step <-> actual DPI)
tools/
  dpi-scale-probe.ps1 # PowerShell probe (-List / -Delta / -SetRel)
  make-icon.ps1       # Generates the tray/app icon (reproducible, no hard-coded binary)
docs/
  可行性评估.md        # Feasibility findings and measurements (Chinese)
```

## Build and run

Requires the .NET 8 SDK.

```powershell
# Run
dotnet run --project src/EasyScale.App

# Single-file portable build (output under publish/)
dotnet publish src/EasyScale.App -c Release -r win-x64 -o publish
```

## Verification

`EasyScale.Probe` independently verifies the scale step <-> actual DPI mapping:

```powershell
dotnet run --project src/EasyScale.Probe
```

> Note: cross-checking requires the caller to be DPI aware (PerMonitorV2).
> A console is not aware by default, so `GetDpiForMonitor` would always report 96 DPI.
> The probe declares awareness explicitly.

## Known limitations

- Windows only.
- Depends on undocumented APIs; if a future Windows build changes the struct layout,
  the size returned by `QueryDisplayConfig` will be the first thing to break.

## License

[MIT](LICENSE)
