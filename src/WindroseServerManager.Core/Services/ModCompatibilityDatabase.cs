using WindroseServerManager.Core.Models;

namespace WindroseServerManager.Core.Services;

/// <summary>
/// Static catalog of known Nexus mods and the Windrose+ multiplier keys they overlap with.
/// Used by <see cref="ConflictScannerService"/> to detect dangerous mod+multiplier combinations.
/// Follows the same static-class pattern as <see cref="WorldParameterCatalog"/>.
/// </summary>
public static class ModCompatibilityDatabase
{
    /// <summary>Multiplier keys known to cause crashes when combined with pak mods.</summary>
    public static readonly IReadOnlySet<string> DangerousMultipliers = new HashSet<string>
    {
        "stack_size",
        "weight",
        "inventory_size",
    };

    public static IReadOnlyList<ModCompatibilityEntry> All { get; } = new List<ModCompatibilityEntry>
    {
        new(28, "MoreStacks",
            new[] { "stack_size" },
            Array.Empty<string>(),
            ModSafetyLevel.Both,
            "Modifies stack sizes via pak — conflicts with the Windrose+ stack_size multiplier."),

        new(26, "Max Stack Sizes",
            new[] { "stack_size" },
            Array.Empty<string>(),
            ModSafetyLevel.Both,
            "Sets stack limits to 999/999999 via pak — conflicts with the Windrose+ stack_size multiplier."),

        new(336, "Stack Size",
            new[] { "stack_size" },
            Array.Empty<string>(),
            ModSafetyLevel.Both,
            "Modifies stack sizes via pak — conflicts with the Windrose+ stack_size multiplier."),

        new(50, "2x-100x All Loot",
            new[] { "loot" },
            Array.Empty<string>(),
            ModSafetyLevel.Both,
            "Multiplies loot drop rates via pak — conflicts with the Windrose+ loot multiplier."),
    };

    private static readonly Dictionary<int, ModCompatibilityEntry> _byId =
        All.ToDictionary(e => e.NexusModId);

    private static readonly Dictionary<string, List<ModCompatibilityEntry>> _byMultiplier;

    static ModCompatibilityDatabase()
    {
        _byMultiplier = new Dictionary<string, List<ModCompatibilityEntry>>();
        foreach (var entry in All)
        {
            foreach (var key in entry.AffectedMultipliers)
            {
                if (!_byMultiplier.TryGetValue(key, out var list))
                    _byMultiplier[key] = list = new List<ModCompatibilityEntry>();
                list.Add(entry);
            }
        }
    }

    /// <summary>Look up a known mod by its Nexus mod ID. Returns null if unknown.</summary>
    public static ModCompatibilityEntry? Lookup(int nexusModId)
        => _byId.GetValueOrDefault(nexusModId);

    /// <summary>Find all known mods that affect a given Windrose+ multiplier key.</summary>
    public static IReadOnlyList<ModCompatibilityEntry> LookupByMultiplier(string multiplierKey)
        => _byMultiplier.TryGetValue(multiplierKey, out var list)
            ? list
            : Array.Empty<ModCompatibilityEntry>();

    /// <summary>Returns true if the given multiplier key is known to cause crashes when combined with pak mods.</summary>
    public static bool IsDangerousMultiplier(string multiplierKey)
        => DangerousMultipliers.Contains(multiplierKey);
}
