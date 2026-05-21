# Windrose Server Manager — Progress

**Fork by [Numa26210](https://github.com/Numa26210) · Status: Active · Current: v1.5.0**

> This is a community fork of [ManuelStaggl/WindroseServerManager](https://github.com/ManuelStaggl/WindroseServerManager), actively maintained and extended beyond the upstream.

---

## ✅ Completed (v1.0.0 → v1.5.0)

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

### Test Coverage
- [x] 215 passing tests (up from 151 in v1.2.x)
- [x] BackupService — 22 tests (create/restore/delete/retention, safety snapshot)
- [x] ServerConfigService — 25 tests (load/save/delete, atomic write, edge cases)
- [x] ServerEventLog — 9 tests (append/read/clear, corruption resilience, 50-writer concurrency)
- [x] RCON sanitization — 8 tests

### Release Infrastructure
- [x] Version in csproj
- [x] `scripts/publish.ps1` — Self-contained single-file
- [x] `scripts/build-release.ps1` — ZIP + optional Inno installer
- [x] `scripts/installer.iss` — Inno Setup template
- [x] README.md (public-release ready)
- [x] LICENSE (MIT)

---

## 🔄 Roadmap

### v1.6.0 — Quality of Life *(next)*
- [ ] Windrose+ toggle on/off without reinstalling — [#15](https://github.com/ManuelStaggl/WindroseServerManager/issues/15)
- [ ] Windrose+ version pinning (choose version instead of always upgrading) — [#15](https://github.com/ManuelStaggl/WindroseServerManager/issues/15)
- [ ] Switch startup bat (`StartServerForeground.bat` vs `StartWindrosePlusServer.bat`) — [#15](https://github.com/ManuelStaggl/WindroseServerManager/issues/15)
- [ ] Per-server backup & mods folders — [#5](https://github.com/ManuelStaggl/WindroseServerManager/issues/5)
- [ ] System tray: start minimized, close to tray, hide server console window — [#13](https://github.com/ManuelStaggl/WindroseServerManager/issues/13)
- [ ] Translation fixes: remaining German strings in English UI — [#3](https://github.com/ManuelStaggl/WindroseServerManager/issues/3)
- [ ] Mod conflict scanner + QoL settings page (Windrose+ multiplier sliders) — [PR #14](https://github.com/ManuelStaggl/WindroseServerManager/pull/14)
- [ ] Configurable auto-start delay (seconds) to allow secondary drives to mount before pre-launch backup + server start — [#9](https://github.com/ManuelStaggl/WindroseServerManager/issues/9)

### v1.7.0 — Automation & CLI
- [ ] CLI/CMDlets for clean shutdown, backup trigger, scheduled restart — [#9](https://github.com/ManuelStaggl/WindroseServerManager/issues/9)
- [ ] Native daily restart with auto-backup (no more `taskkill` workaround) — [#9](https://github.com/ManuelStaggl/WindroseServerManager/issues/9)

### v2.x — Future
- [ ] Multi-server concurrent launch — [#4](https://github.com/ManuelStaggl/WindroseServerManager/issues/4)
- [ ] WebUI for remote management — [#1](https://github.com/ManuelStaggl/WindroseServerManager/issues/1)
- [ ] **Remote / LAN server support** — manage a Windrose server not hosted on the local Windows machine (e.g. Docker on Unraid, NAS via SMB share on LAN). Phase 1: UNC/network path support for logs, backups & config files. Phase 2: configurable Windrose+ API host instead of hardcoded `localhost` (player list, RCON, health check, live map) — [#1](https://github.com/ManuelStaggl/WindroseServerManager/issues/1)
- [ ] Code-signing certificate for installer
- [ ] Per-server port configuration in UI
