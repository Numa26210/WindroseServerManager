using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindroseServerManager.App.Services;
using WindroseServerManager.App.Views.Dialogs;
using WindroseServerManager.Core.Models;
using WindroseServerManager.Core.Services;

namespace WindroseServerManager.App.ViewModels;

public partial class ServerControlViewModel : ViewModelBase, IDisposable
{
    private readonly IServerProcessService _proc;
    private readonly IAppSettingsService _settings;
    private readonly IServerConfigService _config;
    private readonly IToastService _toasts;
    private readonly IServerEventLog _eventLog;
    private readonly Avalonia.Threading.DispatcherTimer _refreshTimer;

    public ObservableCollection<ServerEvent> Events { get; } = new();

    [ObservableProperty] private ServerStatus _status;
    [ObservableProperty] private string _uptimeText = "—";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _inviteCode;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private bool _restartInstallUpdateBeforeStart;
    [ObservableProperty] private bool _restartCreateBackupBeforeStart;
    [ObservableProperty] private bool _restartBroadcastEnabled;
    [ObservableProperty] private string _restartBroadcastMessage = string.Empty;

    public bool CanOpenServerDir => !string.IsNullOrWhiteSpace(_settings.ActiveServerDir)
                                    && Directory.Exists(_settings.ActiveServerDir);

    public bool CanOpenServerDescription
    {
        get
        {
            var p = _config.GetServerDescriptionPath();
            return !string.IsNullOrWhiteSpace(p) && File.Exists(p);
        }
    }

    public bool HasAutomationSummary =>
        !string.IsNullOrEmpty(AutomationSummaryLine1) || !string.IsNullOrEmpty(AutomationSummaryLine2);

    public string AutomationSummaryLine1
    {
        get
        {
            var c = _settings.Current;
            var parts = new List<string>();
            if (c.ScheduledRestartEnabled)
            {
                var dayNames = BuildDaySummary(c);
                parts.Add($"{Loc.Get("ServerControl.Summary.DailyRestart")} {c.DailyRestartTime} — {dayNames}");
            }
            if (c.AutoRestartOnCrash)
                parts.Add(Loc.Get("ServerControl.Summary.AutoRestartOnCrash"));
            return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
        }
    }

    public string AutomationSummaryLine2
    {
        get
        {
            var c = _settings.Current;
            var parts = new List<string>();
            if (c.AutoRestartOnHighRamEnabled)
                parts.Add($"{Loc.Get("ServerControl.Summary.RamOver")} {c.AutoRestartRamThresholdPercent}%");
            if (c.AutoRestartOnMaxUptimeEnabled)
                parts.Add($"{Loc.Get("ServerControl.Summary.UptimeOver")} {c.AutoRestartMaxUptimeHours}h");
            if (c.BackupOnRestartEnabled)
                parts.Add($"{Loc.Get("ServerControl.Summary.BackupOnRestart")} ({c.BackupGraceDelaySeconds}s grace)");
            return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
        }
    }

    private static string BuildDaySummary(AppSettings c)
    {
        var days = c.RestartDays ?? new List<DayOfWeek>();
        if (days.Count == 0) return Loc.Get("ServerControl.Summary.EveryDay");
        var keys = new[] { "Weekday.Mon", "Weekday.Tue", "Weekday.Wed", "Weekday.Thu", "Weekday.Fri", "Weekday.Sat", "Weekday.Sun" };
        var allDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var names = new List<string>();
        for (int i = 0; i < allDays.Length; i++)
        {
            if (days.Contains(allDays[i]))
                names.Add(Loc.Get(keys[i]));
        }
        return string.Join(' ', names);
    }

    public ServerControlViewModel(IServerProcessService proc, IAppSettingsService settings, IServerConfigService config, IToastService toasts, IServerEventLog eventLog, ILocalizationService localization)
    {
        _proc = proc;
        _settings = settings;
        _config = config;
        _toasts = toasts;
        _eventLog = eventLog;
        _proc.StatusChanged += OnStatus;
        _eventLog.Appended += OnEventAppended;
        _status = _proc.Status;

        _ = LoadEventsAsync();

        var c = settings.Current;
        _restartInstallUpdateBeforeStart = c.RestartInstallUpdateBeforeStart;
        _restartCreateBackupBeforeStart = c.RestartCreateBackupBeforeStart;
        _restartBroadcastEnabled = c.RestartBroadcastEnabled;
        _restartBroadcastMessage = c.RestartBroadcastMessage ?? string.Empty;

        _settings.Changed += OnSettingsChanged;

        _ = LoadInviteCodeAsync();

        _refreshTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += async (_, _) => await LoadInviteCodeAsync();
        _refreshTimer.Start();
    }

    partial void OnRestartBroadcastEnabledChanged(bool value) => SaveRestartScheduleAsync();
    partial void OnRestartBroadcastMessageChanged(string value) => SaveRestartScheduleAsync();
    partial void OnRestartInstallUpdateBeforeStartChanged(bool value) => SaveRestartScheduleAsync();
    partial void OnRestartCreateBackupBeforeStartChanged(bool value) => SaveRestartScheduleAsync();

    private async Task SaveRestartScheduleAsync()
    {
        await _settings.UpdateAsync(s =>
        {
            s.RestartInstallUpdateBeforeStart = RestartInstallUpdateBeforeStart;
            s.RestartCreateBackupBeforeStart = RestartCreateBackupBeforeStart;
            s.RestartBroadcastEnabled = RestartBroadcastEnabled;
            s.RestartBroadcastMessage = RestartBroadcastMessage ?? string.Empty;
        });
    }

    public string BroadcastMessagePlaceholder => Loc.Get("ServerControl.BroadcastMessage.Placeholder");

    private void OnStatus(ServerStatus s) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
    {
        Status = s;
        UpdateUptime();
    });

    private void UpdateUptime()
    {
        if (_proc.StartedAtUtc is null) { UptimeText = "—"; return; }
        var t = DateTime.UtcNow - _proc.StartedAtUtc.Value;
        UptimeText = t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
    }

    private static Avalonia.Controls.Window? GetOwnerWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d
                ? d.MainWindow
                : null;
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        ErrorMessage = _proc.ValidateCanStart();
        if (ErrorMessage is not null) { _toasts.Warning(ErrorMessage); return; }
        try { await _proc.StartAsync(); _toasts.Success(Loc.Get("Toast.ServerStarting")); }
        catch (Exception ex) { var msg = ErrorMessageHelper.FriendlyMessage(ex); ErrorMessage = msg; _toasts.Error(msg); }
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        try { await _proc.StopAsync(); _toasts.Info(Loc.Get("Toast.ServerStopping")); }
        catch (Exception ex) { var msg = ErrorMessageHelper.FriendlyMessage(ex); ErrorMessage = msg; _toasts.Error(msg); }
    }

    [RelayCommand]
    private async Task KillAsync()
    {
        // Kein Confirm-Dialog wenn der Server bereits (fast) aus ist.
        if (_proc.Status is ServerStatus.Running or ServerStatus.Starting)
        {
            var owner = GetOwnerWindow();
            if (owner is not null)
            {
                var confirmed = await ConfirmDialog.ShowAsync(
                    owner,
                    Loc.Get("Confirm.Kill.Title"),
                    Loc.Get("Confirm.Kill.Message"),
                    confirmLabel: Loc.Get("Confirm.Kill.Label"),
                    danger: true);
                if (!confirmed) return;
            }
        }

        try { await _proc.KillAsync(); _toasts.Warning(Loc.Get("Toast.ServerKilled")); }
        catch (Exception ex) { var msg = ErrorMessageHelper.FriendlyMessage(ex); ErrorMessage = msg; _toasts.Error(msg); }
    }

    [RelayCommand]
    private async Task RestartAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _toasts.Info(Loc.Get("Toast.RestartInProgress"));

            try { await _proc.StopAsync(); }
            catch (Exception ex) { var msg = ErrorMessageHelper.FriendlyMessage(ex); ErrorMessage = msg; _toasts.Error(msg); return; }

            // Polling: max 10s, alle 500ms
            var maxWait = TimeSpan.FromSeconds(10);
            var step = TimeSpan.FromMilliseconds(500);
            var waited = TimeSpan.Zero;
            while (_proc.Status != ServerStatus.Stopped && waited < maxWait)
            {
                await Task.Delay(step);
                waited += step;
            }

            if (_proc.Status != ServerStatus.Stopped)
            {
                _toasts.Warning(Loc.Get("Toast.StopTooSlow"));
                return;
            }

            ErrorMessage = _proc.ValidateCanStart();
            if (ErrorMessage is not null) { _toasts.Warning(ErrorMessage); return; }

            try { await _proc.StartAsync(); _toasts.Success(Loc.Get("Toast.ServerRestarting")); }
            catch (Exception ex) { var msg = ErrorMessageHelper.FriendlyMessage(ex); ErrorMessage = msg; _toasts.Error(msg); }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadInviteCodeAsync()
    {
        try
        {
            var desc = await _config.LoadServerDescriptionAsync();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                InviteCode = string.IsNullOrWhiteSpace(desc?.InviteCode) ? null : desc!.InviteCode;
                OnPropertyChanged(nameof(CanOpenServerDir));
                OnPropertyChanged(nameof(CanOpenServerDescription));
            });
        }
        catch { }
    }

    [RelayCommand]
    private async Task CopyInviteCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(InviteCode)) return;
        var top = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d ? d.MainWindow : null;
        if (top?.Clipboard is null) return;
        await top.Clipboard.SetTextAsync(InviteCode);
        _toasts.Success(Loc.Format("Toast.InviteCopiedFormat", InviteCode));
    }

    [RelayCommand]
    private void OpenServerDir()
    {
        var path = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch { }
    }

    [RelayCommand]
    private void OpenServerDescription()
    {
        var path = _config.GetServerDescriptionPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch { }
    }

    [RelayCommand]
    private async Task ClearSessionHistoryAsync()
    {
        await _eventLog.ClearAsync().ConfigureAwait(false);
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Events.Clear());
        _toasts.Info(Loc.Get("Toast.SessionHistoryCleared"));
    }

    private async Task LoadEventsAsync()
    {
        var list = await _eventLog.ReadRecentAsync(50).ConfigureAwait(false);
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Events.Clear();
            foreach (var e in list) Events.Add(e);
        });
    }

    private void OnEventAppended(ServerEvent evt) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Events.Insert(0, evt);
            while (Events.Count > 50) Events.RemoveAt(Events.Count - 1);
        });

    private void OnSettingsChanged(AppSettings settings)
    {
        OnPropertyChanged(nameof(HasAutomationSummary));
        OnPropertyChanged(nameof(AutomationSummaryLine1));
        OnPropertyChanged(nameof(AutomationSummaryLine2));

        RestartInstallUpdateBeforeStart = settings.RestartInstallUpdateBeforeStart;
        RestartCreateBackupBeforeStart = settings.RestartCreateBackupBeforeStart;
        RestartBroadcastEnabled = settings.RestartBroadcastEnabled;
        RestartBroadcastMessage = settings.RestartBroadcastMessage ?? string.Empty;
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _proc.StatusChanged -= OnStatus;
        _eventLog.Appended -= OnEventAppended;
    }
}
