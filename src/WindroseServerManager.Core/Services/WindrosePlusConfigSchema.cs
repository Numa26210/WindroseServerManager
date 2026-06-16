using System.Globalization;

namespace WindroseServerManager.Core.Services;

/// <summary>
/// JsonSection: the JSON root key to read/write ("server", "rcon", "multipliers").
/// Null = use lowercase Category as fallback.
/// </summary>
public sealed record ConfigEntrySchema(
    string Category, string Key, string Type,
    double? Min, double? Max, object? Default, string DescriptionKey,
    string? JsonSection = null, bool IsEnabled = true);

public static class WindrosePlusConfigSchema
{
    public static IReadOnlyList<ConfigEntrySchema> All { get; } = new List<ConfigEntrySchema>
    {
        // Server settings (RCON + dashboard)
        new("Server", "http_port", "int",    1024, 65535, 8780,  "Editor.Schema.HttpPort",    "server"),
        new("Server", "enabled",   "bool",   null, null,  false, "Editor.Schema.RconEnabled",  "rcon"),
        new("Server", "password",  "string", null, null,  "",    "Editor.Schema.RconPassword", "rcon"),
        // Economy
        new("Economy", "xp",              "float", 0.1, 100, 1.0, "QoL.Xp",              "multipliers"),
        new("Economy", "loot",            "float", 0.1, 100, 1.0, "QoL.Loot",            "multipliers"),
        new("Economy", "craft_cost",      "float", 0.1, 100, 1.0, "QoL.CraftCost",       "multipliers"),
        // Farming
        new("Farming", "crop_speed",      "float", 0.1, 100, 1.0, "QoL.CropSpeed",       "multipliers"),
        new("Farming", "cooking_speed",   "float", 0.1, 100, 1.0, "QoL.CookingSpeed",    "multipliers"),
        new("Farming", "harvest_yield",   "float", 0.1, 100, 1.0, "QoL.HarvestYield",    "multipliers"),
        // Inventory (disabled — controlled by Windrose+ pak)
        new("Inventory", "stack_size",     "float", 0.1, 100, 1.0, "QoL.StackSize",       "multipliers", IsEnabled: false),
        new("Inventory", "inventory_size", "float", 0.1, 100, 1.0, "QoL.InventorySize",   "multipliers", IsEnabled: false),
        new("Inventory", "weight",         "float", 0.1, 100, 1.0, "QoL.Weight",          "multipliers", IsEnabled: false),
        // Character
        new("Character", "points_per_level","float", 0.1, 100, 1.0, "QoL.PointsPerLevel", "multipliers"),
    };

    public static string? Validate(string key, string rawValue)
    {
        var schema = All.FirstOrDefault(s => s.Key == key);
        if (schema is null) return $"Unknown key: {key}";
        switch (schema.Type)
        {
            case "float":
                if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    return "Not a number";
                if (schema.Min.HasValue && f < schema.Min.Value) return $"Min {schema.Min}";
                if (schema.Max.HasValue && f > schema.Max.Value) return $"Max {schema.Max}";
                return null;
            case "int":
                if (!int.TryParse(rawValue, out var i)) return "Not an integer";
                if (schema.Min.HasValue && i < schema.Min.Value) return $"Min {schema.Min}";
                if (schema.Max.HasValue && i > schema.Max.Value) return $"Max {schema.Max}";
                return null;
            case "bool":
                if (!bool.TryParse(rawValue, out _)) return "Not a boolean";
                return null;
            default:
                return null;
        }
    }
}
