using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindroseServerManager.Core.Models;

namespace WindroseServerManager.Core.Services;

public sealed class ConflictScannerService : IConflictScannerService
{
    private readonly ILogger<ConflictScannerService> _logger;
    private readonly IModService _mods;
    private readonly IWindrosePlusApiService _api;
    private readonly IAppSettingsService _settings;

    public ConflictScannerService(
        ILogger<ConflictScannerService> logger,
        IModService mods,
        IWindrosePlusApiService api,
        IAppSettingsService settings)
    {
        _logger = logger;
        _mods = mods;
        _api = api;
        _settings = settings;
    }

    public IReadOnlyList<ConflictResult> ScanForConflicts()
    {
        var serverDir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(serverDir))
            return Array.Empty<ConflictResult>();

        var isActive = _settings.Current.WindrosePlusActiveByServer
            .GetValueOrDefault(serverDir, false);
        if (!isActive)
            return Array.Empty<ConflictResult>();

        var results = new List<ConflictResult>();

        try
        {
            var enabledMods = _mods.ListMods().Where(m => m.IsEnabled).ToList();
            if (enabledMods.Count == 0)
                return Array.Empty<ConflictResult>();

            var config = _api.ReadConfig(serverDir);
            var multipliers = config?.Multipliers ?? new Dictionary<string, object?>();

            foreach (var mod in enabledMods)
            {
                if (mod.NexusMeta is null)
                    continue;

                var entry = ModCompatibilityDatabase.Lookup(mod.NexusMeta.NexusModId);
                if (entry is null)
                    continue;

                foreach (var key in entry.AffectedMultipliers)
                {
                    if (!TryGetMultiplierValue(multipliers, key, out var value))
                        continue;

                    if (Math.Abs(value - 1.0) < 0.001)
                        continue; // default value, no conflict

                    var severity = ModCompatibilityDatabase.IsDangerousMultiplier(key)
                        ? ConflictSeverity.Error
                        : ConflictSeverity.Warning;

                    results.Add(new ConflictResult(
                        severity,
                        mod.FileName,
                        mod.DisplayName,
                        mod.NexusMeta.NexusModId,
                        key,
                        $"'{mod.DisplayName}' modifies {key} via pak while Windrose+ {key} multiplier is set to {value:F1}. " +
                        (severity == ConflictSeverity.Error
                            ? "This combination is known to cause save/character corruption."
                            : "This may cause unexpected behavior."),
                        $"Either disable the '{mod.DisplayName}' pak mod or reset the '{key}' multiplier to 1.0 (default)."));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Conflict scan failed");
        }

        if (results.Count > 0)
            _logger.LogInformation("Conflict scan found {Count} issue(s)", results.Count);

        return results;
    }

    private static bool TryGetMultiplierValue(Dictionary<string, object?> multipliers, string key, out double value)
    {
        value = 1.0;
        if (!multipliers.TryGetValue(key, out var raw) || raw is null)
            return false;

        if (raw is double d) { value = d; return true; }
        if (raw is float f) { value = f; return true; }
        if (raw is int i) { value = i; return true; }
        if (raw is long l) { value = l; return true; }
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Number)
        {
            value = je.GetDouble();
            return true;
        }
        if (raw is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
