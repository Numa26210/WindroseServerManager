using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindroseServerManager.Core.Models;

namespace WindroseServerManager.Core.Services;

/// <summary>
/// Triggered sources for scheduled and automatic restarts. Raised on the scheduler thread.
/// </summary>
public sealed record RestartEvent(RestartTrigger Trigger, string Reason);

public enum RestartTrigger { ScheduledTime, HighRam, MaxUptime, ScheduledWarning }

/// <summary>
/// Hosted service that triggers scheduled + threshold-based restarts. Raises RestartNotified
/// for warnings (so the UI can toast) and actual restart events.
/// </summary>
public sealed class RestartScheduler : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly ILogger<RestartScheduler> _logger;
    private readonly IAppSettingsService _settings;
    private readonly IServerProcessService _server;
    private readonly IMetricsService _metrics;
    private readonly IServerEventLog _events;
    private readonly IBackupService _backupService;

    private bool _warningSent;
    private bool _hasRestartedToday;
    private DateTime _lastFlagResetDate = DateTime.MinValue;
    private DateTime _lastAutoRestartUtc = DateTime.MinValue;

    public event Action<RestartEvent>? RestartNotified;

    public RestartScheduler(
        ILogger<RestartScheduler> logger,
        IAppSettingsService settings,
        IServerProcessService server,
        IMetricsService metrics,
        IServerEventLog events,
        IBackupService backupService)
    {
        _logger = logger;
        _settings = settings;
        _server = server;
        _metrics = metrics;
        _events = events;
        _backupService = backupService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RestartScheduler started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;

                if (_lastFlagResetDate != now.Date)
                {
                    _warningSent = false;
                    _hasRestartedToday = false;
                    _lastFlagResetDate = now.Date;
                }

                var scheduledTriggered = false;

                if (_settings.Current.ScheduledRestartEnabled
                    && _server.Status == ServerStatus.Running
                    && IsDayEnabled(now))
                {
                    var timeStr = _settings.Current.DailyRestartTime;
                    if (TimeSpan.TryParse(timeStr, out var target))
                    {
                        var todayAt = now.Date + target;
                        var timeUntilRestart = todayAt - now;
                        var warnMins = _settings.Current.RestartWarnMinutes;

                        if (!_warningSent
                            && warnMins > 0
                            && timeUntilRestart <= TimeSpan.FromMinutes(warnMins)
                            && timeUntilRestart > TimeSpan.Zero)
                        {
                            var mins = Math.Max(0, warnMins);
                            var warnReason = $"Scheduled restart in {mins} minutes.";
                            RestartNotified?.Invoke(new RestartEvent(RestartTrigger.ScheduledWarning, warnReason));

                            var serverName = _settings.Current.Servers.FirstOrDefault(s => s.Id == _settings.Current.ActiveServerId)?.Name ?? "Server";
                            await _events.AppendAsync(new ServerEvent(DateTime.UtcNow, ServerEventType.ScheduledRestart, warnReason, ServerName: serverName), stoppingToken).ConfigureAwait(false);

                            _warningSent = true;
                        }

                        if (!_hasRestartedToday
                            && timeUntilRestart <= TimeSpan.Zero
                            && timeUntilRestart >= TimeSpan.FromMinutes(-2))
                        {
                            await TriggerRestartAsync(RestartTrigger.ScheduledTime, "Scheduled restart.", stoppingToken).ConfigureAwait(false);
                            _hasRestartedToday = true;
                            scheduledTriggered = true;
                        }
                    }
                }

                if (!scheduledTriggered
                    && _server.Status == ServerStatus.Running
                    && (DateTime.UtcNow - _lastAutoRestartUtc).TotalMinutes >= 5)
                {
                    var (threshold, reason) = await CheckAutoRestartThresholdsAsync(stoppingToken).ConfigureAwait(false);
                    if (threshold is not null)
                    {
                        await TriggerRestartAsync(threshold.Value, reason, stoppingToken).ConfigureAwait(false);
                        _lastAutoRestartUtc = DateTime.UtcNow;
                    }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RestartScheduler loop error");
            }

            try { await Task.Delay(CheckInterval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }
        }
        _logger.LogInformation("RestartScheduler stopped");
    }

    private bool IsDayEnabled(DateTime now)
    {
        var days = _settings.Current.RestartDays;
        if (days is null || days.Count == 0) return true; // leer = täglich
        return days.Contains(now.DayOfWeek);
    }

    private async Task<(RestartTrigger? trigger, string reason)> CheckAutoRestartThresholdsAsync(CancellationToken ct)
    {
        var s = _settings.Current;

        if (s.AutoRestartOnMaxUptimeEnabled && _server.StartedAtUtc is not null)
        {
            var uptime = DateTime.UtcNow - _server.StartedAtUtc.Value;
            if (uptime.TotalHours >= Math.Max(1, s.AutoRestartMaxUptimeHours))
                return (RestartTrigger.MaxUptime, $"Uptime limit reached ({(int)uptime.TotalHours}h).");
        }

        if (s.AutoRestartOnHighRamEnabled)
        {
            try
            {
                var host = await _metrics.GetHostMetricsAsync(ct: ct).ConfigureAwait(false);
                var proc = _metrics.GetServerProcessMetrics();
                if (proc is not null && host.RamTotalBytes > 0)
                {
                    var pct = proc.RamBytes * 100.0 / host.RamTotalBytes;
                    if (pct >= Math.Max(10, s.AutoRestartRamThresholdPercent))
                        return (RestartTrigger.HighRam, $"RAM limit reached ({pct:F0}%).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Threshold-Check RAM fehlgeschlagen");
            }
        }

        return (null, string.Empty);
    }

    private async Task TriggerRestartAsync(RestartTrigger trigger, string reason, CancellationToken ct)
    {
        _logger.LogInformation("Restart trigger={Trigger} reason={Reason}", trigger, reason);
        RestartNotified?.Invoke(new RestartEvent(trigger, reason));

        var serverName = _settings.Current.Servers.FirstOrDefault(s => s.Id == _settings.Current.ActiveServerId)?.Name ?? "Server";

        if (trigger != RestartTrigger.ScheduledTime)
        {
            var eventType = trigger switch
            {
                RestartTrigger.HighRam => ServerEventType.AutoRestartHighRam,
                RestartTrigger.MaxUptime => ServerEventType.AutoRestartMaxUptime,
                _ => ServerEventType.ScheduledRestart,
            };
            await _events.AppendAsync(new ServerEvent(DateTime.UtcNow, eventType, reason, ServerName: serverName), ct).ConfigureAwait(false);
        }

        try { await _server.StopAsync(ct).ConfigureAwait(false); }
        catch (Exception ex) { _logger.LogError(ex, "Scheduled restart: stop failed"); }

        try
        {
            using var exitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            exitCts.CancelAfter(TimeSpan.FromSeconds(10));
            while (_server.Status != ServerStatus.Stopped && _server.Status != ServerStatus.Crashed)
            {
                await Task.Delay(200, exitCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Server process did not reach Stopped/Crashed state within 10s after stop — proceeding with backup anyway");
        }

        if (_settings.Current.BackupOnRestartEnabled)
        {
            var graceDelay = Math.Clamp(_settings.Current.BackupGraceDelaySeconds, 0, 30);
            if (graceDelay > 0)
            {
                _logger.LogInformation("Waiting {Grace}s grace delay before backup", graceDelay);
                try { await Task.Delay(TimeSpan.FromSeconds(graceDelay), ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }

            try
            {
                _logger.LogInformation("Creating backup before restart");
                await _backupService.CreateBackupAsync(isAutomatic: true, ct).ConfigureAwait(false);
                _logger.LogInformation("Backup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup on restart failed, but proceeding with restart");
                await _events.AppendAsync(
                    new ServerEvent(DateTime.UtcNow, ServerEventType.BackupOnRestartFailed, $"Backup failed before restart: {ex.Message}", ServerName: serverName),
                    ct).ConfigureAwait(false);
            }
        }

        try { await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }

        try
        {
            await _server.StartAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Scheduled restart complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled restart: start failed");
        }
    }
}
