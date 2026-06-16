using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindroseServerManager.Core.Services;

namespace WindroseServerManager.App.Services;

public sealed class IpcPipeServer : BackgroundService
{
    private const string PipeName = "WindroseServerManager";
    private readonly ILogger<IpcPipeServer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public IpcPipeServer(ILogger<IpcPipeServer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IPC pipe server starting on \\\\.\\pipe\\{PipeName}", PipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);

                _ = HandleClientAsync(pipe, stoppingToken);
                pipe = null;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IPC pipe server error");
                pipe?.Dispose();
                try { await Task.Delay(500, stoppingToken); } catch { break; }
            }
        }

        _logger.LogInformation("IPC pipe server stopped");
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(pipe);
            var json = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return;

            var cmd = JsonSerializer.Deserialize<IpcCommand>(json);
            if (cmd is null) return;

            var response = await DispatchAsync(cmd.Command, ct).ConfigureAwait(false);
            var respBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response) + "\n");
            try
            {
                await pipe.WriteAsync(respBytes, ct).ConfigureAwait(false);
                await pipe.FlushAsync(ct).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                _logger.LogDebug(ex, "IPC write failed (client disconnected)");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IPC client handler error");
            try
            {
                var err = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new IpcResponse { Ok = false, Error = ex.Message }) + "\n");
                await pipe.WriteAsync(err, ct).ConfigureAwait(false);
            }
            catch { }
        }
        finally
        {
            pipe.Dispose();
        }
    }

    private async Task<IpcResponse> DispatchAsync(string? command, CancellationToken ct)
    {
        try
        {
            var server = _serviceProvider.GetRequiredService<IServerProcessService>();

            switch (command?.ToLowerInvariant())
            {
                case "start":
                    await server.StartAsync(ct).ConfigureAwait(false);
                    return new IpcResponse { Ok = true };

                case "stop":
                    await server.StopAsync(ct).ConfigureAwait(false);
                    return new IpcResponse { Ok = true };

                case "restart":
                    await server.StopAsync(ct).ConfigureAwait(false);
                    try
                    {
                        using var restartCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        restartCts.CancelAfter(TimeSpan.FromSeconds(15));
                        while (server.Status != Core.Models.ServerStatus.Stopped
                               && server.Status != Core.Models.ServerStatus.Crashed)
                        {
                            await Task.Delay(300, restartCts.Token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return new IpcResponse { Ok = false, Error = "Server did not stop within 15s" };
                    }
                    await server.StartAsync(ct).ConfigureAwait(false);
                    return new IpcResponse { Ok = true };

                case "backup":
                    var backup = _serviceProvider.GetRequiredService<IBackupService>();
                    var info = await backup.CreateBackupAsync(isAutomatic: false, ct).ConfigureAwait(false);
                    return new IpcResponse { Ok = info is not null, Data = info?.FileName };

                case "status":
                    return new IpcResponse
                    {
                        Ok = true,
                        Data = JsonSerializer.Serialize(new
                        {
                            running = server.Status == Core.Models.ServerStatus.Running,
                            status = server.Status.ToString(),
                            pid = server.ProcessId,
                            startedAtUtc = server.StartedAtUtc?.ToString("O"),
                        })
                    };

                case "shutdown":
                    await server.StopAsync(ct).ConfigureAwait(false);
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            desktop.Shutdown();
                        }
                    });
                    return new IpcResponse { Ok = true };

                default:
                    return new IpcResponse { Ok = false, Error = $"Unknown command: {command}" };
            }
        }
        catch (Exception ex)
        {
            return new IpcResponse { Ok = false, Error = ex.Message };
        }
    }
}

public sealed class IpcCommand
{
    public string? Command { get; set; }
}

public sealed class IpcResponse
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? Data { get; set; }
}
