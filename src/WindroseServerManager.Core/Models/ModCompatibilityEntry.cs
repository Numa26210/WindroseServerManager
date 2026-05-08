namespace WindroseServerManager.Core.Models;

public enum ModSafetyLevel
{
    /// <summary>Mod only affects server-side behavior — safe for character portability.</summary>
    ServerSideOnly,

    /// <summary>Mod requires client-side installation — may affect characters.</summary>
    ClientSide,

    /// <summary>Mod affects both server and client — highest conflict risk.</summary>
    Both,
}

/// <summary>
/// Describes a known Nexus mod and which Windrose+ multiplier keys it overlaps with.
/// Used by the conflict scanner to detect dangerous combinations.
/// </summary>
public sealed record ModCompatibilityEntry(
    int NexusModId,
    string ModName,
    IReadOnlyList<string> AffectedMultipliers,
    IReadOnlyList<string> AffectedWorldParams,
    ModSafetyLevel SafetyLevel,
    string Description);
