## What's New in v1.7.0

### 🖥️ CLI via Named Pipe (IPC)
- New IPC pipe server (`\\.\pipe\WindroseServerManager`) allows controlling WSM from any script or terminal
- Supported commands: `start`, `stop`, `restart`, `backup`, `status`, `shutdown`
- Bidirectional JSON protocol — clients receive `{"Ok":true,"Error":null,"Data":null}` responses
- No more deadlocks: server uses `ReadLineAsync` + newline-terminated responses

### 🤖 Discord — Session History Events
- All server lifecycle events (Started, Stopped, Crashed, Player joined/left) now forwarded to Discord as rich embeds
- Anti Rate-Limit: events are batched in a `ConcurrentQueue` and flushed every 3 seconds
- Full unsubscribe (`-= OnSessionEventAppended`) on bot stop — no memory leaks

### 🔄 Reliability — Watchdog & Restart Scheduler
- **Watchdog**: now correctly handles `Crashed` status and triggers auto-restart
- **KillAsync()**: sets `_isGracefulShutdown = true` to prevent unwanted auto-restart after Force Kill
- **RestartScheduler**: `MarkTriggered()` called only after successful restart — retries on failure
- All hardcoded German strings replaced with English in `Core` layer

### 🌐 Windrose+ Remote Host
- `WindrosePlusApiService` now supports remote host/port configuration (not just localhost)
- Circuit Breaker: 3 consecutive failures → 60s cooldown, logs downgraded to `Debug`

### 🔧 IPC Race Condition Fix
- `restart` IPC command now polls server status (timeout 15s) instead of blind `Task.Delay(2000)`

### ✅ Tests
- **268 passing tests** (up from 235 in v1.6.7)

***
**Full changelog**: https://github.com/Numa26210/WindroseServerManager/blob/main/PROGRESS.md
