namespace WindroseServerManager.App.Services;

public interface ISelfUpdateService
{
    Task PrepareAndLaunchUpdaterAsync(string zipPath, CancellationToken ct = default);
}
