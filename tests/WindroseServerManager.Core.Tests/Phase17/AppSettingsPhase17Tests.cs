using WindroseServerManager.Core.Models;
using Xunit;

namespace WindroseServerManager.Core.Tests.Phase17;

public class AppSettingsPhase17Tests
{
    [Fact]
    public void Skin_Default_IsClassic()
    {
        Assert.Equal("classic", new AppSettings().Skin);
    }

    [Fact]
    public void RestartInstallUpdateBeforeStart_Default_IsFalse()
    {
        Assert.False(new AppSettings().RestartInstallUpdateBeforeStart);
    }

    [Fact]
    public void RestartCreateBackupBeforeStart_Default_IsFalse()
    {
        Assert.False(new AppSettings().RestartCreateBackupBeforeStart);
    }

    [Fact]
    public void RestartBroadcastEnabled_Default_IsFalse()
    {
        Assert.False(new AppSettings().RestartBroadcastEnabled);
    }

    [Fact]
    public void RestartBroadcastMessage_Default_IsEmpty()
    {
        Assert.Equal("", new AppSettings().RestartBroadcastMessage);
    }

    [Fact]
    public void DesiredServerRunningByServer_Default_IsEmpty()
    {
        Assert.Empty(new AppSettings().DesiredServerRunningByServer);
    }

    [Fact]
    public void CliEndpointEnabled_Default_IsTrue()
    {
        Assert.True(new AppSettings().CliEndpointEnabled);
    }

    [Fact]
    public void CliPipeName_Default_IsWindroseServerManager()
    {
        Assert.Equal("WindroseServerManager", new AppSettings().CliPipeName);
    }

    [Fact]
    public void WindrosePlusHostByServer_Default_IsEmpty()
    {
        Assert.Empty(new AppSettings().WindrosePlusHostByServer);
    }

    [Fact]
    public void DiscordNotifyPlayerJoinLeave_Default_IsFalse()
    {
        Assert.False(new AppSettings().DiscordNotifyPlayerJoinLeave);
    }

    [Fact]
    public void RoundTrip_WindrosePlusHostByServer()
    {
        var s = new AppSettings();
        var dir = "C:\\servers\\my-server";
        s.WindrosePlusHostByServer[dir] = "192.168.1.100";
        var json = System.Text.Json.JsonSerializer.Serialize(s);
        var restored = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)!;
        Assert.Equal("192.168.1.100", restored.WindrosePlusHostByServer[dir]);
    }

    [Fact]
    public void RoundTrip_DesiredServerRunningByServer()
    {
        var s = new AppSettings();
        s.DesiredServerRunningByServer["C:\\servers\\s1"] = true;
        var json = System.Text.Json.JsonSerializer.Serialize(s);
        var restored = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)!;
        Assert.True(restored.DesiredServerRunningByServer["C:\\servers\\s1"]);
    }
}
