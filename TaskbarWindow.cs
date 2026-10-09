using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace TaskbarTimer;

/// The element hosted inside the Windows 11 taskbar (via Deskband11Lib):  [ 24:37 ] [▶] [■] [↻] [+1m]
/// Height matches the 48 DIP taskbar so everything centers vertically.
public sealed class TaskbarWindow : Window
{
    public const double WidthDip = 206;
    public readonly Grid Root = new();
    readonly TextBlock _t = new();
    readonly List<TextBlock> _glyphs = new();
    Border _play = null!; TextBlock _stopGlyph = null!;

    static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    static readonly FontFamily Text = new("Segoe UI Variable, Segoe UI");
    static readonly Brush Red = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30)));
    static readonly Brush Hover = Frozen(new SolidColorBrush(Color.FromArgb(0x38, 128, 128, 128)));
    static Brush Frozen(Brush b) { b.Freeze(); return b; }

    public TaskbarWindow(TimerApp app)
    {
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize; Width = WidthDip; Height = 48;

        _t.FontFamily = Text; _t.FontSize = 15; _t.FontWeight = FontWeights.SemiBold;
        _t.HorizontalAlignment = HorizontalAlignment.Center; _t.VerticalAlignment = VerticalAlignment.Center;
        _t.Margin = new Thickness(0, 0, 0, 1);
        var time = new Border { Width = 82, Height = 48, Background = Brushes.Transparent, Child = _t, Cursor = Cursors.Hand, ToolTip = "Open timer" };
        time.MouseLeftButtonUp += (_, _) => app.TogglePopup();

        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(time);
        _play = Btn("\uE768", app.Play, "Play / resume", true, out _);
        row.Children.Add(_play);
        row.Children.Add(Btn("\uE71A", app.StopPress, "Stop (pause, or silence the alarm)", true, out _stopGlyph));
        row.Children.Add(Btn("\uE72C", () => app.Engine.Restart(), "Restart", true, out _));
        row.Children.Add(Btn("+1m", () => app.Engine.Add(TimeSpan.FromMinutes(1)), "Add one minute", false, out _));

        Root.Background = Brushes.Transparent; Root.Children.Add(row);
        Content = Root;
    }

    Border Btn(string content, Action act, string tip, bool icon, out TextBlock tb)
    {
        tb = new TextBlock
        {
            Text = content, FontFamily = icon ? Icons : Text, FontSize = icon ? 14 : 12,
            FontWeight = icon ? FontWeights.Normal : FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        };
        _glyphs.Add(tb);
        var b = new Border
        {
            Width = 28, Height = 32, CornerRadius = new CornerRadius(6), Margin = new Thickness(1, 0, 1, 0),
            Background = Brushes.Transparent, Child = tb, Cursor = Cursors.Hand, ToolTip = tip
        };
        b.MouseEnter += (_, _) => b.Background = Hover;
        b.MouseLeave += (_, _) => b.Background = Brushes.Transparent;
        b.MouseLeftButtonUp += (_, e) => { act(); e.Handled = true; };
        return b;
    }

    protected override void OnSourceInitialized(EventArgs e) { base.OnSourceInitialized(e); WinStyle.HideFromAltTab(this, true); }

    public void SetText(string s, bool paused, bool light, bool red, bool running, bool ringing)
    {
        var baseBrush = light ? Brushes.Black : Brushes.White;
        _t.Text = paused ? "⏸ " + s : s;
        _t.Opacity = paused ? 0.7 : 1;
        _t.Foreground = red ? Red : baseBrush;
        foreach (var g in _glyphs) g.Foreground = baseBrush;
        _stopGlyph.Foreground = ringing ? Red : baseBrush;
        _play.Opacity = running ? 0.4 : 1;
    }
}
