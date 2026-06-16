namespace WindroseServerManager.Core.Services;

public sealed class AppSkinDefinition
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
}

public interface IAppSkinService
{
    IReadOnlyList<AppSkinDefinition> AvailableSkins { get; }
    void Initialize(string skinKey);
    void SetSkin(string key);
    string CurrentSkinKey { get; }
}
