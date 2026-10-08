using System;
using System.IO;
using System.Text.Json;
namespace TaskbarTimer;

public sealed class Settings
{
    public bool Autostart { get; set; }
    public bool RememberLast { get; set; } = true;
    public bool Sound { get; set; } = true;
    public bool Toast { get; set; } = true;
    public bool Flash { get; set; } = true;
    public string Theme { get; set; } = "System";
    public int DefaultMinutes { get; set; } = 25;
    public int Monitor { get; set; } = 0;
    public int[] Presets { get; set; } = { 1, 5, 10, 15, 25, 30, 45, 60 };
}

public static class Store
{
    static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarTimer");
    public static T? Load<T>(string name) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(Dir, name))); } catch { return null; }
    }
    public static void Save(string name, object o)
    {
        try { Directory.CreateDirectory(Dir); File.WriteAllText(Path.Combine(Dir, name), JsonSerializer.Serialize(o)); } catch { }
    }
    public static void Log(Exception ex)
    {
        try { Directory.CreateDirectory(Dir); File.AppendAllText(Path.Combine(Dir, "log.txt"), $"{DateTime.Now:s} {ex}\n"); } catch { }
    }
}
