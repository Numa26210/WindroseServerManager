# Issue Triage - 2026-05-31

Maintainer focus: treat `Numa26210/WindroseServerManager` as the active community fork and redirect users from `ManuelStaggl/WindroseServerManager` when their issue is fixed or better handled here.

## Repository State

- Active fork: `https://github.com/Numa26210/WindroseServerManager`
- Upstream/base: `https://github.com/ManuelStaggl/WindroseServerManager`
- Local remotes:
  - `origin` -> Numa26210 fork
  - `upstream` -> ManuelStaggl base, fetch-only; push URL disabled locally
- Branch delta at triage time: `origin/main` is 54 commits ahead of `upstream/main`, 0 behind.
- Local limitation: no `gh` CLI and no `GITHUB_TOKEN` / `GH_TOKEN`, so public issue actions need browser login, GitHub CLI install/auth, or a token.

## Numa26210 Fork - Open Issues

### #9 - Windrose+ File access error: Virus or Unwanted Software

URL: https://github.com/Numa26210/WindroseServerManager/issues/9

Initial classification: likely dependency/download/AV false-positive path around Windrose+ or UE4SS payload, but needs exact blocked file path and release asset name.

Suggested maintainer reply:

```md
Thanks for the report. This looks like Windows Defender or another AV blocking one of the Windrose+ / UE4SS files during install, but I need the exact file name/path to confirm.

Could you please add:

- The full text from the Windows Security detection history
- The Windrose Server Manager version
- Whether this happens during Windrose+ download, extraction, or first launch
- If possible, the log file from `%LocalAppData%\WindroseServerManager\logs`

Do not whitelist anything blindly yet. I want to confirm whether this is a known UE4SS false positive or a changed upstream asset before recommending a workaround.
```

### #8 - App crash

URL: https://github.com/Numa26210/WindroseServerManager/issues/8

Initial classification: Avalonia/SkiaSharp native load failure at startup on Windows 10. Likely missing native dependency, GPU/render backend issue, or packaging/runtime problem.

Maintainer action: do not post another reply yet. A prior owner comment already gave a concrete .NET single-file / Skia extraction diagnosis and workaround steps. Wait for reporter feedback before adding more noise.

### #1 - Existing server install on another machine

URL: https://github.com/Numa26210/WindroseServerManager/issues/1

Initial classification: partly addressed by v1.7.0 remote Windrose+ host support, but still needs UX validation for adopting an existing remote/docker install with explicit IP/port/password.

Suggested maintainer reply:

```md
This is exactly the direction the active fork is moving in. v1.7.0 added remote Windrose+ host/port support, so the manager is no longer limited to localhost for Windrose+ integration.

What I still need to verify is the full "adopt an existing remote/docker server" flow: install path, IP, dashboard/RCON port, and password without forcing a Windrose+ reinstall.

Could you confirm:

- Is the Windows machine accessing the install through a mapped drive or UNC path?
- Is Windrose+ already reachable from Windows in a browser?
- Which host/port/password combo works outside WSM?

I will keep this open as the tracking issue for remote/docker adoption.
```

## Upstream/Base Issues - Redirect Plan

Use a concise redirect comment on upstream issues that are already fixed or actively maintained in the Numa26210 fork.

Base redirect template:

```md
The actively maintained community fork is here:

https://github.com/Numa26210/WindroseServerManager

This fork has continued development beyond the base repo, including recent releases and fixes. If you still hit this problem on the latest release from the fork, please open or continue the issue there with logs/screenshots so it can be tracked in the active repo.
```

### #18 - Window should be sizeable

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/18

Status in fork: fixed in v1.6.6. README/PROGRESS mention restored resize on all 8 edges/corners.

Suggested redirect:

```md
This is fixed in the active community fork:

https://github.com/Numa26210/WindroseServerManager

The v1.6.6 release restored resizing on all 8 edges/corners for the custom window chrome. Please try the latest release from the fork, and if resizing still fails in your RDP setup, open a new issue there with your Windows/RDP details.
```

### #16 - Did you abandon this project?

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/16

Status in fork: answer is to redirect to active fork.

Suggested redirect:

```md
Development is active in the community fork maintained by Numa26210:

https://github.com/Numa26210/WindroseServerManager

That fork has recent releases, fixes, and ongoing issue tracking. Users looking for current builds or support should use the fork's releases/issues.
```

### #15 - Please add windrose+ removal/disable

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/15

Status in fork: implemented. README mentions Windrose+ toggle on/off and version pinning.

Suggested redirect:

```md
This is implemented in the active community fork:

https://github.com/Numa26210/WindroseServerManager

The fork includes per-server Windrose+ enable/disable and version pinning, so you can temporarily disable Windrose+ or lock to a known-good release. Please use the latest release there and report any remaining edge cases in that repo.
```

### #13 - System tray/minimized startup and hidden server process option

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/13

Status in fork: implemented. README/PROGRESS mention close-to-tray, `--tray` / `--minimized`, and hidden server console window.

Suggested redirect:

```md
These startup/background features are available in the active community fork:

https://github.com/Numa26210/WindroseServerManager

The fork supports close-to-tray, `--tray` / `--minimized`, and hiding/minimizing the server console window. Please try the latest release from the fork and open a fork-side issue if one of those modes misbehaves.
```

### #10 - Cannot update server via SteamCMD anymore

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/10

Status in fork: likely addressed/improved. README/PROGRESS mention server install/update via SteamCMD, update checks, and SteamCMD reliability fixes.

Suggested redirect:

```md
The active community fork has continued work on SteamCMD updates and update reliability:

https://github.com/Numa26210/WindroseServerManager

Please try the latest release from that fork. If the update action still fails, open an issue there with the SteamCMD log output and the server install path so it can be debugged against the current code.
```

### #9 - CMDlets

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/9

Status in fork: implemented in v1.7.0 via named pipe IPC. Commands: `start`, `stop`, `restart`, `backup`, `status`, `shutdown`.

Suggested redirect:

```md
This is implemented in the active community fork as of v1.7.0:

https://github.com/Numa26210/WindroseServerManager

WSM now exposes a named-pipe IPC interface at `\\.\pipe\WindroseServerManager` with commands such as `start`, `stop`, `restart`, `backup`, `status`, and `shutdown`, so scheduled clean restart/backup workflows no longer need `taskkill`.
```

### #7 - Windrose+ Integration root cause of performance issues

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/7

Status in fork: mitigated by Windrose+ toggle/version pinning. Root performance bug may be upstream Windrose+.

Suggested redirect:

```md
The active community fork adds tools to work around this while Windrose+ issues are investigated upstream:

https://github.com/Numa26210/WindroseServerManager

The fork supports per-server Windrose+ enable/disable and version pinning. If a specific Windrose+ version causes lag, please report it in the fork with the Windrose+ version, Windrose server version, and whether disabling Windrose+ immediately resolves the issue.
```

### #6 - Automatic backup

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/6

Status in fork: implemented/fixed. README/PROGRESS mention backup before restart and file-lock handling.

Suggested redirect:

```md
This is handled in the active community fork:

https://github.com/Numa26210/WindroseServerManager

The fork supports automatic backup before scheduled/threshold restarts, includes a configurable post-stop grace delay, and fixes backup failures caused by locked log files during restart.
```

### #5 - Mods/Backups Not updating per server

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/5

Status in fork: implemented. README/PROGRESS mention per-server backup and mods folders.

Suggested redirect:

```md
This is implemented in the active community fork:

https://github.com/Numa26210/WindroseServerManager

The fork supports per-server backup and mods folder overrides in Settings. Please use the latest release there and open a fork-side issue if a specific server still uses the wrong folders.
```

### #4 - Multiple Servers Concurrent

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/4

Status in fork: not confirmed as fully implemented. Needs product decision and architecture review.

Suggested redirect:

```md
The actively maintained fork is here:

https://github.com/Numa26210/WindroseServerManager

This request needs tracking against the current fork because it touches process management, port allocation, per-server config isolation, and UI state. Please reopen/continue it there with your desired number of concurrent servers and port layout.
```

### #3 - Incomplete English translation

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/3

Status in fork: partly fixed for app-owned strings, but native context menus may still depend on OS/Avalonia behavior.

Suggested redirect:

```md
The active fork has continued localization work:

https://github.com/Numa26210/WindroseServerManager

Several hardcoded German strings have been fixed there. If you still see German text in the latest fork release, please open a fork-side issue with screenshots and your Windows display language/app language. Native text box context menus may depend on OS/Avalonia behavior, so screenshots are especially useful.
```

### #1 - FEATURE REQUEST - WebUI

URL: https://github.com/ManuelStaggl/WindroseServerManager/issues/1

Status in fork: future roadmap item, not implemented.

Suggested redirect:

```md
The actively maintained fork tracks future work here:

https://github.com/Numa26210/WindroseServerManager

WebUI/remote management is still a larger feature request, not a quick fix. Please open or continue the request in the fork with your preferred deployment model: local-only web UI, LAN-only, authenticated remote access, or headless server mode.
```

## Immediate Maintainer Actions

- [ ] Post redirect comments on upstream issues #18, #16, #15, #13, #9, #6, #5 first because the fork clearly addresses them.
- [ ] Post softer redirect comments on upstream #10, #7, #3 because they are likely addressed or mitigated but need reporter confirmation.
- [ ] Ask upstream #4 and #1 reporters to reopen in the fork as feature requests.
- [ ] Reply on fork issues #9 and #1 with diagnostic questions above; leave #8 alone unless the reporter responds.
- [ ] Add GitHub issue templates to the fork for bug reports, feature requests, and support questions.
- [ ] Install/authenticate GitHub tooling or provide an API token before attempting issue writes.
