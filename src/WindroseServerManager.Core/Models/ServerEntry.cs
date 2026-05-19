namespace WindroseServerManager.Core.Models;

public sealed class ServerEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InstallDir { get; set; } = string.Empty;

    /// <summary>
    /// When true, this specific server is auto-started when the app launches.
    /// Effective per-server start condition: <c>AppSettings.AutoStartServerOnAppLaunch || entry.AutoStartOnAppLaunch</c>.
    /// The app-level flag is a shortcut meaning "start all configured servers"; this per-server
    /// flag lets admins pick individual servers instead.
    /// </summary>
    public bool AutoStartOnAppLaunch { get; set; } = false;

    /// <summary>
    /// When false, WindrosePlus is bypassed for this server (vanilla launch) even if installed.
    /// Default true for backward compatibility — existing servers with W+ installed keep it active.
    /// </summary>
    public bool IsWindrosePlusEnabled { get; set; } = true;

    /// <summary>
    /// When set, WindrosePlus installs/uses this specific version tag instead of latest.
    /// Null or empty = use the latest release.
    /// </summary>
    public string? PinnedWindrosePlusVersion { get; set; }
}
