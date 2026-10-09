using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace TaskbarTimer;

/// The element hosted inside the Windows 11 taskbar (via Deskband11Lib). Height matches the 48 DIP taskbar so text centers.
public sealed class TaskbarWindow : Window
{
    public readonly Grid Root = new();
    readonly TextBlock _t = new();
    static readonly Brush Red = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30)));
    static Brush Frozen(Brush b) { b.Freeze(); return b; }

    public TaskbarWindow(TimerApp app)
    {
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize; Width = 104; Height = 48;
        _t.FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"); _t.FontSize = 15; _t.FontWeight = FontWeights.SemiBold;
        _t.HorizontalAlignment = HorizontalAlignment.Center; _t.VerticalAlignment = VerticalAlignment.Center;
        _t.Margin = new Thickness(0, 0, 0, 1);
        Root.Background = Brushes.Transparent; Root.Children.Add(_t); Root.Cursor = Cursors.Hand;
        Content = Root;
        Root.MouseLeftButtonUp += (_, _) => app.TogglePopup();
    }

    public void SetText(string s, bool paused, bool light, bool red)
    {
        _t.Text = paused ? "⏸ " + s : s;
        _t.Opacity = paused ? 0.7 : 1;
        _t.Foreground = red ? Red : (light ? Brushes.Black : Brushes.White);
    }
}
