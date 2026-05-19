using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WindroseServerManager.Core.Models;
using WindroseServerManager.Core.Services;
using WindroseServerManager.Core.Tests.Fixtures;
using WindroseServerManager.Core.Tests.TestDoubles;
using Xunit;

namespace WindroseServerManager.Core.Tests.Services;

/// <summary>
/// Tests for v1.6.0 features: Windrose+ per-server toggle and version pinning.
/// </summary>
public class WindrosePlusToggleAndVersionTests
{
    // ---------- ServerEntry model ----------

    [Fact]
    public void ServerEntry_IsWindrosePlusEnabled_DefaultsToTrue()
    {
        var entry = new ServerEntry();
        Assert.True(entry.IsWindrosePlusEnabled);
    }

    [Fact]
    public void ServerEntry_PinnedWindrosePlusVersion_DefaultsToNull()
    {
        var entry = new ServerEntry();
        Assert.Null(entry.PinnedWindrosePlusVersion);
    }

    // ---------- FetchByTagAsync ----------

    [Fact]
    public async Task FetchByTagAsync_ParsesTagAndDigest()
    {
        using var fixture = new TempServerFixture();
        var github = new FakeGithubReleaseServer();
        var svc = CreateService(fixture, github.CreateHandler());
        var release = await svc.FetchByTagAsync("v1.0.6", CancellationToken.None);
        Assert.Equal("v1.0.6", release.Tag);
        Assert.NotNull(release.DigestSha256);
    }

    [Fact]
    public async Task FetchByTagAsync_ThrowsOnEmptyTag()
    {
        using var fixture = new TempServerFixture();
        var svc = CreateService(fixture, FakeHttpMessageHandler.ThrowsOffline());
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.FetchByTagAsync("", CancellationToken.None));
    }

    [Fact]
    public async Task FetchByTagAsync_ThrowsOffline_WhenNoCache()
    {
        using var fixture = new TempServerFixture();
        var svc = CreateService(fixture, FakeHttpMessageHandler.ThrowsOffline());
        await Assert.ThrowsAsync<WindrosePlusOfflineException>(
            () => svc.FetchByTagAsync("v1.0.5", CancellationToken.None));
    }

    // ---------- ServerEntry toggle integration ----------

    [Fact]
    public void ServerEntry_ToggleFalse_DisablesWindrosePlus()
    {
        var entry = new ServerEntry { IsWindrosePlusEnabled = false };
        Assert.False(entry.IsWindrosePlusEnabled);
    }

    [Fact]
    public void ServerEntry_VersionPin_SetsTag()
    {
        var entry = new ServerEntry { PinnedWindrosePlusVersion = "v1.3.0" };
        Assert.Equal("v1.3.0", entry.PinnedWindrosePlusVersion);
    }

    // ---------- JSON round-trip ----------

    [Fact]
    public void ServerEntry_SerializesNewProperties()
    {
        var entry = new ServerEntry
        {
            Id = "test",
            Name = "Test Server",
            InstallDir = "C:\\server",
            IsWindrosePlusEnabled = false,
            PinnedWindrosePlusVersion = "v1.2.0"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(entry);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ServerEntry>(json);

        Assert.NotNull(deserialized);
        Assert.False(deserialized!.IsWindrosePlusEnabled);
        Assert.Equal("v1.2.0", deserialized.PinnedWindrosePlusVersion);
    }

    [Fact]
    public void ServerEntry_DefaultsPreserved_WhenJsonMissingNewFields()
    {
        // Simulates loading a settings file from before v1.6.0 (no IsWindrosePlusEnabled/PinnedWindrosePlusVersion)
        var json = """{"Id":"s1","Name":"Old","InstallDir":"C:\\old","AutoStartOnAppLaunch":false}""";
        var entry = System.Text.Json.JsonSerializer.Deserialize<ServerEntry>(json);

        Assert.NotNull(entry);
        Assert.True(entry!.IsWindrosePlusEnabled); // default true for backward compat
        Assert.Null(entry.PinnedWindrosePlusVersion); // default null = latest
    }

    // ---- Helpers ----

    private static WindrosePlusService CreateService(
        TempServerFixture fixture,
        FakeHttpMessageHandler handler)
    {
        var factory = new FakeHttpClientFactory(handler);
        return new WindrosePlusService(
            NullLogger<WindrosePlusService>.Instance,
            factory,
            NullAppSettingsService.Instance,
            fixture.CacheDir);
    }
}
