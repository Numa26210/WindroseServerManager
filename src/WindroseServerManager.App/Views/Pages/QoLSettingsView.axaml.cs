using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WindroseServerManager.App.ViewModels;

namespace WindroseServerManager.App.Views.Pages;

public partial class QoLSettingsView : UserControl
{
    public QoLSettingsView() { AvaloniaXamlLoader.Load(this); }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as QoLSettingsViewModel)?.Start();
    }
}
