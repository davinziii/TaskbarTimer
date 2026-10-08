using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace TaskbarTimer;

public sealed class SettingsWindow : Window
{
    public SettingsWindow(TimerApp app)
    {
        Title = "Taskbar Timer settings"; Width = 320; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var dark = app.Dark; var cfg = app.Cfg;
        Background = new SolidColorBrush(dark ? Color.FromRgb(0x20, 0x20, 0x20) : Color.FromRgb(0xF3, 0xF3, 0xF3));
        Foreground = dark ? Brushes.White : Brushes.Black;
        var p = new StackPanel { Margin = new Thickness(18) };

        void Chk(string t, bool v, System.Action<bool> set)
        {
            var c = new CheckBox { Content = t, IsChecked = v, Margin = new Thickness(0, 4, 0, 4), Foreground = Foreground };
            c.Click += (_, _) => { set(c.IsChecked == true); app.SaveCfg(); };
            p.Children.Add(c);
        }
        void Combo(string label, object[] items, object selected, System.Action<object> set)
        {
            p.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 10, 0, 3) });
            var cb = new ComboBox { ItemsSource = items, SelectedItem = selected };
            cb.SelectionChanged += (_, _) => { if (cb.SelectedItem != null) { set(cb.SelectedItem); app.SaveCfg(); } };
            p.Children.Add(cb);
        }

        Chk("Start with Windows", cfg.Autostart, v => app.SetAutostart(v));
        Chk("Remember last timer", cfg.RememberLast, v => cfg.RememberLast = v);
        Chk("Play sound on completion", cfg.Sound, v => cfg.Sound = v);
        Chk("Windows notification on completion", cfg.Toast, v => cfg.Toast = v);
        Chk("Flash taskbar timer on completion", cfg.Flash, v => cfg.Flash = v);
        Combo("Theme (applies next time a window opens)", new object[] { "System", "Light", "Dark" }, cfg.Theme, v => cfg.Theme = (string)v);
        Combo("Taskbar monitor (0 = primary)", new object[] { 0, 1, 2 }, cfg.Monitor, v => { cfg.Monitor = (int)v; _ = app.AttachWidgetAsync(); });
        Content = p;
    }
}
