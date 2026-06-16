using Avalonia;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using WindroseServerManager.Core.Services;

namespace WindroseServerManager.App.Services;

public sealed class AppSkinService : IAppSkinService
{
    private readonly ILogger<AppSkinService> _logger;

    public IReadOnlyList<AppSkinDefinition> AvailableSkins { get; } = new List<AppSkinDefinition>
    {
        new() { Key = "classic", DisplayName = "Classic" },
        new() { Key = "stitch", DisplayName = "Stitch" },
    };

    public string CurrentSkinKey { get; private set; } = "classic";

    public AppSkinService(ILogger<AppSkinService> logger)
    {
        _logger = logger;
    }

    public void Initialize(string skinKey) => SetSkin(skinKey);

    public void SetSkin(string key)
    {
        if (!AvailableSkins.Any(s => s.Key == key))
        {
            _logger.LogWarning("Unknown skin '{Key}', falling back to 'classic'", key);
            key = "classic";
        }

        CurrentSkinKey = key;
        Apply(key);
    }

    private void Apply(string key)
    {
        var app = Application.Current;
        if (app is null) return;

        var resources = app.Resources;

        Color surface, surfaceAlt, mica, border, accent, accentHover, textPrimary, textSecondary;

        switch (key)
        {
            case "stitch":
                SetColor(resources, "BrandPrimaryColor", "#FF2D2D2D");
                SetColor(resources, "BrandSecondaryColor", "#FF404040");
                SetColor(resources, "BrandAccentColor", "#FF00B4D8");
                SetColor(resources, "BrandSuccessColor", "#FF2ECC71");
                SetColor(resources, "BrandWarningColor", "#FFF39C12");
                SetColor(resources, "BrandErrorColor", "#FFE74C3C");
                SetColor(resources, "BrandInfoColor", "#FF3498DB");
                SetColor(resources, "SystemAccentColor", "#FF00B4D8");
                surface = Color.Parse("#FF2D2D2D");
                surfaceAlt = Color.Parse("#FF404040");
                mica = Color.Parse("#FF2D2D2D");
                border = Color.Parse("#FF1A1A1A");
                accent = Color.Parse("#FF00B4D8");
                accentHover = Color.Parse("#FF00D4FF");
                textPrimary = Color.Parse("#FFE0E0E0");
                textSecondary = Color.Parse("#FF999999");
                break;
            default:
                SetColor(resources, "BrandPrimaryColor", "#FF1A1A2E");
                SetColor(resources, "BrandSecondaryColor", "#FF16213E");
                SetColor(resources, "BrandAccentColor", "#FFFFB703");
                SetColor(resources, "BrandSuccessColor", "#FF2ECC71");
                SetColor(resources, "BrandWarningColor", "#FFF39C12");
                SetColor(resources, "BrandErrorColor", "#FFE74C3C");
                SetColor(resources, "BrandInfoColor", "#FF3498DB");
                SetColor(resources, "SystemAccentColor", "#FFFFB703");
                surface = Color.Parse("#FF1A1A2E");
                surfaceAlt = Color.Parse("#FF16213E");
                mica = Color.Parse("#FF1A1A2E");
                border = Color.Parse("#FF0D0D17");
                accent = Color.Parse("#FFFFB703");
                accentHover = Color.Parse("#FFFFC233");
                textPrimary = Color.Parse("#FFF5E6D3");
                textSecondary = Color.Parse("#FF8B7D6B");
                break;
        }

        SetBrush(resources, "BrandNavySurfaceBrush", surface);
        SetBrush(resources, "BrandNavySurfaceAltBrush", surfaceAlt);
        SetBrush(resources, "BrandNavyMicaBrush", mica);
        SetBrush(resources, "BrandNavyBorderBrush", border);
        SetBrush(resources, "BrandAmberBrush", accent);
        SetBrush(resources, "BrandAmberHoverBrush", accentHover);
        SetBrush(resources, "BrandTextPrimaryBrush", textPrimary);
        SetBrush(resources, "BrandTextSecondaryBrush", textSecondary);

        _logger.LogInformation("Skin applied: {Key}", key);
    }

    private static void SetBrush(Avalonia.Controls.IResourceDictionary resources, string key, Color color)
    {
        resources[key] = new SolidColorBrush(color);
    }

    private static void SetColor(Avalonia.Controls.IResourceDictionary resources, string key, string hex)
    {
        var color = Color.Parse(hex);
        resources[key] = color;

        var brushKey = key.Replace("Color", "Brush");
        if (brushKey != key)
            resources[brushKey] = new SolidColorBrush(color);

        if (key == "SystemAccentColor")
        {
            var lighter = Color.FromRgb(
                (byte)Math.Min(255, color.R + 40),
                (byte)Math.Min(255, color.G + 40),
                (byte)Math.Min(255, color.B + 40));
            var darker = Color.FromRgb(
                (byte)Math.Max(0, color.R - 40),
                (byte)Math.Max(0, color.G - 40),
                (byte)Math.Max(0, color.B - 40));
            resources["SystemAccentColorDark1"] = darker;
            resources["SystemAccentColorDark2"] = darker;
            resources["SystemAccentColorLight1"] = lighter;
            resources["SystemAccentColorLight2"] = lighter;
        }
    }
}
