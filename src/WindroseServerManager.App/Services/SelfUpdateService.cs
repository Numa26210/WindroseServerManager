using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace WindroseServerManager.App.Services;

public sealed class SelfUpdateService : ISelfUpdateService
{
    private static readonly string UpdateTempDir = Path.Combine(Path.GetTempPath(), "WindroseUpdate");
    private readonly ILogger<SelfUpdateService> _logger;

    public SelfUpdateService(ILogger<SelfUpdateService> logger)
    {
        _logger = logger;
    }

    public async Task PrepareAndLaunchUpdaterAsync(string zipPath, CancellationToken ct = default)
    {
        var extractDir = Path.Combine(UpdateTempDir, "extracted");
        if (Directory.Exists(extractDir))
        {
            try { Directory.Delete(extractDir, true); } catch { }
        }
        Directory.CreateDirectory(extractDir);

        await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true), ct).ConfigureAwait(false);

        var sourceExe = FindExe(extractDir);
        if (sourceExe is null)
            throw new InvalidOperationException("No exe found in the update ZIP.");

        var currentExe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine current exe path.");

        var targetExe = currentExe;

        var pid = Environment.ProcessId;
        var scriptPath = Path.Combine(UpdateTempDir, "update.ps1");

        var script = $@"$proc = Get-Process -Id {pid} -ErrorAction SilentlyContinue
if ($proc) {{ $proc.WaitForExit(30000) }}
Start-Sleep -Milliseconds 500
Copy-Item -Path ""{sourceExe}"" -Destination ""{targetExe}"" -Force
Start-Process -FilePath ""{targetExe}""
";

        await File.WriteAllTextAsync(scriptPath, script, ct).ConfigureAwait(false);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        Process.Start(psi);

        _logger.LogInformation("Updater script launched. PID={Pid}, Source={Source}, Target={Target}", pid, sourceExe, targetExe);
    }

    private static string? FindExe(string dir)
    {
        var exes = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories);
        if (exes.Length == 1) return exes[0];

        var name = "WindroseServerManager";
        var match = exes.FirstOrDefault(e => Path.GetFileNameWithoutExtension(e)
            .Equals(name, StringComparison.OrdinalIgnoreCase));
        return match ?? exes.FirstOrDefault();
    }
}
