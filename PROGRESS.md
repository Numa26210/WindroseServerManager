# Windrose Server Manager — Progress

**Fork by [Numa26210](https://github.com/Numa26210) · Status: Active · Current: v1.6.0**

> This is a community fork of [ManuelStaggl/WindroseServerManager](https://github.com/ManuelStaggl/WindroseServerManager), actively maintained and extended beyond the upstream.

---

## ✅ Completed (v1.0.0 → v1.6.0)

### Core
- [x] Avalonia Desktop UI (.NET 9, Semi.Avalonia)
- [x] MVVM via CommunityToolkit.Mvvm
- [x] DI via Microsoft.Extensions.Hosting
- [x] Serilog (File-Sink, rolling daily, 7 days retention)
- [x] Global crash handler (`%LocalAppData%\WindroseServerManager\crashes\`)
- [x] Atomic settings write (`.tmp` + `File.Move`)

### UI
- [x] Dark/Light Theme (Amber accent)
- [x] English + German (auto-detected from Windows language)
- [x] Custom window chrome (Drag, Min/Max/Close)
- [x] Navigation sidebar
- [x] Toast notifications (Info 3s, Success 3s, Error 15s)
- [x] Dashboard with live metrics
- [x] Onboarding card (first run)
- [x] Crash warning card (last 7 days)
- [x] About dialog (version, links, license)

### Server Management
- [x] SteamCMD auto-install
- [x] Windrose server install/update via SteamCMD
- [x] Start / Graceful-Stop / Force-Kill
- [x] Auto-restart on crash
- [x] Live stdout/stderr log
- [x] Configurable launch args
- [x] Cancel button for in-progress updates *(v1.3.0)*
- [x] SteamCMD non-zero exit code correctly treated as failure *(v1.3.0)*
- [x] Real-time bootstrap logs during SteamCMD self-update *(v1.3.0)*

### Configuration
- [x] `ServerDescription.json` editor
- [x] `WorldDescription.json` editor
- [x] Invite code generator
- [x] Active world selection
- [x] `P2pProxyAddress` auto-heal on launch (prevents gRPC crash) *(v1.2.3)*

### Backups
- [x] Manual backup (ZIP)
- [x] Auto-backup (configurable interval, min 5 min)
- [x] Retention (MaxBackupsToKeep)
- [x] Restore with safety snapshot
- [x] Confirm dialog for destructive actions
- [x] Backup on scheduled/threshold-based restart *(v1.3.0)*

### System Integration
- [x] Windows Firewall one-click rule
- [x] Daily auto-restart scheduler
- [x] Update check vs. Steam build ID
- [x] Tray icon (Show/Start/Stop/Quit)
- [x] Autostart via HKCU Run-Key
- [x] `--tray` / `--minimized` launch arg

### Discord Bot *(v1.3.0)*
- [x] Background service via Discord.Net 3.13.0
- [x] Live server status as Discord Activity
- [x] Session history events forwarded to configurable channel
- [x] Slash commands: `/status`, `/start`, `/stop`, `/restart`, `/backup`, `/backuprestart`, `/update`
- [x] Anti-spam log buffering (batch every 3s)
- [x] Full EN/DE localization for all bot responses
- [x] Configuration UI in Settings page (Token, Guild ID, Log Channel ID)

### Security & Performance *(v1.5.0)*
- [x] RCON command injection prevention (sanitize `\n`, `\r`, `\0` in kick/ban/broadcast)
- [x] PowerShell script SHA-256 integrity verification before execution
- [x] Strict SHA-256 digest enforcement for downloads (fail instead of warn)
- [x] Deadlock fix in RestartScheduler (`async Task` with `await`)
- [x] HttpClient reuse (single `_loginClient` instance)
- [x] Async semaphore in session management (`WaitAsync` instead of `Wait`)
- [x] Correct Windrose+ release URL (`github.com/HumanGenome/WindrosePlus`)

### Windrose+ QoL *(v1.6.0)*
- [x] Windrose+ toggle on/off without reinstalling (preserves files on disable, prompts install on enable)
- [x] Version pinning with ComboBox dropdown (paginated GitHub releases, "(Latest)" = auto)

### Per-Server Folders *(v1.6.0)*
- [x] `BackupDirOverride` / `ModsDirOverride` on `ServerEntry`
- [x] Folder picker + reset buttons in Settings UI
- [x] Backward compatible (null override = global default)

### System Tray *(v1.6.0)*
- [x] Close-to-tray setting (window hides instead of quitting)
- [x] Start fully minimized with `--tray` / `--minimized`
- [x] Native close button intercepted via `OnClosing` override
- [x] Tray icon uses app logo instead of Avalonia default

### QoL Editor *(v1.6.0)*
- [x] 10 categorized Windrose+ multiplier sliders (xp, loot, stack_size, weight, etc.)
- [x] Save/Reset, conflict detection banner
- [x] Mod conflict scanner (per-mod warnings + editor banner)

### UI & Config *(v1.6.0)*
- [x] Resizable main window (MinWidth=900, MinHeight=600)
- [x] Configurable auto-start delay (0-60 seconds)
- [x] Fork credit in About dialog and Settings
- [x] App Update points to Numa26210 fork releases

### Release Infrastructure *(v1.6.0)*
- [x] Bump version to 1.6.0
- [x] Installer updated (author, URL, version)
- [x] GitHub release v1.6.0 published

### Test Coverage *(v1.6.0)*
- [x] **235 passing tests** (up from 215 in v1.5.0)
- [x] Per-server folder overrides, CloseToTray, W+ toggle, version pinning — 11 new tests

---

## 🔄 Roadmap

### v1.7.0 — Automation & CLI *(next)*
- [ ] CLI/CMDlets for clean shutdown, backup trigger, scheduled restart
- [ ] Native daily restart with auto-backup (no more `taskkill` workaround)

### v2.x — Future
- [ ] Multi-server concurrent launch
- [ ] WebUI for remote management
- [ ] Code-signing certificate for installer
- [ ] Per-server port configuration in UI
