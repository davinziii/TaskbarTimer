using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
namespace TaskbarTimer;

public sealed class PopupWindow : Window
{
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int v, int size);

    sealed record Palette(Color Bg, Color Surface, Color Fg, Color Sub, Color Accent, Color AccentFg, Color Danger);
    static Color H(string s) => (Color)ColorConverter.ConvertFromString(s);
    static readonly Palette PDark = new(H("#202020"), H("#2E2E2E"), H("#FFFFFF"), H("#A6A6A6"), H("#4CC2FF"), H("#000000"), H("#FF5A4F"));
    static readonly Palette PLight = new(H("#F7F7F7"), H("#E6E6E6"), H("#1A1A1A"), H("#666666"), H("#0067C0"), H("#FFFFFF"), H("#D13438"));
    static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    static readonly FontFamily TextFont = new("Segoe UI Variable, Segoe UI");

    // Every colour is bound explicitly (Background/Foreground/Tag=hover) so nothing depends on inherited or system colours.
    static readonly Style Flat = (Style)XamlReader.Parse(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'><Setter.Value>
  <ControlTemplate TargetType='Button'>
   <Border x:Name='b' Background='{TemplateBinding Background}' CornerRadius='8'>
    <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'
        TextElement.Foreground='{TemplateBinding Foreground}' TextElement.FontFamily='{TemplateBinding FontFamily}' TextElement.FontSize='{TemplateBinding FontSize}'/>
   </Border>
   <ControlTemplate.Triggers>
    <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='b' Property='Background' Value='{Binding Tag, RelativeSource={RelativeSource TemplatedParent}}'/></Trigger>
   </ControlTemplate.Triggers>
  </ControlTemplate></Setter.Value></Setter>
</Style>");

    readonly TimerApp _app; Palette P = PDark; string _lastCustom = "25";
    TextBlock _time = null!, _sub = null!; Button _play = null!, _stop = null!, _modeCd = null!, _modeSw = null!; TextBox _custom = null!;

    static SolidColorBrush Br(Color c) => new(c);
    static Color Mix(Color a, Color b, double t) =>
        Color.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    public PopupWindow(TimerApp app)
    {
        _app = app;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; Topmost = true;
        Width = 320; SizeToContent = SizeToContent.Height; FontFamily = TextFont;
        KeyDown += (_, k) => { if (k.Key == Key.Escape && !_app.Ringing) Hide(); };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        int round = 2; DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref round, 4); // Win11 rounded corners
    }

    // kind: 0 text button, 1 icon button, 2 small chip
    Button Mk(string content, Action act, Color bg, Color fg, int kind = 0, string? tip = null)
    {
        var b = new Button
        {
            Content = content, Style = Flat, Background = Br(bg), Foreground = Br(fg), Tag = Br(Mix(bg, fg, .15)),
            FontFamily = kind == 1 ? IconFont : TextFont, FontSize = kind == 1 ? 18 : kind == 2 ? 12 : 14,
            Height = kind == 1 ? 46 : kind == 2 ? 28 : 34, Margin = new Thickness(kind == 2 ? 2 : 3), ToolTip = tip,
            MinWidth = kind == 2 ? 44 : 0
        };
        b.Click += (_, _) => act();
        return b;
    }

    UIElement Build()
    {
        var e = _app.Engine;
        var root = new StackPanel { Margin = new Thickness(18) };
        _time = new TextBlock { FontSize = 54, FontWeight = FontWeights.Light, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI") };
        _sub = new TextBlock { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 14) };
        root.Children.Add(_time); root.Children.Add(_sub);

        _play = Mk("\uE768", _app.Play, P.Accent, P.AccentFg, 1, "Play / resume");
        _stop = Mk("\uE71A", _app.StopPress, P.Surface, P.Fg, 1, "Stop: pause the timer, or silence the alarm");
        var controls = new UniformGrid { Columns = 4 };
        controls.Children.Add(_play); controls.Children.Add(_stop);
        controls.Children.Add(Mk("\uE72C", () => e.Restart(), P.Surface, P.Fg, 1, "Restart from the beginning"));
        controls.Children.Add(Mk("+1 min", () => e.Add(TimeSpan.FromMinutes(1)), P.Surface, P.Fg, 0, "Add one minute"));
        foreach (Button b in controls.Children) if (b.Content as string == "+1 min") b.Height = 46;
        root.Children.Add(controls);

        var presets = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 6) };
        foreach (var m in _app.Cfg.Presets)
            presets.Children.Add(Mk(m >= 60 && m % 60 == 0 ? $"{m / 60}h" : $"{m}m", () => e.SetDuration(TimeSpan.FromMinutes(m)), P.Surface, P.Fg, 2));
        root.Children.Add(presets);

        _custom = new TextBox
        {
            Text = _lastCustom, Width = 64, Height = 30, Margin = new Thickness(3), VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center, Background = Br(P.Surface), Foreground = Br(P.Fg),
            CaretBrush = Br(P.Fg), BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(0)
        };
        var left = new StackPanel { Orientation = Orientation.Horizontal };
        left.Children.Add(_custom);
        left.Children.Add(new TextBlock { Text = "min", Foreground = Br(P.Sub), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 8, 0) });
        left.Children.Add(Mk("Set", () => { if (double.TryParse(_custom.Text, out var mins) && mins > 0) e.SetDuration(TimeSpan.FromMinutes(mins)); }, P.Surface, P.Fg, 2));
        var gear = Mk("\uE713", _app.ShowSettings, P.Bg, P.Sub, 1, "Settings"); gear.Height = 30; gear.Width = 36;
        var row = new DockPanel { Margin = new Thickness(0, 4, 0, 8) };
        DockPanel.SetDock(gear, Dock.Right); row.Children.Add(gear); row.Children.Add(left);
        root.Children.Add(row);

        _modeCd = Mk("Countdown", () => e.SetMode(TimerMode.Countdown), P.Bg, P.Fg, 2);
        _modeSw = Mk("Stopwatch", () => e.SetMode(TimerMode.Stopwatch), P.Bg, P.Fg, 2);
        var modes = new UniformGrid { Columns = 2 };
        modes.Children.Add(_modeCd); modes.Children.Add(_modeSw);
        root.Children.Add(modes);
        return root;
    }

    public void Refresh(bool red)
    {
        if (_time == null) return;
        var e = _app.Engine; bool ringing = _app.Ringing;
        _time.Text = Fmt.Clock(e.Value, e.Mode);
        _time.Foreground = Br(red ? P.Danger : P.Fg);
        _sub.Foreground = Br(ringing ? P.Danger : P.Sub);
        _sub.Text = ringing ? "Time's up — press Stop" : e.State switch
        {
            TimerState.Done => "Done",
            TimerState.Paused => "Paused",
            TimerState.Running => e.Mode == TimerMode.Countdown ? "Counting down" : "Running",
            _ => e.Mode == TimerMode.Countdown ? "Ready" : "Stopwatch"
        };
        _play.Opacity = e.State == TimerState.Running ? 0.45 : 1;
        var sb = ringing ? P.Danger : P.Surface; var sf = ringing ? Colors.White : P.Fg;
        _stop.Background = Br(sb); _stop.Foreground = Br(sf); _stop.Tag = Br(Mix(sb, sf, .15));
        foreach (var (b, sel) in new[] { (_modeCd, e.Mode == TimerMode.Countdown), (_modeSw, e.Mode == TimerMode.Stopwatch) })
        { var c = sel ? Mix(P.Bg, P.Fg, .14) : P.Bg; b.Background = Br(c); b.Tag = Br(Mix(c, P.Fg, .1)); }
    }

    public void ShowAt()
    {
        if (IsVisible) { Activate(); return; }
        P = _app.Dark ? PDark : PLight;
        Background = Br(P.Bg); BorderBrush = Br(Mix(P.Bg, P.Fg, .2)); BorderThickness = new Thickness(1);
        if (_custom != null) _lastCustom = _custom.Text;
        Content = Build();
        Refresh(_app.RedNow);
        Show(); UpdateLayout();
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 12; Top = wa.Bottom - ActualHeight - 12;
        Activate();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (_app.Ringing) return; // stays up until the alarm is stopped
        Hide(); _app.PopupHidden();
    }
}
