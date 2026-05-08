using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WindroseServerManager.App.Services;
using WindroseServerManager.Core.Models;
using WindroseServerManager.Core.Services;

namespace WindroseServerManager.App.ViewModels;

public partial class QoLSettingsViewModel : ViewModelBase
{
    private readonly IWindrosePlusApiService _api;
    private readonly IAppSettingsService _settings;
    private readonly IModService _mods;
    private readonly IConflictScannerService _conflictScanner;
    private readonly IServerProcessService _proc;
    private readonly IToastService _toasts;

    private bool _suppressConflictScan;

    // Economy
    [ObservableProperty] private double _xpMultiplier = 1.0;
    [ObservableProperty] private double _lootMultiplier = 1.0;
    [ObservableProperty] private double _craftCostMultiplier = 1.0;

    // Farming
    [ObservableProperty] private double _cropSpeedMultiplier = 1.0;
    [ObservableProperty] private double _cookingSpeedMultiplier = 1.0;
    [ObservableProperty] private double _harvestYieldMultiplier = 1.0;

    // Inventory
    [ObservableProperty] private double _stackSizeMultiplier = 1.0;
    [ObservableProperty] private double _inventorySizeMultiplier = 1.0;
    [ObservableProperty] private double _weightMultiplier = 1.0;

    // Character
    [ObservableProperty] private double _pointsPerLevelMultiplier = 1.0;

    // State
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<ConflictResult> _activeConflicts = new();

    public bool IsWindrosePlusActive =>
        _settings.Current.WindrosePlusActiveByServer.GetValueOrDefault(
            _settings.ActiveServerDir ?? string.Empty, false);

    public bool HasConflicts => ActiveConflicts.Count > 0;
    public bool HasErrors => ActiveConflicts.Any(c => c.Severity == ConflictSeverity.Error);
    public bool HasWarnings => ActiveConflicts.Any(c => c.Severity == ConflictSeverity.Warning);

    public QoLSettingsViewModel(
        IWindrosePlusApiService api,
        IAppSettingsService settings,
        IModService mods,
        IConflictScannerService conflictScanner,
        IServerProcessService proc,
        IToastService toasts)
    {
        _api = api;
        _settings = settings;
        _mods = mods;
        _conflictScanner = conflictScanner;
        _proc = proc;
        _toasts = toasts;
    }

    public void Start()
    {
        OnPropertyChanged(nameof(IsWindrosePlusActive));
        if (IsWindrosePlusActive) _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var serverDir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(serverDir)) return;

        IsLoading = true;
        try
        {
            var cfg = await Task.Run(() => _api.ReadConfig(serverDir));
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _suppressConflictScan = true;
                var m = cfg?.Multipliers ?? new Dictionary<string, object?>();
                XpMultiplier = GetDouble(m, "xp", 1.0);
                LootMultiplier = GetDouble(m, "loot", 1.0);
                CraftCostMultiplier = GetDouble(m, "craft_cost", 1.0);
                CropSpeedMultiplier = GetDouble(m, "crop_speed", 1.0);
                CookingSpeedMultiplier = GetDouble(m, "cooking_speed", 1.0);
                HarvestYieldMultiplier = GetDouble(m, "harvest_yield", 1.0);
                StackSizeMultiplier = GetDouble(m, "stack_size", 1.0);
                InventorySizeMultiplier = GetDouble(m, "inventory_size", 1.0);
                WeightMultiplier = GetDouble(m, "weight", 1.0);
                PointsPerLevelMultiplier = GetDouble(m, "points_per_level", 1.0);
                _suppressConflictScan = false;
                RefreshConflicts();
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "QoL settings load failed");
            _toasts.Error(Loc.Get("QoL.Error.Load"));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var serverDir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(serverDir)) return;

        var cfg = _api.ReadConfig(serverDir) ?? new WindrosePlusConfig();
        cfg.Multipliers["xp"] = XpMultiplier;
        cfg.Multipliers["loot"] = LootMultiplier;
        cfg.Multipliers["craft_cost"] = CraftCostMultiplier;
        cfg.Multipliers["crop_speed"] = CropSpeedMultiplier;
        cfg.Multipliers["cooking_speed"] = CookingSpeedMultiplier;
        cfg.Multipliers["harvest_yield"] = HarvestYieldMultiplier;
        cfg.Multipliers["stack_size"] = StackSizeMultiplier;
        cfg.Multipliers["inventory_size"] = InventorySizeMultiplier;
        cfg.Multipliers["weight"] = WeightMultiplier;
        cfg.Multipliers["points_per_level"] = PointsPerLevelMultiplier;

        try
        {
            await _api.WriteConfigAsync(serverDir, cfg, CancellationToken.None);
            _toasts.Success(Loc.Get("QoL.Saved"));

            if (_proc.Status == ServerStatus.Running)
                _toasts.Warning(Loc.Get("QoL.RestartRequired"));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "QoL settings save failed");
            _toasts.Error(Loc.Get("QoL.Error.Save"));
        }
    }

    [RelayCommand]
    private void ResetAll()
    {
        _suppressConflictScan = true;
        XpMultiplier = 1.0;
        LootMultiplier = 1.0;
        CraftCostMultiplier = 1.0;
        CropSpeedMultiplier = 1.0;
        CookingSpeedMultiplier = 1.0;
        HarvestYieldMultiplier = 1.0;
        StackSizeMultiplier = 1.0;
        InventorySizeMultiplier = 1.0;
        WeightMultiplier = 1.0;
        PointsPerLevelMultiplier = 1.0;
        _suppressConflictScan = false;
        RefreshConflicts();
    }

    // Trigger conflict rescan when any multiplier changes
    partial void OnXpMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnLootMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnCraftCostMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnCropSpeedMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnCookingSpeedMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnHarvestYieldMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnStackSizeMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnInventorySizeMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnWeightMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();
    partial void OnPointsPerLevelMultiplierChanged(double value) => RefreshConflictsIfNotSuppressed();

    private void RefreshConflictsIfNotSuppressed()
    {
        if (!_suppressConflictScan) RefreshConflicts();
    }

    private void RefreshConflicts()
    {
        try
        {
            var conflicts = _conflictScanner.ScanForConflicts();
            ActiveConflicts = new ObservableCollection<ConflictResult>(conflicts);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Conflict scan in QoL view failed");
            ActiveConflicts = new ObservableCollection<ConflictResult>();
        }
        OnPropertyChanged(nameof(HasConflicts));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(HasWarnings));
    }

    /// <summary>Get the conflict warning for a specific multiplier key, or null if no conflict.</summary>
    public string? GetConflictForKey(string key)
    {
        var conflict = ActiveConflicts.FirstOrDefault(c => c.ConflictingParameter == key);
        return conflict?.Description;
    }

    public bool HasConflictForKey(string key)
        => ActiveConflicts.Any(c => c.ConflictingParameter == key);

    private static double GetDouble(Dictionary<string, object?> dict, string key, double fallback)
    {
        if (!dict.TryGetValue(key, out var raw) || raw is null) return fallback;
        if (raw is double d) return d;
        if (raw is float f) return f;
        if (raw is int i) return i;
        if (raw is long l) return l;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Number) return je.GetDouble();
        if (raw is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return fallback;
    }
}
