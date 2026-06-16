using System.Text.Json;
using WindroseServerManager.Core.Services;
using Xunit;

namespace WindroseServerManager.Core.Tests.Phase17;

public class WindroseConfigValueHelperTests
{
    [Fact]
    public void TryReadString_Null_ReturnsNull()
    {
        Assert.Null(WindroseConfigValueHelper.TryReadString(null));
    }

    [Fact]
    public void TryReadString_StringValue_ReturnsString()
    {
        Assert.Equal("hello", WindroseConfigValueHelper.TryReadString("hello"));
    }

    [Fact]
    public void TryReadString_JsonElementString_ReturnsString()
    {
        var je = JsonSerializer.Deserialize<JsonElement>("\"world\"");
        Assert.Equal("world", WindroseConfigValueHelper.TryReadString(je));
    }

    [Fact]
    public void TryReadString_JsonElementNumber_ReturnsRawText()
    {
        var je = JsonSerializer.Deserialize<JsonElement>("42");
        Assert.Equal("42", WindroseConfigValueHelper.TryReadString(je));
    }

    [Fact]
    public void TryReadInt_Null_ReturnsFalse()
    {
        Assert.False(WindroseConfigValueHelper.TryReadInt(null, out _));
    }

    [Fact]
    public void TryReadInt_JsonElementNumber_ReturnsTrueAndValue()
    {
        var je = JsonSerializer.Deserialize<JsonElement>("8780");
        Assert.True(WindroseConfigValueHelper.TryReadInt(je, out var val));
        Assert.Equal(8780, val);
    }

    [Fact]
    public void TryReadInt_JsonElementStringNumber_ReturnsTrueAndValue()
    {
        var je = JsonSerializer.Deserialize<JsonElement>("\"8780\"");
        Assert.True(WindroseConfigValueHelper.TryReadInt(je, out var val));
        Assert.Equal(8780, val);
    }

    [Fact]
    public void TryReadInt_JsonElementStringInvalid_ReturnsFalse()
    {
        var je = JsonSerializer.Deserialize<JsonElement>("\"abc\"");
        Assert.False(WindroseConfigValueHelper.TryReadInt(je, out _));
    }

    [Fact]
    public void TryReadInt_IntValue_ReturnsTrueAndValue()
    {
        Assert.True(WindroseConfigValueHelper.TryReadInt(42, out var val));
        Assert.Equal(42, val);
    }

    [Fact]
    public void TryReadInt_LongInRange_ReturnsTrueAndInt()
    {
        Assert.True(WindroseConfigValueHelper.TryReadInt(1_000_000L, out var val));
        Assert.Equal(1_000_000, val);
    }

    [Fact]
    public void TryReadInt_LongTooLarge_ReturnsFalse()
    {
        Assert.False(WindroseConfigValueHelper.TryReadInt(3_000_000_000L, out _));
    }

    [Fact]
    public void TryReadInt_StringParsable_ReturnsTrueAndValue()
    {
        Assert.True(WindroseConfigValueHelper.TryReadInt("123", out var val));
        Assert.Equal(123, val);
    }

    [Fact]
    public void TryReadInt_StringNotParsable_ReturnsFalse()
    {
        Assert.False(WindroseConfigValueHelper.TryReadInt("not-a-number", out _));
    }
}
