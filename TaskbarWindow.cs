using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace TaskbarTimer;

/// The element hosted inside the Windows 11 taskbar (via Deskband11Lib).
public sealed class TaskbarWindow : Window
{
    public readonly Border Root = new();
    readonly TextBlock _t = new();
    DispatcherTimer? _flash; int _n; bool _light;

    public TaskbarWindow(TimerApp app)
    {
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize; Width = 104; Height = 40;
        _t.FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"); _t.FontSize = 15; _t.FontWeight = FontWeights.SemiBold;
        _t.HorizontalAlignment = HorizontalAlignment.Center; _t.VerticalAlignment = VerticalAlignment.Center;
        Root.Background = Brushes.Transparent; Root.Child = _t; Root.Cursor = Cursors.Hand;
        Content = Root;
        Root.MouseLeftButtonUp += (_, _) => app.TogglePopup();
    }

    Brush Base() => _light ? Brushes.Black : Brushes.White;

    public void SetText(string s, bool paused, bool light)
    {
        _light = light;
        _t.Text = paused ? "⏸ " + s : s;
        _t.Opacity = paused ? 0.7 : 1;
        if (_flash?.IsEnabled != true) _t.Foreground = Base();
    }

    public void Flash()
    {
        _n = 0;
        _flash ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _flash.Tick -= OnFlash; _flash.Tick += OnFlash; _flash.Start();
    }
    void OnFlash(object? s, EventArgs e)
    {
        _n++; _t.Foreground = _n % 2 == 1 ? Brushes.OrangeRed : Base();
        if (_n >= 8) { _flash!.Stop(); _t.Foreground = Base(); }
    }
}
