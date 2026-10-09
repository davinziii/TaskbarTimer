using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
namespace TaskbarTimer;

public sealed class SettingsWindow : Window
{
    public SettingsWindow(TimerApp app)
    {
        Title = "Taskbar Timer — Settings"; Width = 380; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen; FontFamily = new FontFamily("Segoe UI Variable, Segoe UI");
        var dark = app.Dark; var cfg = app.Cfg;
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#202020" : "#F7F7F7"));
        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FFFFFF" : "#1A1A1A"));
        var subBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#A6A6A6" : "#666666"));
        var p = new StackPanel { Margin = new Thickness(20) };

        void Head(string t) => p.Children.Add(new TextBlock { Text = t, Foreground = subBrush, FontSize = 12, Margin = new Thickness(0, 14, 0, 4) });
        void Chk(string t, bool v, System.Action<bool> set)
        {
            var c = new CheckBox { Content = t, IsChecked = v, Margin = new Thickness(0, 3, 0, 3), Foreground = Foreground };
            c.Click += (_, _) => { set(c.IsChecked == true); app.SaveCfg(); };
            p.Children.Add(c);
        }
        ComboBox Combo(string[] items, string selected)
        {
            var cb = new ComboBox { ItemsSource = items, SelectedItem = selected }; p.Children.Add(cb); return cb;
        }

        Head("GENERAL");
        Chk("Start with Windows", cfg.Autostart, v => app.SetAutostart(v));
        Chk("Remember last timer", cfg.RememberLast, v => cfg.RememberLast = v);

        Head("WHEN THE TIMER ENDS (keeps ringing until you press Stop)");
        Chk("Play alarm sound", cfg.Sound, v => cfg.Sound = v);
        Chk("Repeat Windows notification", cfg.Toast, v => cfg.Toast = v);
        Chk("Blink the countdown red", cfg.Flash, v => cfg.Flash = v);

        Head("RINGTONE");
        var tones = Alarm.Names.Append(Alarm.CustomName).ToArray();
        var combo = Combo(tones, tones.Contains(cfg.Ringtone) ? cfg.Ringtone : Alarm.Names[0]);
        var file = new TextBlock { Foreground = subBrush, FontSize = 12, Margin = new Thickness(0, 4, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
        file.Text = cfg.Ringtone == Alarm.CustomName && cfg.CustomSoundPath != "" ? Path.GetFileName(cfg.CustomSoundPath) : "";
        bool Browse()
        {
            var dlg = new OpenFileDialog { Filter = "Audio files|*.mp3;*.wav;*.m4a;*.wma;*.aac|All files|*.*" };
            if (dlg.ShowDialog() != true) return false;
            cfg.CustomSoundPath = dlg.FileName; file.Text = Path.GetFileName(dlg.FileName); return true;
        }
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is not string s) return;
            if (s == Alarm.CustomName && !File.Exists(cfg.CustomSoundPath) && !Browse()) { combo.SelectedItem = cfg.Ringtone; return; }
            cfg.Ringtone = s; if (s != Alarm.CustomName) file.Text = ""; app.SaveCfg();
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var browse = new Button { Content = "Choose your own file…", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 8, 0) };
        browse.Click += (_, _) => { if (Browse()) { cfg.Ringtone = Alarm.CustomName; combo.SelectedItem = Alarm.CustomName; app.SaveCfg(); } };
        var test = new Button { Content = "▶ Test", Padding = new Thickness(10, 4, 10, 4) }; bool on = false;
        test.Click += (_, _) => { on = !on; app.Preview(on); test.Content = on ? "■ Stop test" : "▶ Test"; };
        buttons.Children.Add(browse); buttons.Children.Add(test); p.Children.Add(buttons); p.Children.Add(file);
        Closed += (_, _) => app.Preview(false);

        Head("TASKBAR POSITION");
        var places = new[] { ("Beside the system tray", "BeforeNotificationArea"), ("Automatic", "Auto"), ("Left edge", "LeftEdge"), ("Beside the Start button", "BeforeStartButton") };
        var pc = Combo(places.Select(x => x.Item1).ToArray(), (places.FirstOrDefault(x => x.Item2 == cfg.Placement) is { Item1: not null } m ? m : places[0]).Item1);
        pc.SelectionChanged += (_, _) => { cfg.Placement = places[pc.SelectedIndex].Item2; app.SaveCfg(); _ = app.AttachWidgetAsync(); };

        Head("TASKBAR MONITOR");
        var mc = Combo(new[] { "Primary monitor", "Second monitor", "Third monitor" }, new[] { "Primary monitor", "Second monitor", "Third monitor" }[System.Math.Clamp(cfg.Monitor, 0, 2)]);
        mc.SelectionChanged += (_, _) => { cfg.Monitor = mc.SelectedIndex; app.SaveCfg(); _ = app.AttachWidgetAsync(); };

        Head("APPEARANCE (applies the next time a window opens)");
        var tc = Combo(new[] { "System", "Light", "Dark" }, cfg.Theme);
        tc.SelectionChanged += (_, _) => { cfg.Theme = (string)tc.SelectedItem; app.SaveCfg(); };
        Content = p;
    }
}
