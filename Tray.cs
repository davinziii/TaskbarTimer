using System;
using System.Runtime.InteropServices;
using WF = System.Windows.Forms;
using D = System.Drawing;
namespace TaskbarTimer;

public sealed class Tray : IDisposable
{
    readonly WF.NotifyIcon _ni; readonly TimerApp _app; IntPtr _h; string _lastIcon = "";
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    public Tray(TimerApp app)
    {
        _app = app;
        var menu = new WF.ContextMenuStrip();
        menu.Items.Add(new WF.ToolStripMenuItem("Taskbar Timer") { Enabled = false });
        menu.Items.Add(new WF.ToolStripSeparator());
        menu.Items.Add("▶ Start", null, (_, _) => app.Engine.Start());
        menu.Items.Add("⏸ Pause", null, (_, _) => app.Engine.Pause());
        menu.Items.Add("↻ Reset", null, (_, _) => app.Engine.Reset());
        menu.Items.Add(new WF.ToolStripSeparator());
        foreach (var m in new[] { 5, 10, 25, 30, 60 })
            menu.Items.Add(m == 60 ? "1 hour" : $"{m} minutes", null, (_, _) => app.StartPreset(m));
        menu.Items.Add("Custom...", null, (_, _) => app.ShowPopup());
        menu.Items.Add(new WF.ToolStripSeparator());
        menu.Items.Add("Settings", null, (_, _) => app.ShowSettings());
        var auto = new WF.ToolStripMenuItem("Start with Windows") { CheckOnClick = true };
        auto.Click += (_, _) => app.SetAutostart(auto.Checked);
        menu.Items.Add(auto);
        menu.Items.Add("Quit", null, (_, _) => app.Quit());
        menu.Opening += (_, _) => auto.Checked = app.Cfg.Autostart;

        _ni = new WF.NotifyIcon { Visible = true, Text = "Taskbar Timer", ContextMenuStrip = menu, Icon = DefaultIcon() };
        _ni.MouseClick += (_, e) => { if (e.Button == WF.MouseButtons.Left) app.TogglePopup(); };
    }

    static D.Icon DefaultIcon() => D.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? D.SystemIcons.Application;

    public void Update(string text, TimerState state, TimeSpan t)
    {
        _ni.Text = state == TimerState.Idle ? "Taskbar Timer" : text;
        string icon = state == TimerState.Idle ? "" : Fmt.Short(t);
        if (icon == _lastIcon) return;
        _lastIcon = icon;
        if (icon == "") { _ni.Icon = DefaultIcon(); Free(); return; }
        using var bmp = new D.Bitmap(32, 32);
        using (var g = D.Graphics.FromImage(bmp))
        {
            g.TextRenderingHint = D.Text.TextRenderingHint.AntiAliasGridFit;
            using var f = new D.Font("Segoe UI", icon.Length > 2 ? 15f : 20f, D.FontStyle.Bold, D.GraphicsUnit.Pixel);
            using var br = new D.SolidBrush(_app.TaskbarLight ? D.Color.Black : D.Color.White);
            using var sf = new D.StringFormat { Alignment = D.StringAlignment.Center, LineAlignment = D.StringAlignment.Center };
            g.DrawString(icon, f, br, new D.RectangleF(0, 0, 32, 32), sf);
        }
        var h = bmp.GetHicon();
        _ni.Icon = D.Icon.FromHandle(h);
        Free(); _h = h;
    }
    void Free() { if (_h != IntPtr.Zero) { DestroyIcon(_h); _h = IntPtr.Zero; } }

    public void Balloon(string title, string text) => _ni.ShowBalloonTip(5000, title, text, WF.ToolTipIcon.Info);
    public void Dispose() { _ni.Visible = false; _ni.Dispose(); Free(); }
}
