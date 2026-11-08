# PauseMediaKey

A minimalist, elegant dark-mode Windows desktop utility that seamlessly remaps the physical `Pause / Break` (`Pause / Interr`) key into a global Windows Media `Play / Pause` control.

Designed with a clean, low-profile office slate gray aesthetic, low memory footprint, and deep system tray integration.

---

## Why PauseMediaKey?

On standard PC keyboards, the `Pause / Break` key is rarely used in modern workflows. However, remapping it natively via the Windows Registry (`Scancode Map`) is notoriously impossible because the Pause key sends a legacy multi-byte `E1 1D 45 ...` scancode sequence that the registry parser rejects.

`PauseMediaKey` solves this cleanly by installing an un-intrusive Windows low-level keyboard hook (`WH_KEYBOARD_LL`). When active:
- Pressing `Pause / Break` (or `Ctrl + Break`) intercepts the signal and emits a native `VK_MEDIA_PLAY_PAUSE` keystroke directly to the Windows System Media Transport Controls (SMTC).
- The original key event is swallowed so no default OS interruption occurs.
- Any active media playback—such as **Opera GX**, **Spotify**, **Chrome**, **YouTube**, or **Windows Media Player**—toggles Play/Pause instantly.

---

## Features

- **Minimalist Office Dark Theme**: Styled with refined dark charcoal and slate gray tones, high-contrast readable typography, and Windows 11 immersive dark title bar support.
- **One-Click Toggle**: Enable or disable the remap instantly with visual state confirmation.
- **System Tray Integration**:
  - Closes / minimizes to the notification area to keep your taskbar uncluttered.
  - Double-click to restore or right-click for quick toggle and exit actions.
  - Dynamic tray icon reflecting the active/inactive state.
- **Autostart Support**: Built-in option to launch minimized with Windows automatically.
- **Single Instance**: Ensures only one instance runs at any time, bringing the existing window forward if reopened.
- **Zero Overhead**: Consumes minimal system resources (< 15 MB RAM) with 0% idle CPU usage.

---

## Installation & Running

### Option 1: Pre-built Binary
1. Download the latest `v1.0.0.zip` from the [Releases](https://github.com/tiadiff/PauseMediaKey/releases) section.
2. Extract the archive to any folder of your choice.
3. Run `PauseMediaKey.exe`.

*Note: Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).*

### Option 2: Build from Source
Ensure [.NET 8 SDK](https://dotnet.microsoft.com/) is installed on your Windows machine:

```powershell
git clone https://github.com/tiadiff/PauseMediaKey.git
cd PauseMediaKey
dotnet build -c Release
```

The compiled binaries will be generated inside `bin/Release/net8.0-windows/`.

---

## Usage

1. Launch `PauseMediaKey.exe`.
2. The application begins with remapping **Active** by default.
3. Play music or a video in **Opera GX**, **Spotify**, or any media app.
4. Press `Pause / Interr` on your keyboard: playback will toggle immediately.
5. Click **Disable Remapping** to instantly restore the default Windows key behavior.

---

## Tech Stack

- **Platform**: Windows 10 / Windows 11
- **Framework**: .NET 8.0 (Windows Forms)
- **APIs**: User32 `SetWindowsHookEx` (`WH_KEYBOARD_LL`), `keybd_event`, Dwmapi `DwmSetWindowAttribute`

---

## License

This project is licensed under the [MIT License](LICENSE).
