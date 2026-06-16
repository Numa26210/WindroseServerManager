using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WindroseServerManager.App.Services;

public sealed class AppUpdateService : IAppUpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/Numa26210/WindroseServerManager/releases/latest";
    private const string UserAgent = "WindroseServerManager-UpdateCheck";
    private static readonly string UpdateTempDir = Path.Combine(Path.GetTempPath(), "WindroseUpdate");

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AppUpdateService> _logger;

    public event Action<AppUpdateResult>? UpdateChecked;

    public AppUpdateService(IHttpClientFactory httpFactory, ILogger<AppUpdateService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<AppUpdateResult> CheckAsync(CancellationToken ct = default)
    {
        var result = await CheckInternalAsync(ct).ConfigureAwait(false);
        try { UpdateChecked?.Invoke(result); }
        catch (Exception ex) { _logger.LogWarning(ex, "UpdateChecked-Subscriber warf Exception"); }
        return result;
    }

    private async Task<AppUpdateResult> CheckInternalAsync(CancellationToken ct)
    {
        var current = GetCurrentVersion();
        try
        {
            var http = _httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(10);
            http.DefaultRequestHeaders.UserAgent.Clear();
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, current.ToString()));
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var resp = await http.GetAsync(ApiUrl, ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("GitHub releases endpoint returned 404 (noch kein Release veröffentlicht)");
                return new AppUpdateResult(false, current.ToString(), null, null, null, null, null,
                    Loc.Get("AppUpdate.NoReleasePublished"));
            }
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;
            var isDraft = root.TryGetProperty("draft", out var draftEl) && draftEl.GetBoolean();
            var isPrerelease = root.TryGetProperty("prerelease", out var preEl) && preEl.GetBoolean();

            if (isDraft || isPrerelease)
            {
                _logger.LogDebug("Skipping draft/prerelease {Tag}", tag);
                return new AppUpdateResult(false, current.ToString(), null, null, null, null, null,
                    Loc.Get("AppUpdate.NoStableVersion"));
            }

            if (string.IsNullOrWhiteSpace(tag) || !TryParseVersion(tag, out var latest))
            {
                _logger.LogWarning("Konnte Tag nicht parsen: {Tag}", tag);
                return new AppUpdateResult(false, current.ToString(), null, null, null, null, null,
                    Loc.Get("AppUpdate.ReleaseUnreadable"));
            }

            var latestStr = NormalizeVersion(latest);
            var (downloadUrl, portableZipUrl, portableZipDigest) = ExtractInstallerAssets(root);
            var hasUpdate = latest > current;

            var msg = hasUpdate
                ? Loc.Format("Update.Banner.AvailableFormat", latestStr)
                : Loc.Format("AppUpdate.UpToDateFormat", current);

            return new AppUpdateResult(hasUpdate, current.ToString(), latestStr, htmlUrl, downloadUrl, portableZipUrl, portableZipDigest, msg);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "App-Update-Check fehlgeschlagen");
            return new AppUpdateResult(false, current.ToString(), null, null, null, null, null,
                Loc.Get("AppUpdate.CheckUnreachable"));
        }
    }

    private static Version GetCurrentVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info) && TryParseVersion(info, out var parsed))
            return parsed;

        return asm.GetName().Version ?? new Version(1, 2, 5, 0);
    }

    private static bool TryParseVersion(string raw, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = raw.Trim();
        if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);

        var cutoff = s.IndexOfAny(new[] { '-', '+' });
        if (cutoff > 0) s = s.Substring(0, cutoff);

        return Version.TryParse(s, out version!);
    }

    private static string NormalizeVersion(Version v)
    {
        if (v.Revision > 0) return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        if (v.Build > 0) return $"{v.Major}.{v.Minor}.{v.Build}";
        return $"{v.Major}.{v.Minor}";
    }

    private static (string? DownloadUrl, string? PortableZipUrl, string? PortableZipDigest) ExtractInstallerAssets(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return (null, null, null);

        string? zipUrl = null;
        string? exeUrl = null;
        string? anyDownload = null;
        string? zipDigest = null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
            if (string.IsNullOrWhiteSpace(url)) continue;

            anyDownload ??= url;

            if (!string.IsNullOrWhiteSpace(name) &&
                name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                name.Contains("win-x64", StringComparison.OrdinalIgnoreCase))
            {
                zipUrl = url;
                if (asset.TryGetProperty("digest", out var digestEl))
                {
                    var dig = digestEl.ValueKind == JsonValueKind.Object
                        ? digestEl.TryGetProperty("sha256", out var shaEl) ? shaEl.GetString() : null
                        : digestEl.GetString();
                    zipDigest = dig;
                }
            }

            if (!string.IsNullOrWhiteSpace(name) &&
                name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                name.Contains("setup", StringComparison.OrdinalIgnoreCase))
            {
                exeUrl = url;
            }
        }

        return (zipUrl ?? exeUrl ?? anyDownload, zipUrl, zipDigest);
    }

    public async Task<string?> DownloadUpdateAsync(string zipUrl, IProgress<int> progress, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(UpdateTempDir);
            var fileName = "WindroseServerManager-update.zip";
            var destPath = Path.Combine(UpdateTempDir, fileName);
            var tmpPath = destPath + ".download";

            if (File.Exists(destPath)) File.Delete(destPath);
            if (File.Exists(tmpPath)) File.Delete(tmpPath);

            var http = _httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromMinutes(10);

            using var resp = await http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var totalBytes = resp.Content.Headers.ContentLength ?? -1L;

            await using var contentStream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var pct = (int)(totalRead * 100 / totalBytes);
                    progress.Report(pct);
                }
            }

            await fileStream.FlushAsync(ct).ConfigureAwait(false);
            fileStream.Close();

            File.Move(tmpPath, destPath, overwrite: true);

            progress.Report(100);
            return destPath;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DownloadUpdateAsync failed");
            return null;
        }
    }
}