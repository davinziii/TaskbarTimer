using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Deskband11Lib.Core;
using Deskband11Lib.Wpf;
using Microsoft.Win32;
namespace TaskbarTimer;

public sealed class TimerApp : Application
{
    public Settings Cfg = new();
    public readonly TimerEngine Engine = new();
    public bool TaskbarLight { get; private set; }
    public bool Dark => Cfg.Theme == "Dark" || (Cfg.Theme == "System" && !ReadLight("AppsUseLightTheme"));

    Tray? _tray; TaskbarWindow? _widget; object? _host; PopupWindow? _popup; SettingsWindow? _settings;
    readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(250) };
    DateTime _popupHidden;

    static bool ReadLight(string name)
    {
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return k?.GetValue(name) is int i && i != 0;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Cfg = Store.Load<Settings>("settings.json") ?? new Settings();
        Engine.Changed += OnChanged;
        Engine.Completed += OnCompleted;
        var snap = Cfg.RememberLast ? Store.Load<Snapshot>("state.json") : null;
        if (snap != null) Engine.Restore(snap); else Engine.SetDuration(TimeSpan.FromMinutes(Cfg.DefaultMinutes));
        TaskbarLight = ReadLight("SystemUsesLightTheme");
        SystemEvents.UserPreferenceChanged += (_, _) => Dispatcher.InvokeAsync(() => { TaskbarLight = ReadLight("SystemUsesLightTheme"); Refresh(); });
        _tray = new Tray(this);
        _ticker.Tick += (_, _) => { Engine.Tick(); Refresh(); }; // runs only while a timer is running
        SyncTicker(); Refresh();
        _ = AttachWidgetAsync();
    }

    void OnChanged() { SyncTicker(); Refresh(); Store.Save("state.json", Engine.Snapshot()); }
    void SyncTicker() => _ticker.IsEnabled = Engine.State == TimerState.Running;

    void OnCompleted()
    {
        if (Cfg.Sound) SystemSounds.Exclamation.Play();
        if (Cfg.Toast) _tray?.Balloon("Timer finished", "Your timer reached 00:00.");
        if (Cfg.Flash) _widget?.Flash();
    }

    void Refresh()
    {
        var t = Engine.Value;
        var text = Fmt.Clock(t, Engine.Mode);
        _widget?.SetText(text, Engine.State == TimerState.Paused, TaskbarLight);
        _tray?.Update(text, Engine.State, t);
        _popup?.Refresh();
    }

    public async Task AttachWidgetAsync()
    {
        try
        {
            if (_host is IDisposable d) d.Dispose();
            _host = null; _widget?.Close();
            var w = new TaskbarWindow(this); _widget = w;
            var host = new TaskbarContentHost(w, w.Root, new TaskbarContentHostOptions
            { PreferredWidth = 104, PreferredHeight = 40, PreferredMonitorIdentity = Cfg.Monitor });
            // Explorer restart / DPI change / monitor removal: rebuild the hosted window. The timer engine is unaffected.
            host.TaskbarWindowRecreationRequired += (_, _) => Dispatcher.InvokeAsync(async () => await AttachWidgetAsync());
            _host = host;
            await host.AttachWhenLayoutReadyAsync();
            w.Show(); Refresh();
        }
        catch (Exception ex)
        {
            Store.Log(ex); _widget?.Close(); _widget = null;
            _tray?.Balloon("Taskbar integration unavailable", "Showing the timer in the tray icon instead.");
        }
    }

    public void ToggleRun() { if (Engine.State == TimerState.Running) Engine.Pause(); else Engine.Start(); }
    public void StartPreset(int minutes) { Engine.SetDuration(TimeSpan.FromMinutes(minutes)); Engine.Start(); }
    public void SaveCfg() => Store.Save("settings.json", Cfg);

    public void TogglePopup()
    {
        if (_popup is { IsVisible: true }) { _popup.Hide(); return; }
        if ((DateTime.UtcNow - _popupHidden).TotalMilliseconds < 250) return; // click that just dismissed it
        ShowPopup();
    }
    public void ShowPopup() { _popup ??= new PopupWindow(this); _popup.ShowAt(); }
    public void PopupHidden() => _popupHidden = DateTime.UtcNow;

    public void ShowSettings()
    {
        _settings?.Close();
        _settings = new SettingsWindow(this); _settings.Show(); _settings.Activate();
    }

    public void SetAutostart(bool on)
    {
        Cfg.Autostart = on;
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (on) k?.SetValue("TaskbarTimer", $"\"{Environment.ProcessPath}\""); else k?.DeleteValue("TaskbarTimer", false);
        SaveCfg();
    }

    public void Quit()
    {
        Store.Save("state.json", Engine.Snapshot());
        _tray?.Dispose();
        if (_host is IDisposable d) d.Dispose();
        Shutdown();
    }
}
