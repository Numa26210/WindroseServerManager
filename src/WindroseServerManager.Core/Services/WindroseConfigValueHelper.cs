using System.Text.Json;

namespace WindroseServerManager.Core.Services;

public static class WindroseConfigValueHelper
{
    public static string? TryReadString(object? value)
    {
        if (value is null) return null;
        if (value is JsonElement je)
            return je.ValueKind == JsonValueKind.String ? je.GetString() : je.GetRawText();
        return value as string ?? value.ToString();
    }

    public static bool TryReadInt(object? value, out int result)
    {
        result = 0;
        if (value is null) return false;
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number)
                return je.TryGetInt32(out result);
            if (je.ValueKind == JsonValueKind.String)
                return int.TryParse(je.GetString(), out result);
            return false;
        }
        if (value is int i) { result = i; return true; }
        if (value is long l && l >= int.MinValue && l <= int.MaxValue) { result = (int)l; return true; }
        return int.TryParse(value.ToString(), out result);
    }
}
