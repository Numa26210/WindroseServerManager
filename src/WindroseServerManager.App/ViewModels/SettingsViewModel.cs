using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WindroseServerManager.App.Services;
using WindroseServerManager.App.Views.Dialogs;
using WindroseServerManager.Core.Models;
using WindroseServerManager.Core.Services;

namespace WindroseServerManager.App.ViewModels;

public sealed class LanguageOption
{
    public required string Key { get; init; }        // "auto" | "de" | "en"
    public required string DisplayName { get; init; }
}

public sealed class UpdateIntervalOption
{
    public required int Hours { get; init; }         // 0 = off
    public required string DisplayName { get; init; }
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _settings;
    private readonly IToastService _toasts;
    private readonly IFirewallService _firewall;
    private readonly IAutoStartService _autoStart;
    private readonly IAppUpdateService _appUpdate;
    private readonly ILocalizationService _localization;
    private readonly IWindrosePlusService _wplus;
    private readonly IWindrosePlusApiService _wplusApi;
    private readonly IWindrosePlusUpdateService _wplusUpdate;

    [ObservableProperty] private bool _autoRestartOnCrash;
    [ObservableProperty] private int _gracefulShutdownSeconds;

    // Launch-Args (strukturiert)
    [ObservableProperty] private bool _logEnabled = true;
    [ObservableProperty] private string _extraLaunchArgs = string.Empty;

    // Steam
    [ObservableProperty] private string _steamAppId = "4129620";
    [ObservableProperty] private string _steamLogin = string.Empty;

    // Wird true während der Konstruktor die Properties aus Settings füllt —
    // verhindert dass jedes Initial-Assign eine Persist-Runde auslöst.
    private bool _suppressPersist;

    // Firewall
    [ObservableProperty] private bool _isFirewallRuleInstalled;
    [ObservableProperty] private bool _isFirewallBusy;

    // Autostart
    [ObservableProperty] private bool _autoStartEnabled;
    [ObservableProperty] private bool _autoStartServerOnAppLaunch;
    [ObservableProperty] private int _autoStartDelaySeconds;
    [ObservableProperty] private bool _closeToTray;

    // Discord Bot Integration
    [ObservableProperty] private bool _enableDiscordBot;
    [ObservableProperty] private string _discordBotToken = string.Empty;
    [ObservableProperty] private string _discordGuildId = "0";
    [ObservableProperty] private string _discordLogChannelId = "0";

    // App-Update-Check
    [ObservableProperty] private bool _isUpdateCheckBusy;
    [ObservableProperty] private string? _updateCheckStatus;
    [ObservableProperty] private bool _hasUpdateAvailable;
    [ObservableProperty] private string? _pendingReleaseUrl;
    [ObservableProperty] private string? _pendingDownloadUrl;

    // WindrosePlus-Update-Check
    [ObservableProperty] private bool _isWindrosePlusUpdateCheckBusy;
    [ObservableProperty] private string? _windrosePlusUpdateStatus;
    [ObservableProperty] private bool _hasWindrosePlusUpdateAvailable;
    [ObservableProperty] private string? _windrosePlusLatestTag;
    [ObservableProperty] private string? _windrosePlusReleaseUrl;
    [ObservableProperty] private int _windrosePlusPendingCount;

    // WindrosePlus Toggle & Version Pinning (per active server)
    [ObservableProperty] private bool _isWindrosePlusEnabled = true;
    [ObservableProperty] private string _pinnedWindrosePlusVersion = string.Empty;
    private bool _suppressWpSettings;

    [ObservableProperty] private bool _isVersionListLoading;
    [ObservableProperty] private string? _installedWindrosePlusVersion;
    [ObservableProperty] private bool _isForceReinstallBusy;
    public ObservableCollection<string> AvailableWindrosePlusVersions { get; } = new();

    [ObservableProperty] private string _backupDirOverride = string.Empty;
    [ObservableProperty] private string _modsDirOverride = string.Empty;
    private bool _suppressDirOverrides;

    public ObservableCollection<UpdateIntervalOption> WindrosePlusIntervalOptions { get; } = new();
    [ObservableProperty] private UpdateIntervalOption? _selectedWindrosePlusInterval;
    private bool _suppressIntervalWrite;

    // Language
    public ObservableCollection<LanguageOption> LanguageOptions { get; } = new();
    [ObservableProperty] private LanguageOption? _selectedLanguageOption;
    private bool _suppressLanguageWrite;

    private bool _suppressAutoStartWrite;

    public SettingsViewModel(
        IAppSettingsService settings,
        IToastService toasts,
        IFirewallService firewall,
        IAutoStartService autoStart,
        IAppUpdateService appUpdate,
        ILocalizationService localization,
        IWindrosePlusService wplus,
        IWindrosePlusApiService wplusApi,
        IWindrosePlusUpdateService wplusUpdate)
    {
        _settings = settings;
        _toasts = toasts;
        _firewall = firewall;
        _autoStart = autoStart;
        _appUpdate = appUpdate;
        _localization = localization;
        _wplus = wplus;
        _wplusApi = wplusApi;
        _wplusUpdate = wplusUpdate;

        _suppressPersist = true;
        var c = settings.Current;
        _autoRestartOnCrash = c.AutoRestartOnCrash;
        _gracefulShutdownSeconds = c.GracefulShutdownSeconds;

        _logEnabled = c.LogEnabled;
        _extraLaunchArgs = c.ExtraLaunchArgs;

        _steamAppId = c.SteamAppId;
        _steamLogin = c.SteamLogin;

        // Discord Bot Integration
        _enableDiscordBot = c.EnableDiscordBot;
        _discordBotToken = c.DiscordBotToken ?? string.Empty;
        _discordGuildId = c.DiscordGuildId > 0 ? c.DiscordGuildId.ToString() : string.Empty;
        _discordLogChannelId = c.DiscordLogChannelId > 0 ? c.DiscordLogChannelId.ToString() : string.Empty;

        _suppressPersist = false;

        _suppressAutoStartWrite = true;
        _autoStartEnabled = _autoStart.IsEnabled();
        _autoStartServerOnAppLaunch = c.AutoStartServerOnAppLaunch;
        _autoStartDelaySeconds = c.AutoStartDelaySeconds;
        _suppressAutoStartWrite = false;
        _closeToTray = c.CloseToTray;

        // WindrosePlus per-server toggle + version pin
        _suppressWpSettings = true;
        var activeEntry = c.Servers.FirstOrDefault(srv => srv.Id == c.ActiveServerId);
        if (activeEntry is not null)
        {
            _isWindrosePlusEnabled = activeEntry.IsWindrosePlusEnabled;
            _pinnedWindrosePlusVersion = activeEntry.PinnedWindrosePlusVersion ?? string.Empty;
        }
        _suppressWpSettings = false;

        _suppressDirOverrides = true;
        if (activeEntry is not null)
        {
            _backupDirOverride = activeEntry.BackupDirOverride ?? string.Empty;
            _modsDirOverride = activeEntry.ModsDirOverride ?? string.Empty;
        }
        _suppressDirOverrides = false;

        RebuildLanguageOptions();
        _localization.LanguageChanged += OnLanguageChanged;

        RebuildIntervalOptions();
        _wplusUpdate.UpdateChecked += OnWindrosePlusUpdateChecked;
        ApplyLastWindrosePlusResult();

        _settings.Changed += OnSettingsChanged;
        SafeFireAndForget(CheckFirewallCoreAsync(showToast: false), "CheckFirewall");
        SafeFireAndForget(LoadWindrosePlusVersionsAsync(), "LoadWplusVersions");
        LoadInstalledWindrosePlusVersion();
    }

    private async Task LoadWindrosePlusVersionsAsync()
    {
        try
        {
            IsVersionListLoading = true;
            var tags = await _wplus.FetchAllReleaseTagsAsync().ConfigureAwait(false);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AvailableWindrosePlusVersions.Clear();
                AvailableWindrosePlusVersions.Add(Loc.Get("Settings.WindrosePlus.Version.Latest"));
                foreach (var t in tags)
                    AvailableWindrosePlusVersions.Add(t);
                IsVersionListLoading = false;
            });
        }
        catch
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsVersionListLoading = false);
        }
    }

    private void LoadInstalledWindrosePlusVersion()
    {
        var dir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(dir)) { InstalledWindrosePlusVersion = null; return; }
        var marker = _wplus.ReadVersionMarker(dir);
        InstalledWindrosePlusVersion = marker?.Tag;
    }

    /// <summary>
    /// Safely fires off an async task without awaiting it.
    /// Catches any exceptions to prevent unobserved task exceptions.
    /// </summary>
    private void SafeFireAndForget(Task task, string taskName = "unknown")
    {
        if (task is null) return;
        _ = task.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception is not null)
            {
                Log.Warning(t.Exception, "Fire-and-forget task '{TaskName}' failed", taskName);
            }
        }, TaskScheduler.Default);
    }

    private void RebuildIntervalOptions()
    {
        _suppressIntervalWrite = true;
        try
        {
            WindrosePlusIntervalOptions.Clear();
            WindrosePlusIntervalOptions.Add(new UpdateIntervalOption { Hours = 0,  DisplayName = Loc.Get("Settings.WindrosePlus.Update.IntervalOff") });
            WindrosePlusIntervalOptions.Add(new UpdateIntervalOption { Hours = 4,  DisplayName = Loc.Format("Settings.WindrosePlus.Update.IntervalHours", 4) });
            WindrosePlusIntervalOptions.Add(new UpdateIntervalOption { Hours = 6,  DisplayName = Loc.Format("Settings.WindrosePlus.Update.IntervalHours", 6) });
            WindrosePlusIntervalOptions.Add(new UpdateIntervalOption { Hours = 12, DisplayName = Loc.Format("Settings.WindrosePlus.Update.IntervalHours", 12) });
            WindrosePlusIntervalOptions.Add(new UpdateIntervalOption { Hours = 24, DisplayName = Loc.Format("Settings.WindrosePlus.Update.IntervalHours", 24) });

            var current = _settings.Current.WindrosePlusUpdateCheckIntervalHours;
            SelectedWindrosePlusInterval =
                WindrosePlusIntervalOptions.FirstOrDefault(o => o.Hours == current)
                ?? WindrosePlusIntervalOptions.First(o => o.Hours == 6);
        }
        finally { _suppressIntervalWrite = false; }
    }

    partial void OnSelectedWindrosePlusIntervalChanged(UpdateIntervalOption? value)
    {
        if (_suppressIntervalWrite || value is null) return;
        if (_settings.Current.WindrosePlusUpdateCheckIntervalHours == value.Hours) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.WindrosePlusUpdateCheckIntervalHours = value.Hours),
            "WindrosePlusUpdateCheckInterval");
    }

    private void OnWindrosePlusUpdateChecked(WindrosePlusUpdateResult r)
        => Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyWindrosePlusResult(r));

    private void ApplyLastWindrosePlusResult()
    {
        if (_wplusUpdate.LastResult is { } r) ApplyWindrosePlusResult(r);
    }

    private void ApplyWindrosePlusResult(WindrosePlusUpdateResult r)
    {
        WindrosePlusUpdateStatus = r.Message;
        HasWindrosePlusUpdateAvailable = r.AnyUpdate;
        WindrosePlusLatestTag = r.LatestTag;
        WindrosePlusReleaseUrl = r.ReleaseUrl;
        WindrosePlusPendingCount = r.Servers.Count(s => s.HasUpdate);
    }

    [RelayCommand]
    private async Task CheckWindrosePlusUpdateAsync()
    {
        if (IsWindrosePlusUpdateCheckBusy) return;
        try
        {
            IsWindrosePlusUpdateCheckBusy = true;
            WindrosePlusUpdateStatus = Loc.Get("Toast.UpdateChecking");
            var result = await _wplusUpdate.CheckAsync();
            ApplyWindrosePlusResult(result);
            if (!result.Succeeded) _toasts.Warning(result.Message);
            else if (result.AnyUpdate) _toasts.Info(result.Message);
            else _toasts.Success(result.Message);
        }
        catch (Exception ex)
        {
            WindrosePlusUpdateStatus = ErrorMessageHelper.FriendlyMessage(ex);
            _toasts.Error(WindrosePlusUpdateStatus);
        }
        finally { IsWindrosePlusUpdateCheckBusy = false; }
    }

    [RelayCommand]
    private void OpenWindrosePlusReleasePage()
    {
        if (!string.IsNullOrWhiteSpace(WindrosePlusReleaseUrl))
            TryOpenUrl(WindrosePlusReleaseUrl!);
    }

    public string AppVersionDisplay =>
        Loc.Format("Settings.About.VersionFormat",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.2.5");

    private void OnLanguageChanged()
    {
        RebuildLanguageOptions();
        OnPropertyChanged(nameof(AppVersionDisplay));
    }

    private void RebuildLanguageOptions()
    {
        _suppressLanguageWrite = true;
        try
        {
            LanguageOptions.Clear();
            LanguageOptions.Add(new LanguageOption { Key = "auto", DisplayName = Loc.Get("Settings.Language.Auto") });
            LanguageOptions.Add(new LanguageOption { Key = "de",   DisplayName = Loc.Get("Settings.Language.German") });
            LanguageOptions.Add(new LanguageOption { Key = "en",   DisplayName = Loc.Get("Settings.Language.English") });

            var current = _localization.CurrentSetting;
            SelectedLanguageOption = LanguageOptions.FirstOrDefault(o => o.Key == current) ?? LanguageOptions[0];
        }
        finally
        {
            _suppressLanguageWrite = false;
        }
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (_suppressLanguageWrite || value is null) return;
        if (string.Equals(value.Key, _localization.CurrentSetting, StringComparison.OrdinalIgnoreCase)) return;

        _localization.SetLanguage(value.Key);
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.Language = value.Key),
            "Language");
    }

    [RelayCommand]
    private Task CheckFirewallAsync() => CheckFirewallCoreAsync(showToast: true);

    private async Task CheckFirewallCoreAsync(bool showToast)
    {
        var binary = ResolveServerBinary();
        if (string.IsNullOrWhiteSpace(binary))
        {
            IsFirewallRuleInstalled = false;
            if (showToast) _toasts.Warning(Loc.Get("Toast.FirewallBinaryMissing"));
            return;
        }
        try
        {
            IsFirewallBusy = true;
            IsFirewallRuleInstalled = await _firewall.IsRuleInstalledAsync(binary);
            if (showToast)
            {
                if (IsFirewallRuleInstalled) _toasts.Success(Loc.Get("Toast.FirewallRuleActive"));
                else _toasts.Info(Loc.Get("Toast.FirewallNoRule"));
            }
        }
        catch (Exception ex)
        {
            if (showToast) _toasts.Error(Loc.Format("Toast.FirewallCheckFailedFormat", ErrorMessageHelper.FriendlyMessage(ex)));
        }
        finally
        {
            IsFirewallBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFirewallAsync()
    {
        var binary = ResolveServerBinary();
        if (string.IsNullOrWhiteSpace(binary))
        {
            _toasts.Warning(Loc.Get("Toast.FirewallBinaryMissing"));
            return;
        }

        if (!FirewallService.IsCurrentProcessElevated())
        {
            _toasts.Warning(Loc.Get("Toast.FirewallAdminNeeded"));
            return;
        }

        try
        {
            IsFirewallBusy = true;
            bool ok;
            if (IsFirewallRuleInstalled)
            {
                ok = await _firewall.RemoveRuleAsync(binary);
                if (ok) _toasts.Success(Loc.Get("Toast.FirewallRuleRemoved"));
                else _toasts.Error(Loc.Get("Toast.FirewallRuleNotRemoved"));
            }
            else
            {
                ok = await _firewall.InstallRuleAsync(binary);
                if (ok) _toasts.Success(Loc.Get("Toast.FirewallRuleAdded"));
                else _toasts.Error(Loc.Get("Toast.FirewallRuleNotAdded"));
            }
            await CheckFirewallCoreAsync(showToast: false);
        }
        finally
        {
            IsFirewallBusy = false;
        }
    }

    private void OnSettingsChanged(WindroseServerManager.Core.Models.AppSettings settings)
    {
        SafeFireAndForget(CheckFirewallCoreAsync(showToast: false), "CheckFirewall");
    }

    private string? ResolveServerBinary()
        => ServerInstallService.FindServerBinary(_settings.ActiveServerDir);

    partial void OnAutoStartServerOnAppLaunchChanged(bool value)
    {
        if (_suppressAutoStartWrite) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.AutoStartServerOnAppLaunch = value),
            "AutoStartServerOnAppLaunch");
    }

    partial void OnAutoStartDelaySecondsChanged(int value)
    {
        if (_suppressAutoStartWrite) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.AutoStartDelaySeconds = Math.Clamp(value, 0, 60)),
            "AutoStartDelaySeconds");
    }

    partial void OnAutoStartEnabledChanged(bool value)
    {
        if (_suppressAutoStartWrite) return;
        try
        {
            if (value)
            {
                var exe = Environment.ProcessPath
                    ?? System.Reflection.Assembly.GetEntryAssembly()?.Location
                    ?? string.Empty;
                if (string.IsNullOrWhiteSpace(exe))
                {
                    _toasts.Warning(Loc.Get("Toast.AppPathUnknown"));
                    return;
                }
                _autoStart.Enable(exe);
                _toasts.Success(Loc.Get("Toast.AutoStartOn"));
            }
            else
            {
                _autoStart.Disable();
                _toasts.Info(Loc.Get("Toast.AutoStartOff"));
            }
        }
        catch (Exception ex)
        {
            _toasts.Error(Loc.Format("Toast.AutoStartErrorFormat", ErrorMessageHelper.FriendlyMessage(ex)));
        }
    }

    partial void OnCloseToTrayChanged(bool value)
    {
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.CloseToTray = value),
            "CloseToTray");
    }

    partial void OnAutoRestartOnCrashChanged(bool value)
    {
        if (_suppressPersist) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.AutoRestartOnCrash = value),
            "AutoRestartOnCrash");
    }

    partial void OnGracefulShutdownSecondsChanged(int value)
    {
        if (_suppressPersist) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.GracefulShutdownSeconds = Math.Max(5, value)),
            "GracefulShutdownSeconds");
    }

    partial void OnLogEnabledChanged(bool value)
    {
        if (_suppressPersist) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.LogEnabled = value),
            "LogEnabled");
    }

    partial void OnExtraLaunchArgsChanged(string value)
    {
        if (_suppressPersist) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.ExtraLaunchArgs = value?.Trim() ?? string.Empty),
            "ExtraLaunchArgs");
    }

    partial void OnSteamAppIdChanged(string value)
    {
        if (_suppressPersist) return;
        // Leer bleibt leer während Tippens — Fallback auf 4129620 nur wenn der User das Feld leer lässt.
        var v = value?.Trim();
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.SteamAppId = string.IsNullOrEmpty(v) ? "4129620" : v),
            "SteamAppId");
    }

    partial void OnSteamLoginChanged(string value)
    {
        if (_suppressPersist) return;
        SafeFireAndForget(
            _settings.UpdateAsync(s => s.SteamLogin = value?.Trim() ?? string.Empty),
            "SteamLogin");
    }

    partial void OnEnableDiscordBotChanged(bool value)
    {
        if (_suppressPersist) return;
        try { _ = _settings.UpdateAsync(s => s.EnableDiscordBot = value); }
        catch (Exception ex) { Log.Warning(ex, "Failed to update EnableDiscordBot setting"); }
    }

    partial void OnDiscordBotTokenChanged(string value)
    {
        if (_suppressPersist) return;
        try { _ = _settings.UpdateAsync(s => s.DiscordBotToken = value?.Trim() ?? string.Empty); }
        catch (Exception ex) { Log.Warning(ex, "Failed to update DiscordBotToken setting"); }
    }

    partial void OnDiscordGuildIdChanged(string value)
    {
        if (_suppressPersist) return;
        if (ulong.TryParse(value?.Trim() ?? "0", out var id))
        {
            try { _ = _settings.UpdateAsync(s => s.DiscordGuildId = id); }
            catch (Exception ex) { Log.Warning(ex, "Failed to update DiscordGuildId setting"); }
        }
    }

    partial void OnDiscordLogChannelIdChanged(string value)
    {
        if (_suppressPersist) return;
        if (ulong.TryParse(value?.Trim() ?? "0", out var id))
        {
            try { _ = _settings.UpdateAsync(s => s.DiscordLogChannelId = id); }
            catch (Exception ex) { Log.Warning(ex, "Failed to update DiscordLogChannelId setting"); }
        }
    }

    partial void OnIsWindrosePlusEnabledChanged(bool value)
    {
        if (_suppressWpSettings) return;

        var serverDir = _settings.ActiveServerDir;
        var hasWplusFiles = !string.IsNullOrWhiteSpace(serverDir)
            && File.Exists(Path.Combine(serverDir, ".wplus-version"));

        if (value && !hasWplusFiles)
        {
            SafeFireAndForget(
                _settings.UpdateAsync(s =>
                {
                    var entry = s.Servers.FirstOrDefault(srv => srv.Id == s.ActiveServerId);
                    if (entry is not null) entry.IsWindrosePlusEnabled = true;
                }),
                "IsWindrosePlusEnabled");
            SafeFireAndForget(OpenWindrosePlusDialogAsync(), "WplusToggleInstallPrompt");
            return;
        }

        SafeFireAndForget(
            _settings.UpdateAsync(s =>
            {
                var entry = s.Servers.FirstOrDefault(srv => srv.Id == s.ActiveServerId);
                if (entry is not null) entry.IsWindrosePlusEnabled = value;
            }),
            "IsWindrosePlusEnabled");

        if (value)
            _toasts.Success(Loc.Get("Toast.WindrosePlusEnabled"));
        else
            _toasts.Success(Loc.Get("Toast.WindrosePlusDisabled"));

        var server = App.Services.GetService(typeof(IServerProcessService)) as IServerProcessService;
        if (server?.Status is ServerStatus.Running or ServerStatus.Starting)
            _toasts.Warning(Loc.Get("Toast.WindrosePlusToggleRestartRequired"));

        OnPropertyChanged(nameof(WindrosePlusStatusText));
    }

    partial void OnPinnedWindrosePlusVersionChanged(string value)
    {
        if (_suppressWpSettings) return;
        var v = value?.Trim() ?? string.Empty;
        var latestLabel = Loc.Get("Settings.WindrosePlus.Version.Latest");
        if (string.Equals(v, latestLabel, StringComparison.OrdinalIgnoreCase)) v = string.Empty;
        SafeFireAndForget(
            _settings.UpdateAsync(s =>
            {
                var entry = s.Servers.FirstOrDefault(srv => srv.Id == s.ActiveServerId);
                if (entry is not null) entry.PinnedWindrosePlusVersion = string.IsNullOrWhiteSpace(v) ? null : v;
            }),
            "PinnedWindrosePlusVersion");
    }

    partial void OnBackupDirOverrideChanged(string value)
    {
        if (_suppressDirOverrides) return;
        var v = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        SafeFireAndForget(
            _settings.UpdateAsync(s =>
            {
                var entry = s.Servers.FirstOrDefault(srv => srv.Id == s.ActiveServerId);
                if (entry is not null) entry.BackupDirOverride = v;
            }),
            "BackupDirOverride");
        if (v is not null) _toasts.Success(Loc.Get("Toast.BackupDirUpdated"));
    }

    partial void OnModsDirOverrideChanged(string value)
    {
        if (_suppressDirOverrides) return;
        var v = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        SafeFireAndForget(
            _settings.UpdateAsync(s =>
            {
                var entry = s.Servers.FirstOrDefault(srv => srv.Id == s.ActiveServerId);
                if (entry is not null) entry.ModsDirOverride = v;
            }),
            "ModsDirOverride");
        if (v is not null) _toasts.Success(Loc.Get("Toast.ModsDirUpdated"));
    }

    [RelayCommand]
    private async Task BrowseBackupDirAsync()
    {
        var top = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null;
        if (top is null) return;
        var picks = await top.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = Loc.Get("Settings.PerServer.BackupDir.Title")
        });
        if (picks.Count > 0)
        {
            BackupDirOverride = picks[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private void ResetBackupDir() => BackupDirOverride = string.Empty;

    [RelayCommand]
    private async Task BrowseModsDirAsync()
    {
        var top = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null;
        if (top is null) return;
        var picks = await top.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = Loc.Get("Settings.PerServer.ModsDir.Title")
        });
        if (picks.Count > 0)
        {
            ModsDirOverride = picks[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private void ResetModsDir() => ModsDirOverride = string.Empty;

    public string AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.2.5";

    [RelayCommand]
    private async Task CheckAppUpdateAsync()
    {
        if (IsUpdateCheckBusy) return;
        try
        {
            IsUpdateCheckBusy = true;
            UpdateCheckStatus = Loc.Get("Toast.UpdateChecking");
            HasUpdateAvailable = false;
            PendingReleaseUrl = null;
            PendingDownloadUrl = null;

            var result = await _appUpdate.CheckAsync();
            UpdateCheckStatus = result.Message;
            HasUpdateAvailable = result.HasUpdate;
            PendingReleaseUrl = result.ReleaseUrl;
            PendingDownloadUrl = result.DownloadUrl;

            if (result.HasUpdate) _toasts.Info(result.Message);
            else _toasts.Success(result.Message);
        }
        catch (Exception ex)
        {
            UpdateCheckStatus = Loc.Get("Toast.UpdateCheckFailed");
            _toasts.Error(ErrorMessageHelper.FriendlyMessage(ex));
        }
        finally
        {
            IsUpdateCheckBusy = false;
        }
    }

    [RelayCommand]
    private void DownloadAppUpdate()
    {
        var url = PendingDownloadUrl ?? PendingReleaseUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        TryOpenUrl(url);
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        var url = PendingReleaseUrl ?? PendingDownloadUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        TryOpenUrl(url);
    }

    private void TryOpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _toasts.Error(Loc.Get("Toast.ReleasePageFailed"));
            System.Diagnostics.Debug.WriteLine($"Failed to open URL {url}: {ex.Message}");
        }
    }

    // ── WindrosePlus ────────────────────────────────────────────
    public bool HasServerConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ActiveServerDir);

    public string RconPassword
    {
        get
        {
            var dir = _settings.ActiveServerDir;
            if (string.IsNullOrWhiteSpace(dir)) return string.Empty;

            // Try windrose_plus.json first (JsonElement-safe extraction)
            var config = _wplusApi.ReadConfig(dir);
            if (config?.Rcon.TryGetValue("password", out var pw) == true && pw is not null)
            {
                var s = pw is System.Text.Json.JsonElement el ? el.GetString() : pw as string;
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }

            // Fallback: password stored in app settings (set via install wizard)
            return _settings.Current.WindrosePlusRconPasswordByServer
                .GetValueOrDefault(dir, string.Empty);
        }
    }

    [ObservableProperty] private bool _showRconPassword;

    [RelayCommand]
    private void ToggleShowRconPassword() => ShowRconPassword = !ShowRconPassword;

    [RelayCommand]
    private async Task CopyRconPasswordAsync()
    {
        var pw = RconPassword;
        if (string.IsNullOrEmpty(pw)) return;
        var top = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null;
        if (top?.Clipboard is null) return;
        await top.Clipboard.SetTextAsync(pw);
        _toasts.Success(Loc.Get("Settings.WindrosePlus.RconPasswordCopied"));
    }

    public string WindrosePlusStatusText
    {
        get
        {
            var dir = _settings.ActiveServerDir;
            if (string.IsNullOrWhiteSpace(dir)) return Loc.Get("Settings.WindrosePlus.StatusNoServer");
            var active = _settings.Current.WindrosePlusActiveByServer.GetValueOrDefault(dir, false);
            if (active) return Loc.Get("Settings.WindrosePlus.StatusActive");
            var state = _settings.Current.WindrosePlusOptInStateByServer.GetValueOrDefault(dir, OptInState.OptedOut);
            return state == OptInState.OptedOut
                ? Loc.Get("Settings.WindrosePlus.StatusOptedOut")
                : Loc.Get("Settings.WindrosePlus.StatusNotInstalled");
        }
    }

    public bool CanForceReinstallWindrosePlus
    {
        get
        {
            var dir = _settings.ActiveServerDir;
            if (string.IsNullOrWhiteSpace(dir)) return false;
            return _wplus.IsPhysicallyInstalled(dir);
        }
    }

    [RelayCommand(CanExecute = nameof(CanForceReinstallWindrosePlus))]
    private async Task ForceReinstallWindrosePlusAsync(CancellationToken ct)
    {
        var dir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(dir)) return;

        var top = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null;
        if (top is null) return;

        var confirmed = await ConfirmDialog.ShowAsync(
            top,
            Loc.Get("Confirm.ForceReinstall.Title"),
            Loc.Get("Confirm.ForceReinstall.Message"),
            Loc.Get("Confirm.ForceReinstall.Label"));
        if (!confirmed) return;

        IsForceReinstallBusy = true;
        try
        {
            await _wplus.DeleteInstallFilesAsync(dir, ct);

            var result = await _wplus.InstallAsync(dir, null, ct);
            if (result is not null)
            {
                _toasts.Success(Loc.Get("Toast.WindrosePlusForceReinstallSuccess"));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Force reinstall of Windrose+ failed");
            _toasts.Error(Loc.Format("Toast.WindrosePlusForceReinstallFailed", ex.Message));
        }
        finally
        {
            IsForceReinstallBusy = false;
            OnPropertyChanged(nameof(CanForceReinstallWindrosePlus));
            OnPropertyChanged(nameof(WindrosePlusStatusText));
        }
    }

    [RelayCommand]
    private async Task OpenWindrosePlusDialogAsync()
    {
        var dir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(dir)) return;

        var top = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null;
        if (top is null) return;

        var bannerVm = new RetrofitBannerViewModel(dir, _wplus, _wplusApi, _settings, _toasts);
        var dialog = new RetrofitDialog { DataContext = bannerVm };
        var confirmed = await dialog.ShowDialog<bool>(top);
        if (confirmed)
            OnPropertyChanged(nameof(WindrosePlusStatusText));
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        var top = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null;
        if (top is null) return;
        await AboutDialog.ShowAsync(top);
    }
}
