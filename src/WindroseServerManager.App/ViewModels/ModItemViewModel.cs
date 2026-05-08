using CommunityToolkit.Mvvm.ComponentModel;
using WindroseServerManager.Core.Models;

namespace WindroseServerManager.App.ViewModels;

public partial class ModItemViewModel : ObservableObject
{
    public ModInfo Info { get; private set; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string? _conflictWarning;

    public string FileName => Info.FileName;
    public string DisplayName => Info.DisplayName;
    public long SizeBytes => Info.SizeBytes;
    public bool IsEnabled => Info.IsEnabled;
    public IReadOnlyList<string> CompanionFiles => Info.CompanionFiles;
    public ModMeta? NexusMeta => Info.NexusMeta;
    public bool HasNexusLink => Info.NexusMeta is not null;
    public bool HasConflict => !string.IsNullOrEmpty(ConflictWarning);

    public ModItemViewModel(ModInfo info)
    {
        Info = info;
    }

    partial void OnConflictWarningChanged(string? value)
        => OnPropertyChanged(nameof(HasConflict));

    /// <summary>Ersetzt die zugrundeliegende ModInfo (z.B. nach Verlinkung neu geladen).</summary>
    public void UpdateInfo(ModInfo info)
    {
        Info = info;
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(NexusMeta));
        OnPropertyChanged(nameof(HasNexusLink));
    }
}
