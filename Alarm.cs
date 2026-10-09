using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
namespace TaskbarTimer;

/// Looping alarm. Built-in ringtones are synthesized once into %AppData%\TaskbarTimer\sounds; users can pick any audio file.
public sealed class Alarm
{
    public static readonly string[] Names = { "Classic Alarm", "Digital Beeps", "Soft Chime", "Temple Bell", "Siren" };
    public const string CustomName = "Custom file…";
    readonly MediaPlayer _p = new();
    bool _on, _fellBack;

    public Alarm()
    {
        _p.MediaEnded += (_, _) => { if (_on) { _p.Position = TimeSpan.Zero; _p.Play(); } };
        _p.MediaFailed += (_, _) => { if (_on && !_fellBack) { _fellBack = true; Play(Ensure(Names[0])); } };
    }

    public void Start(Settings s) { Stop(); _on = true; _fellBack = false; Play(PathFor(s)); }
    void Play(string path) { _p.Open(new Uri(path)); _p.Volume = 1; _p.Play(); }
    public void Stop() { _on = false; _p.Stop(); _p.Close(); }

    static string PathFor(Settings s)
    {
        if (s.Ringtone == CustomName && File.Exists(s.CustomSoundPath)) return s.CustomSoundPath;
        return Ensure(Array.IndexOf(Names, s.Ringtone) >= 0 ? s.Ringtone : Names[0]);
    }

    static string Ensure(string name)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarTimer", "sounds");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name.Replace(' ', '_') + ".wav");
        if (!File.Exists(path)) File.WriteAllBytes(path, Synth(name));
        return path;
    }

    // ---- tiny synthesizer (16-bit mono 44.1 kHz) ----
    const int SR = 44100;
    static void Tone(List<short> b, double f, int ms, int mode = 0, double decay = 0, double f2 = 0, double amp = 0.5)
    {
        int n = SR * ms / 1000; double ph = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SR, fr = f2 > 0 ? f + (f2 - f) * i / n : f;
            ph += 2 * Math.PI * fr / SR;
            double v = mode switch
            {
                1 => Math.Sin(ph) + 0.35 * Math.Sin(3 * ph),
                2 => (Math.Sin(ph) + 0.5 * Math.Sin(2.76 * ph) + 0.25 * Math.Sin(5.4 * ph)) / 1.6,
                _ => Math.Sin(ph)
            };
            double env = Math.Min(1, i / (SR * 0.004)) * Math.Min(1, (n - i) / (SR * 0.01)) * Math.Exp(-decay * t);
            b.Add((short)(v * env * amp * 32767));
        }
    }
    static void Gap(List<short> b, int ms) { for (int i = 0; i < SR * ms / 1000; i++) b.Add(0); }

    static byte[] Synth(string name)
    {
        var b = new List<short>();
        switch (name)
        {
            case "Digital Beeps": for (int i = 0; i < 3; i++) { Tone(b, 1760, 70); Gap(b, 70); } Gap(b, 500); break;
            case "Soft Chime": foreach (var f in new[] { 523.25, 659.25, 783.99, 1046.5 }) Tone(b, f, 320, 0, 4); Gap(b, 600); break;
            case "Temple Bell": Tone(b, 660, 1800, 2, 2.2); Gap(b, 400); break;
            case "Siren": for (int i = 0; i < 2; i++) { Tone(b, 600, 600, 0, 0, 1000); Tone(b, 1000, 600, 0, 0, 600); } Gap(b, 300); break;
            default: for (int i = 0; i < 4; i++) { Tone(b, 880, 130, 1); Gap(b, 70); } Gap(b, 550); break;
        }
        using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
        int bytes = b.Count * 2;
        w.Write("RIFF"u8.ToArray()); w.Write(36 + bytes); w.Write("WAVEfmt "u8.ToArray()); w.Write(16);
        w.Write((short)1); w.Write((short)1); w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8.ToArray()); w.Write(bytes);
        foreach (var s in b) w.Write(s);
        return ms.ToArray();
    }
}
