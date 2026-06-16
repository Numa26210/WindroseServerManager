using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindroseServerManager.Core.Models;

namespace WindroseServerManager.Core.Services;

public sealed class ServerWatchdogService : BackgroundService
{
    private readonly ILogger<ServerWatchdogService> _logger;
    private readonly IAppSettingsService _settings;
    private readonly IServerProcessService _server;
    private readonly IServiceProvider _serviceProvider;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public ServerWatchdogService(
        ILogger<ServerWatchdogService> logger,
        IAppSettingsService settings,
        IServerProcessService server,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings;
        _server = server;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServerWatchdogService started");

        var delay = Math.Clamp(_settings.Current.AutoStartDelaySeconds, 0, 60);
        if (delay > 0)
        {
            _logger.LogInformation("Watchdog: waiting {Delay}s initial delay", delay);
            try { await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CheckDesiredState();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Watchdog check failed");
            }

            try { await Task.Delay(CheckInterval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("ServerWatchdogService stopped");
    }

    private void CheckDesiredState()
    {
        var current = _settings.Current;
        var activeDir = _settings.ActiveServerDir;
        if (string.IsNullOrWhiteSpace(activeDir)) return;

        var normalizedKey = NormalizePath(activeDir);
        if (!current.DesiredServerRunningByServer.TryGetValue(normalizedKey, out var desired)) return;

        var status = _server.Status;
        if (desired && (status == ServerStatus.Stopped || status == ServerStatus.Crashed))
        {
            _logger.LogInformation("Watchdog: desired=running, status={Status} — starting server", status);
            _ = _server.StartAsync();
        }
        else if (!desired && status == ServerStatus.Running)
        {
            _logger.LogInformation("Watchdog: desired=stopped, status=running — stopping server");
            _ = _server.StopAsync();
        }
    }

    public static bool IsDesiredRunning(AppSettings settings, string installDir)
    {
        var key = NormalizePath(installDir);
        return settings.DesiredServerRunningByServer.TryGetValue(key, out var desired) && desired;
    }

    public static async Task SetDesiredRunningAsync(IAppSettingsService settingsService, string installDir, bool running)
    {
        var key = NormalizePath(installDir);
        await settingsService.UpdateAsync(s => s.DesiredServerRunningByServer[key] = running).ConfigureAwait(false);
    }

    public static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd('\\', '/');
}
