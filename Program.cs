using System;
using System.Threading;
namespace TaskbarTimer;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "TaskbarTimer.SingleInstance", out var created);
        if (!created) return;
        new TimerApp().Run();
    }
}
