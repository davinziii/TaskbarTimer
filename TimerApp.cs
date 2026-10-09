using System;
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
    public readonly Alarm Alarm = new();
    public bool TaskbarLight { get; private set; }
    public bool Ringing { get; private set; }
    public bool Dark => Cfg.Theme == "Dark" || (Cfg.Theme == "System" && !ReadLight("AppsUseLightTheme"));
    public bool RedNow => Ringing && (!Cfg.Flash || _blinkOn);

    Tray? _tray; TaskbarWindow? _widget; object? _host; PopupWindow? _popup; SettingsWindow? _settings;
    readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(250) };
    readonly DispatcherTimer _blink = new() { Interval = TimeSpan.FromMilliseconds(500) };
    readonly DispatcherTimer _nag = new() { Interval = TimeSpan.FromSeconds(20) };
    DateTime _popupHidden; bool _blinkOn;

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
        TaskbarLight = ReadLight("SystemUsesLightTheme");
        SystemEvents.UserPreferenceChanged += (_, _) => Dispatcher.InvokeAsync(() => { TaskbarLight = ReadLight("SystemUsesLightTheme"); Refresh(); });
        _tray = new Tray(this);
        _ticker.Tick += (_, _) => { Engine.Tick(); Refresh(); };   // runs only while a timer is running
        _blink.Tick += (_, _) => { _blinkOn = !_blinkOn; Refresh(); }; // runs only while the alarm rings
        _nag.Tick += (_, _) => Nag();
        Engine.Changed += OnChanged;
        Engine.Completed += OnCompleted;
        var snap = Cfg.RememberLast ? Store.Load<Snapshot>("state.json") : null;
        if (snap != null) Engine.Restore(snap); else Engine.SetDuration(TimeSpan.FromMinutes(Cfg.DefaultMinutes));
        SyncTicker(); Refresh();
        _ = AttachWidgetAsync();
    }

    void OnChanged()
    {
        if (Ringing && Engine.State != TimerState.Done) StopAlarm();
        SyncTicker(); Refresh(); Store.Save("state.json", Engine.Snapshot());
    }
    void SyncTicker() => _ticker.IsEnabled = Engine.State == TimerState.Running;

    // ---- alarm: loops sound, blinks red, re-notifies and re-opens the popup every 20 s until the user stops it ----
    void OnCompleted()
    {
        Ringing = true; _blinkOn = true; _blink.Start(); _nag.Start();
        if (Cfg.Sound) Alarm.Start(Cfg);
        Nag(); Refresh();
    }
    void Nag()
    {
        if (!Ringing) return;
        if (Cfg.Toast) _tray?.Balloon("Time's up!", "Your timer reached 00:00. Open it and press Stop.");
        ShowPopup();
    }
    void StopAlarm()
    {
        if (!Ringing) return;
        Ringing = false; _blink.Stop(); _nag.Stop(); Alarm.Stop(); Refresh();
    }
    public void Preview(bool on) { if (Ringing) return; if (on) Alarm.Start(Cfg); else Alarm.Stop(); }

    void Refresh()
    {
        var t = Engine.Value; var text = Fmt.Clock(t, Engine.Mode); var red = RedNow;
        _widget?.SetText(text, Engine.State == TimerState.Paused, TaskbarLight, red);
        _tray?.Update(text, Engine.State, t);
        _popup?.Refresh(red);
    }

    public async Task AttachWidgetAsync()
    {
        try
        {
            if (_host is IDisposable d) d.Dispose();
            _host = null; _widget?.Close();
            var w = new TaskbarWindow(this); _widget = w;
            var opts = new TaskbarContentHostOptions { PreferredWidth = 104, PreferredHeight = 48, PreferredMonitorIdentity = Cfg.Monitor };
            try // set Placement by name so this compiles regardless of the enum's type name
            {
                var pr = opts.GetType().GetProperty("Placement");
                if (pr != null && pr.PropertyType.IsEnum) pr.SetValue(opts, Enum.Parse(pr.PropertyType, Cfg.Placement));
            }
            catch (Exception ex) { Store.Log(ex); }
            var host = new TaskbarContentHost(w, w.Root, opts);
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

    // ---- commands ----
    public void Play() => Engine.Start();                       // from Done/Idle: start; from Paused: resume
    public void StopPress()
    {
        switch (Engine.State)
        {
            case TimerState.Running: Engine.Pause(); break;
            case TimerState.Paused: Engine.Reset(); break;
            case TimerState.Done: Engine.Reset(); break;       // silences the alarm via OnChanged
        }
    }
    public void StartPreset(int minutes) { Engine.SetDuration(TimeSpan.FromMinutes(minutes)); Engine.Start(); }
    public void SaveCfg() => Store.Save("settings.json", Cfg);

    public void TogglePopup()
    {
        if (Ringing) { ShowPopup(); return; }
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
        StopAlarm();
        Store.Save("state.json", Engine.Snapshot());
        _tray?.Dispose();
        if (_host is IDisposable d) d.Dispose();
        Shutdown();
    }
}
