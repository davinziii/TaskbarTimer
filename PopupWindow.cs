using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
namespace TaskbarTimer;

public sealed class PopupWindow : Window
{
    static readonly Style Flat = (Style)XamlReader.Parse(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'><Setter.Value>
  <ControlTemplate TargetType='Button'>
   <Border x:Name='b' Background='#18808080' CornerRadius='6' Padding='10,6'>
    <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
   </Border>
   <ControlTemplate.Triggers>
    <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='b' Property='Background' Value='#38808080'/></Trigger>
    <Trigger Property='IsPressed' Value='True'><Setter TargetName='b' Property='Background' Value='#50808080'/></Trigger>
   </ControlTemplate.Triggers>
  </ControlTemplate></Setter.Value></Setter>
</Style>");

    readonly TimerApp _app;
    readonly TextBlock _time = new(), _sub = new();
    readonly Button _play = Btn("▶");
    readonly TextBox _custom = new() { Width = 56, Text = "25", VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(3) };

    static Button Btn(string c, Action? a = null)
    {
        var b = new Button { Content = c, Style = Flat, Margin = new Thickness(3), MinWidth = 40 };
        if (a != null) b.Click += (_, _) => a();
        return b;
    }
    static StackPanel Row(params UIElement[] c)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var x in c) p.Children.Add(x);
        return p;
    }

    public PopupWindow(TimerApp app)
    {
        _app = app; var e = app.Engine;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; Topmost = true;
        Width = 300; SizeToContent = SizeToContent.Height;
        FontFamily = new FontFamily("Segoe UI Variable, Segoe UI");
        _time.FontSize = 46; _time.FontWeight = FontWeights.Light; _time.HorizontalAlignment = HorizontalAlignment.Center;
        _sub.HorizontalAlignment = HorizontalAlignment.Center; _sub.Opacity = .6; _sub.FontSize = 12;
        _play.Click += (_, _) => app.ToggleRun();

        var presets = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
        foreach (var m in app.Cfg.Presets)
            presets.Children.Add(Btn(m >= 60 && m % 60 == 0 ? $"{m / 60}h" : $"{m}m", () => e.SetDuration(TimeSpan.FromMinutes(m))));

        var set = Btn("Set", () => { if (double.TryParse(_custom.Text, out var mins) && mins > 0) e.SetDuration(TimeSpan.FromMinutes(mins)); });
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(_time); panel.Children.Add(_sub);
        panel.Children.Add(new Border { Height = 8 });
        panel.Children.Add(Row(_play, Btn("↻", () => e.Reset()), Btn("−1m", () => e.Add(TimeSpan.FromMinutes(-1))), Btn("+1m", () => e.Add(TimeSpan.FromMinutes(1)))));
        panel.Children.Add(presets);
        panel.Children.Add(Row(_custom, new TextBlock { Text = "min", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) }, set));
        panel.Children.Add(Row(Btn("Countdown", () => e.SetMode(TimerMode.Countdown)), Btn("Stopwatch", () => e.SetMode(TimerMode.Stopwatch)), Btn("⚙", () => app.ShowSettings())));
        Content = panel;
        KeyDown += (_, k) => { if (k.Key == Key.Escape) Hide(); };
    }

    public void Refresh()
    {
        var e = _app.Engine;
        _time.Text = Fmt.Clock(e.Value, e.Mode);
        _play.Content = e.State == TimerState.Running ? "⏸" : "▶";
        _sub.Text = e.State switch
        {
            TimerState.Done => "Done",
            TimerState.Paused => "Paused",
            TimerState.Running => e.Mode == TimerMode.Countdown ? "Counting down" : "Running",
            _ => e.Mode == TimerMode.Countdown ? "Ready" : "Stopwatch"
        };
    }

    public void ShowAt()
    {
        var dark = _app.Dark;
        Background = new SolidColorBrush(dark ? Color.FromRgb(0x20, 0x20, 0x20) : Color.FromRgb(0xF3, 0xF3, 0xF3));
        Foreground = dark ? Brushes.White : Brushes.Black;
        BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 128, 128, 128)); BorderThickness = new Thickness(1);
        _custom.Foreground = Foreground; _custom.Background = new SolidColorBrush(Color.FromArgb(0x18, 128, 128, 128));
        Refresh(); Show(); UpdateLayout();
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 12; Top = wa.Bottom - ActualHeight - 12;
        Activate();
    }

    protected override void OnDeactivated(EventArgs e) { base.OnDeactivated(e); Hide(); _app.PopupHidden(); }
}
