using WindroseServerManager.Core.Models;
using Xunit;

namespace WindroseServerManager.Core.Tests.Services;

public class PerServerFolderTests
{
    [Fact]
    public void ServerEntry_BackupDirOverride_DefaultsToNull()
    {
        var entry = new ServerEntry();
        Assert.Null(entry.BackupDirOverride);
    }

    [Fact]
    public void ServerEntry_ModsDirOverride_DefaultsToNull()
    {
        var entry = new ServerEntry();
        Assert.Null(entry.ModsDirOverride);
    }

    [Fact]
    public void ServerEntry_SerializeOverrideDirs()
    {
        var entry = new ServerEntry
        {
            Id = "s1",
            Name = "Test",
            InstallDir = "C:\\srv",
            BackupDirOverride = "D:\\backups",
            ModsDirOverride = "D:\\mods"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(entry);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ServerEntry>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("D:\\backups", deserialized!.BackupDirOverride);
        Assert.Equal("D:\\mods", deserialized.ModsDirOverride);
    }

    [Fact]
    public void ServerEntry_DefaultsPreserved_WhenJsonMissingOverrides()
    {
        var json = """{"Id":"s1","Name":"Old","InstallDir":"C:\\old"}""";
        var entry = System.Text.Json.JsonSerializer.Deserialize<ServerEntry>(json);

        Assert.NotNull(entry);
        Assert.Null(entry!.BackupDirOverride);
        Assert.Null(entry.ModsDirOverride);
    }
}
