# PauseMediaKey

A minimalist, elegant dark-mode Windows utility designed with a decoupled architecture: an independent silent background worker service (`PauseMediaService.exe`) and a companion desktop management controller (`PauseMediaKey.exe`).

It seamlessly remaps the physical `Pause / Break` (`Pause / Interr`) key into a global Windows Media `Play / Pause` control for **Opera GX**, **Spotify**, **Chrome**, **YouTube**, and all Windows media players.

---

## Architecture

The project is split into two independent components:

```
[Windows Startup]
        │
        ▼
[PauseMediaService.exe] ── (Silent Background Worker, no UI, ~12 MB RAM)
        │                  Installs WH_KEYBOARD_LL hook & fires Play/Pause
        │
        ▲ (IPC via EventWaitHandle & JSON Config)
        │
[PauseMediaKey.exe] ───── (Dark Minimal Dashboard / Form Controller)
                           Open anytime to toggle or configure;
                           closing the window leaves the service running!
```

1. **`PauseMediaService.exe`**:
   - Runs headlessly in the background with 0% CPU and zero UI.
   - Automatically starts with Windows (`HKCU\...\Run`).
   - Handles the low-level keyboard hook (`WH_KEYBOARD_LL`).
   - Keeps remapping active even if the GUI window is never opened or is closed.

2. **`PauseMediaKey.exe`**:
   - Minimalist office slate dark UI (`#18191C` / `#222529`).
   - Windows 11 immersive dark title bar support.
   - Shows live service status, process PID, and last-triggered timestamp.
   - Provides a one-click toggle to enable or disable the remap in real time.
   - Closing the window (`X`) does not interrupt the service.

---

## Features

- **Decoupled Background Service**: The service keeps running independently in the background; you don't need to keep any window open or clutter the taskbar/tray.
- **Instant Toggle**: Enable or disable remapping on the fly with 0 ms latency without restarting the service.
- **Autostart with Windows**: The service automatically registers to run silently on boot.
- **Low Overhead**: Extremely lightweight (< 15 MB RAM total) with no idle CPU usage.
- **Opera GX & Browser Friendly**: Tested and compatible with Opera GX, Chrome, Spotify, YouTube, and Windows Media Transport Controls.

---

## Installation & Running

### Pre-built Binary
1. Download the latest `v1.0.0.zip` from [Releases](https://github.com/tiadiff/PauseMediaKey/releases).
2. Extract the archive.
3. Run `PauseMediaKey.exe` to open the control panel. The background service will launch automatically and configure autostart with Windows.
4. Close the window whenever you like—remapping will continue in the background.

*Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).*

### Build from Source
```powershell
git clone https://github.com/tiadiff/PauseMediaKey.git
cd PauseMediaKey
dotnet build -c Release
```

All binaries will be placed in `bin/Release/net8.0-windows/`.

---

## Tech Stack

- **Platform**: Windows 10 / Windows 11
- **Framework**: .NET 8.0 (Windows Forms, headless worker + GUI controller)
- **APIs**: User32 `SetWindowsHookEx` (`WH_KEYBOARD_LL`), `keybd_event`, Dwmapi `DwmSetWindowAttribute`
- **IPC**: Named `EventWaitHandle` + LocalAppData JSON configuration

---

## License

This project is licensed under the [MIT License](LICENSE).
