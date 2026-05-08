namespace WindroseServerManager.Core.Models;

public enum ConflictSeverity
{
    /// <summary>Informational — no immediate risk but worth noting.</summary>
    Info,

    /// <summary>Potential issue — may cause unexpected behavior.</summary>
    Warning,

    /// <summary>High risk — known to cause save/character corruption.</summary>
    Error,
}

/// <summary>
/// Represents a detected conflict between an installed pak mod and an active multiplier setting.
/// </summary>
public sealed record ConflictResult(
    ConflictSeverity Severity,
    string ModFileName,
    string ModDisplayName,
    int? NexusModId,
    string ConflictingParameter,
    string Description,
    string SuggestedFix);
