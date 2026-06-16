## What's new in v1.6.7

### 🐛 Critical Fix — RocksDB_v2 path migration (issue #7)
- After the 0.10.0.5.120 server update, worlds were stored in `RocksDB_v2` but the app was still reading/writing to the legacy `RocksDB` folder. World list, "Open Worlds folder", and all config changes now auto-detect and use the correct path.

### 🔧 Windrose+ Self-Healing & Force Reinstall (issue #6)
- Missing W+ files on startup → install state auto-reset + warning toast.
- New **"Force Reinstall Windrose+"** button in Settings: reinstalls W+ from scratch while preserving your config files.

### 🌐 Localization Fix — Dashboard "frei" (issue #5)
- "frei" in Dashboard > Host is now properly localized: English Windows → "free", German Windows → "frei".

### Upgrade
Simply replace the existing exe. No settings migration needed.
