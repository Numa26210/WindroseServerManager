namespace WindroseServerManager.Core.Models;

public enum BackupOrigin { Manual, Auto, PreRestart, PreLaunch, PreConfig }

public sealed record BackupInfo(
    string FileName,
    string FullPath,
    DateTime CreatedUtc,
    long SizeBytes,
    bool IsAutomatic,
    BackupOrigin Origin = BackupOrigin.Manual);
