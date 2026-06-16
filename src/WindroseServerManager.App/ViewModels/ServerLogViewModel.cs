using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindroseServerManager.App.Services;
using WindroseServerManager.Core.Models;
using WindroseServerManager.Core.Services;

namespace WindroseServerManager.App.ViewModels;

public enum LogLevelFilter
{
    All,
    InfoPlus,
    WarningPlus,
    ErrorOnly
}

public partial class ServerLogViewModel : ViewModelBase, IDisposable
{
    private readonly IServerProcessService _proc;
    private readonly IAppSettingsService _settings;
    private readonly IToastService _toasts;
    private readonly IServerEventLog _eventLog;

    [ObservableProperty] private LogLevelFilter _currentLogFilter = LogLevelFilter.All;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int _logBufferSize = 2000;

    public int[] LogBufferSizeOptions { get; } = { 500, 2000, 10000 };

    public string FilteredLinesDisplay => Loc.Format("ServerControl.LinesFormat", FilteredLog.Count);

    public ObservableCollection<string> Log { get; } = new();
    public ObservableCollection<string> FilteredLog { get; } = new();

    public bool IsAllFilter
    {
        get => CurrentLogFilter == LogLevelFilter.All;
        set { if (value) CurrentLogFilter = LogLevelFilter.All; }
    }
    public bool IsInfoPlusFilter
    {
        get => CurrentLogFilter == LogLevelFilter.InfoPlus;
        set { if (value) CurrentLogFilter = LogLevelFilter.InfoPlus; }
    }
    public bool IsWarningPlusFilter
    {
        get => CurrentLogFilter == LogLevelFilter.WarningPlus;
        set { if (value) CurrentLogFilter = LogLevelFilter.WarningPlus; }
    }
    public bool IsErrorOnlyFilter
    {
        get => CurrentLogFilter == LogLevelFilter.ErrorOnly;
        set { if (value) CurrentLogFilter = LogLevelFilter.ErrorOnly; }
    }

    public ServerLogViewModel(
        IServerProcessService proc,
        IAppSettingsService settings,
        IToastService toasts,
        ILocalizationService localization,
        IServerEventLog eventLog)
    {
        FilteredLog.CollectionChanged += (_, _) => OnPropertyChanged(nameof(FilteredLinesDisplay));
        localization.LanguageChanged += () => OnPropertyChanged(nameof(FilteredLinesDisplay));

        _proc = proc;
        _settings = settings;
        _toasts = toasts;
        _eventLog = eventLog;
        _proc.LogAppended += OnLog;
        _eventLog.Appended += OnEventAppended;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            foreach (var line in _proc.RecentLog) Log.Add(line.Text);
            RebuildFilteredLog();
        });

        LogBufferSize = settings.Current.LogBufferSize > 0 ? settings.Current.LogBufferSize : 2000;
    }

    partial void OnCurrentLogFilterChanged(LogLevelFilter value)
    {
        OnPropertyChanged(nameof(IsAllFilter));
        OnPropertyChanged(nameof(IsInfoPlusFilter));
        OnPropertyChanged(nameof(IsWarningPlusFilter));
        OnPropertyChanged(nameof(IsErrorOnlyFilter));
        RebuildFilteredLog();
    }

    partial void OnSearchQueryChanged(string value) => RebuildFilteredLog();

    partial void OnLogBufferSizeChanged(int value)
    {
        if (value <= 0) return;
        _ = _settings.UpdateAsync(s => s.LogBufferSize = value);
        TrimLog();
    }

    private void TrimLog()
    {
        var max = Math.Max(100, LogBufferSize);
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            while (Log.Count > max) Log.RemoveAt(0);
            while (FilteredLog.Count > max) FilteredLog.RemoveAt(0);
        });
    }

    private bool MatchesFilter(string line)
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery)
            && line.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        var level = ClassifyLine(line);
        return CurrentLogFilter switch
        {
            LogLevelFilter.All => true,
            LogLevelFilter.InfoPlus => level == LogLevelFilter.InfoPlus || level == LogLevelFilter.WarningPlus || level == LogLevelFilter.ErrorOnly,
            LogLevelFilter.WarningPlus => level == LogLevelFilter.WarningPlus || level == LogLevelFilter.ErrorOnly,
            LogLevelFilter.ErrorOnly => level == LogLevelFilter.ErrorOnly,
            _ => true,
        };
    }

    private static LogLevelFilter ClassifyLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return LogLevelFilter.InfoPlus;
        if (line.Contains("[FEHLER]", StringComparison.OrdinalIgnoreCase)
            || line.Contains("!!!", StringComparison.Ordinal)
            || line.Contains("Error!", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(line, @"Log\w+:\s*Error:", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)))
            return LogLevelFilter.ErrorOnly;
        if (line.Contains("Warning:", StringComparison.OrdinalIgnoreCase)
            || line.Contains("[Warn]", StringComparison.OrdinalIgnoreCase))
            return LogLevelFilter.WarningPlus;
        return LogLevelFilter.InfoPlus;
    }

    private void RebuildFilteredLog()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            FilteredLog.Clear();
            foreach (var line in Log)
                if (MatchesFilter(line))
                    FilteredLog.Add(line);
        });
    }

    private void OnLog(ServerLogLine line) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
    {
        var max = Math.Max(100, LogBufferSize);
        Log.Add(line.Text);
        if (Log.Count > max) Log.RemoveAt(0);

        if (MatchesFilter(line.Text))
        {
            FilteredLog.Add(line.Text);
            if (FilteredLog.Count > max) FilteredLog.RemoveAt(0);
        }
    });

    private void OnEventAppended(ServerEvent ev) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
    {
        var title = GetEventTypeName(ev.Type);
        var detail = string.IsNullOrEmpty(ev.ReasonArg)
            ? (ev.ReasonKey is not null ? Loc.Get(ev.ReasonKey) ?? ev.Reason : ev.Reason)
            : Loc.Format(ev.ReasonKey!, ev.ReasonArg);

        var msg = $"[{ev.TimestampUtc.ToLocalTime():HH:mm:ss}] [Manager] {title} : {detail}";

        var max = Math.Max(100, LogBufferSize);
        Log.Add(msg);
        if (Log.Count > max) Log.RemoveAt(0);

        if (MatchesFilter(msg))
        {
            FilteredLog.Add(msg);
            if (FilteredLog.Count > max) FilteredLog.RemoveAt(0);
        }
    });

    private static string GetEventTypeName(ServerEventType type)
    {
        return type switch
        {
            ServerEventType.Started => Loc.Get("Event.Started"),
            ServerEventType.Stopped => Loc.Get("Event.Stopped"),
            ServerEventType.Crashed => Loc.Get("Event.Crashed"),
            ServerEventType.ScheduledRestart => Loc.Get("Event.ScheduledRestart"),
            ServerEventType.AutoRestartHighRam => Loc.Get("Event.AutoRestartRam"),
            ServerEventType.AutoRestartMaxUptime => Loc.Get("Event.AutoRestartUptime"),
            ServerEventType.BackupOnRestartSuccess => Loc.Get("Event.BackupOnRestartSuccess"),
            ServerEventType.BackupOnRestartFailed => Loc.Get("Event.BackupOnRestartFailed"),
            ServerEventType.BackupManual => Loc.Get("Event.BackupManual"),
            ServerEventType.BackupAutomatic => Loc.Get("Event.BackupAutomatic"),
            ServerEventType.BackupRestored => Loc.Get("Event.BackupRestored"),
            ServerEventType.BackupDeleted => Loc.Get("Event.BackupDeleted"),
            _ => type.ToString(),
        };
    }

    [RelayCommand]
    private void ClearLog()
    {
        Log.Clear();
        FilteredLog.Clear();
        _toasts.Info(Loc.Get("Toast.LogCleared"));
    }

    [RelayCommand]
    private async Task ExportLogAsync()
    {
        var owner = GetOwnerWindow();
        if (owner is null) return;

        var ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Loc.Get("ServerControl.Save.Title"),
            SuggestedFileName = $"windrose-log-{ts}.txt",
            DefaultExtension = "txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(Loc.Get("ServerControl.Save.TextFile")) { Patterns = new[] { "*.txt" } },
            },
        });
        if (file is null) return;

        try
        {
            var path = file.Path.LocalPath;
            var snapshot = Log.ToArray();
            await File.WriteAllLinesAsync(path, snapshot);
            _toasts.Success(Loc.Format("Toast.LogExportedFormat", Path.GetFileName(path)));
        }
        catch (Exception ex)
        {
            _toasts.Error(Loc.Format("Toast.ExportFailedFormat", ErrorMessageHelper.FriendlyMessage(ex)));
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        var installDir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(installDir)) { _toasts.Warning(Loc.Get("Toast.InstallPathUnset")); return; }

        var logDir = Path.Combine(installDir, "R5", "Saved", "Logs");
        if (!Directory.Exists(logDir))
        {
            _toasts.Warning(Loc.Get("Toast.LogFolderMissing"));
            return;
        }
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = logDir, UseShellExecute = true }); }
        catch (Exception ex) { _toasts.Error(ErrorMessageHelper.FriendlyMessage(ex)); }
    }

    private static Avalonia.Controls.Window? GetOwnerWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d
                ? d.MainWindow
                : null;
    }

    public void Dispose()
    {
        _proc.LogAppended -= OnLog;
        _eventLog.Appended -= OnEventAppended;
    }
}
