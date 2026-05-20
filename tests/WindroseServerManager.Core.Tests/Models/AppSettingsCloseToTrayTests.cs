using WindroseServerManager.Core.Models;
using Xunit;

namespace WindroseServerManager.Core.Tests.Models;

public class AppSettingsCloseToTrayTests
{
    [Fact]
    public void CloseToTray_DefaultsToFalse()
    {
        var s = new AppSettings();
        Assert.False(s.CloseToTray);
    }

    [Fact]
    public void CloseToTray_SerializeAndDeserialize()
    {
        var s = new AppSettings { CloseToTray = true };
        var json = System.Text.Json.JsonSerializer.Serialize(s);
        var des = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
        Assert.NotNull(des);
        Assert.True(des!.CloseToTray);
    }

    [Fact]
    public void CloseToTray_DefaultsPreserved_WhenMissingInJson()
    {
        var json = """{"AutoRestartOnCrash":true}""";
        var s = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
        Assert.NotNull(s);
        Assert.False(s!.CloseToTray);
    }
}
