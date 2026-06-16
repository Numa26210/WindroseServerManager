## What's new in v1.7.0

### 📋 Server Log Viewer (issue)
- Dedicated **Logs** page with real-time tailing, filtering, export, and "Open Logs Folder".
- Removed log clutter from the Server Control page for a cleaner layout.

### 🎨 Customizable App Skin
- **Settings → General** tab: choose between **Classic** (blue accent) and **Stitch** (green accent) themes.
- Skins persist across restarts.

### 🛡️ Server Watchdog
- The app can now **auto-start** your server and keep it running.
- Toggle desired server state per server in settings; configure auto-start delay.

### 🔄 Enhanced Restart Flow
- **Native broadcast messages** before restart (customizable message text).
- Optional **Steam update install** before restart.
- Optional **backup creation** before restart.

### ⚙️ Editor Improvements
- Disabled unsupported fields (`stack_size`, `inventory_size`, `weight`) with visual indicator.
- Editor now automatically syncs `http_port` and `rcon.password` to Windrose+ settings on save.

### 🖥️ Remote Windrose+ Dashboard
- Configure a **remote host** per server (e.g., `192.168.1.50`) for servers running on other machines.
- Falls back to `localhost` when no remote host is configured.

### 🤖 Discord Enhancements
- **Player join/leave tracking** — optional notification (Settings → Discord → "Player Join/Leave").
- **Crash notifications** now include the **last 10 log lines** in an embed for faster diagnosis.

### 🎮 Headless & CLI
- Server launches in **headless mode** (`.bat`/`.cmd`) with `cmd.exe /c`, no visible console window.
- Named pipe **IPC endpoint** with `start`, `stop`, `restart`, `backup`, `status`, `shutdown` commands.

### 🔧 Other
- Dashboard timer switched from `System.Timers.Timer` to `DispatcherTimer` (fixes UI thread issues).
- Backups list sorted by creation date (newest first).
- Settings page reorganized into 4-tab layout (General / Server / System / Windrose+).
- Config value parsing consolidated into `WindroseConfigValueHelper` (shared `TryReadString`/`TryReadInt`).

### Upgrade
Simply replace the existing exe. No settings migration needed.
