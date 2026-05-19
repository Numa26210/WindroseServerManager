using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Runtime.InteropServices;

namespace WindroseServerManager.App.Views;

public partial class MainWindow : Window
{
    private Border? _rootBorder;
    private static readonly CornerRadius NormalCorner = new(10);
    private static readonly Thickness NormalBorder = new(1);

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel);
        Title = $"Windrose Server Manager {GetType().Assembly.GetName().Version?.ToString(3)}";

        Opened += (_, _) =>
        {
            _rootBorder = this.FindControl<Border>("RootBorder");
            ApplyStateVisuals();
            TryApplyDwmRoundedCorners();
        };
    }

    // Windows 11: DwmSetWindowAttribute für native Runde-Ecken, ohne dass das Window transparent sein muss.
    // Greenshot & PrintWindow funktionieren damit weiter.
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private void TryApplyDwmRoundedCorners()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        try
        {
            var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero) return;
            int pref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
        }
        catch { /* nicht kritisch */ }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty)
            ApplyStateVisuals();
    }

    private void ApplyStateVisuals()
    {
        if (_rootBorder is null) return;
        if (WindowState == WindowState.Maximized)
        {
            _rootBorder.CornerRadius = new CornerRadius(0);
            _rootBorder.BorderThickness = new Thickness(0);
            // OffScreenMargin: bei Windows sind Teile vom Maximized-Fenster außerhalb des Screens.
            Padding = OffScreenMargin;
        }
        else
        {
            _rootBorder.CornerRadius = NormalCorner;
            _rootBorder.BorderThickness = NormalBorder;
            Padding = new Thickness(0);
        }
    }

    private void OnMinimize(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnToggleMaximize(object? sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object? sender, RoutedEventArgs e) => Close();

    private void OnDragZoneDoubleTapped(object? sender, TappedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnToastTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: Services.ToastItem item })
            item.Dismiss();
    }

    private const int ResizeBorderThickness = 6;

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var pos = e.GetPosition(this);
        bool inTopBar = pos.Y <= 38;

        if (e.Source is Control source && IsInteractive(source)) return;

        // Edge/corner resize hit-testing (only in normal window state)
        if (WindowState == WindowState.Normal)
        {
            var edge = HitTestResizeEdge(pos);
            if (edge is not null)
            {
                BeginResizeDrag(edge.Value, e);
                return;
            }
        }

        if (inTopBar)
            BeginMoveDrag(e);
    }

    private WindowEdge? HitTestResizeEdge(Point pos)
    {
        double x = pos.X, y = pos.Y;
        double w = Bounds.Width, h = Bounds.Height;
        int t = ResizeBorderThickness;

        bool left   = x <= t;
        bool right  = x >= w - t;
        bool top    = y <= t;
        bool bottom = y >= h - t;

        if (top    && left)  return WindowEdge.NorthWest;
        if (top    && right) return WindowEdge.NorthEast;
        if (bottom && left)  return WindowEdge.SouthWest;
        if (bottom && right) return WindowEdge.SouthEast;
        if (top)    return WindowEdge.North;
        if (bottom) return WindowEdge.South;
        if (left)   return WindowEdge.West;
        if (right)  return WindowEdge.East;

        return null;
    }

    private static bool IsInteractive(Control c)
    {
        Visual? cur = c;
        while (cur is not null)
        {
            if (cur is Button or ListBoxItem or TextBox or ComboBox
                or NumericUpDown or Slider or TimePicker)
                return true;
            cur = cur.GetVisualParent();
        }
        return false;
    }
}
