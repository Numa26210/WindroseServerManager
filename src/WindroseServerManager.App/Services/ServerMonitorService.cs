using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindroseServerManager.Core.Models;
using WindroseServerManager.Core.Services;

namespace WindroseServerManager.App.Services;

public sealed record MetricSample(DateTime TimestampUtc, double Value);

public interface IServerMonitorService
{
    IReadOnlyList<MetricSample> CpuSamples { get; }
    IReadOnlyList<MetricSample> RamSamples { get; }
    event Action? MetricsUpdated;
    string? LastCrashText { get; }
    int AutoRestartsToday { get; }
}

public sealed class ServerMonitorService : BackgroundService, IServerMonitorService
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(30);
    private static readonly int MaxSamples = 120;

    private readonly ILogger<ServerMonitorService> _logger;
    private readonly IServerProcessService _server;
    private readonly IMetricsService _metrics;
    private readonly IServerEventLog _events;

    private readonly List<MetricSample> _cpuSamples = new();
    private readonly List<MetricSample> _ramSamples = new();

    public IReadOnlyList<MetricSample> CpuSamples => _cpuSamples;
    public IReadOnlyList<MetricSample> RamSamples => _ramSamples;
    public event Action? MetricsUpdated;

    public string? LastCrashText { get; private set; }
    public int AutoRestartsToday { get; private set; }

    public ServerMonitorService(
        ILogger<ServerMonitorService> logger,
        IServerProcessService server,
        IMetricsService metrics,
        IServerEventLog events)
    {
        _logger = logger;
        _server = server;
        _metrics = metrics;
        _events = events;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServerMonitorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_server.Status == ServerStatus.Running)
                {
                    var p = _metrics.GetServerProcessMetrics();
                    if (p is not null)
                    {
                        var now = DateTime.UtcNow;
                        AddSample(_cpuSamples, new MetricSample(now, p.CpuPercent));
                        AddSample(_ramSamples, new MetricSample(now, p.RamBytes / (1024.0 * 1024.0)));
                        MetricsUpdated?.Invoke();
                    }
                }

                await UpdateCrashInfoAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ServerMonitorService sampling error");
            }

            try { await Task.Delay(SampleInterval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task UpdateCrashInfoAsync(CancellationToken ct)
    {
        try
        {
            var events = await _events.ReadRecentAsync(50, ct).ConfigureAwait(false);
            var today = DateTime.UtcNow.Date;

            var lastCrash = events.LastOrDefault(e => e.Type == ServerEventType.Crashed);
            if (lastCrash is not null)
            {
                var hours = (DateTime.UtcNow - lastCrash.TimestampUtc).TotalHours;
                LastCrashText = hours < 1
                    ? Loc.Format("Dashboard.Crash.MinutesAgo", (int)(hours * 60))
                    : Loc.Format("Dashboard.Crash.HoursAgo", (int)hours);
            }
            else
            {
                LastCrashText = Loc.Get("Dashboard.Crash.NoneRecent");
            }

            AutoRestartsToday = events.Count(e =>
                e.Type is ServerEventType.ScheduledRestart or ServerEventType.AutoRestartHighRam or ServerEventType.AutoRestartMaxUptime
                && e.TimestampUtc.Date == today);
        }
        catch { }
    }

    private static void AddSample(List<MetricSample> list, MetricSample sample)
    {
        list.Add(sample);
        while (list.Count > MaxSamples)
            list.RemoveAt(0);
    }
}
