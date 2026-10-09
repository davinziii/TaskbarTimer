using System;
namespace TaskbarTimer;

public enum TimerMode { Countdown, Stopwatch }
public enum TimerState { Idle, Running, Paused, Done }
public sealed record Snapshot(TimerMode Mode, TimerState State, double DurationSec, DateTime EndUtc, DateTime StartUtc, double FrozenSec);

/// UI-independent, timestamp-based engine. No per-second decrementing: values are always derived from UtcNow.
public sealed class TimerEngine
{
    public TimerMode Mode { get; private set; } = TimerMode.Countdown;
    public TimerState State { get; private set; } = TimerState.Idle;
    public TimeSpan Duration { get; private set; } = TimeSpan.FromMinutes(25);
    DateTime _end, _start; TimeSpan _frozen;
    public event Action? Changed;
    public event Action? Completed;

    public TimeSpan Value
    {
        get
        {
            var now = DateTime.UtcNow;
            return (Mode, State) switch
            {
                (TimerMode.Countdown, TimerState.Running) => Max(_end - now, TimeSpan.Zero),
                (TimerMode.Countdown, TimerState.Paused) => _frozen,
                (TimerMode.Countdown, TimerState.Done) => TimeSpan.Zero,
                (TimerMode.Countdown, _) => Duration,
                (_, TimerState.Running) => now - _start,
                (_, TimerState.Paused) => _frozen,
                _ => TimeSpan.Zero
            };
        }
    }

    static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    public void Start()
    {
        if (State == TimerState.Running) return;
        var now = DateTime.UtcNow;
        if (State == TimerState.Paused)
        {
            if (Mode == TimerMode.Countdown) _end = now + _frozen; else _start = now - _frozen;
        }
        else if (Mode == TimerMode.Countdown)
        {
            if (Duration <= TimeSpan.Zero) return;
            _end = now + Duration;
        }
        else _start = now;
        State = TimerState.Running; Changed?.Invoke();
    }
    public void Pause() { if (State != TimerState.Running) return; _frozen = Value; State = TimerState.Paused; Changed?.Invoke(); }
    public void Reset() { State = TimerState.Idle; _frozen = TimeSpan.Zero; Changed?.Invoke(); }
    public void Restart() { State = TimerState.Idle; _frozen = TimeSpan.Zero; Start(); }
    public void SetMode(TimerMode m) { Mode = m; State = TimerState.Idle; Changed?.Invoke(); }
    public void SetDuration(TimeSpan d) { Duration = Max(d, TimeSpan.FromSeconds(1)); Mode = TimerMode.Countdown; State = TimerState.Idle; Changed?.Invoke(); }
    public void Add(TimeSpan d)
    {
        if (Mode != TimerMode.Countdown) return;
        switch (State)
        {
            case TimerState.Running: _end += d; Tick(); break;
            case TimerState.Paused: _frozen = Max(_frozen + d, TimeSpan.Zero); break;
            case TimerState.Done: if (d > TimeSpan.Zero) { _end = DateTime.UtcNow + d; State = TimerState.Running; } break; // snooze
            default: Duration = Max(Duration + d, TimeSpan.FromSeconds(1)); State = TimerState.Idle; break;
        }
        Changed?.Invoke();
    }
    public void Tick()
    {
        if (Mode == TimerMode.Countdown && State == TimerState.Running && _end <= DateTime.UtcNow)
        { State = TimerState.Done; Changed?.Invoke(); Completed?.Invoke(); }
    }
    public Snapshot Snapshot() => new(Mode, State, Duration.TotalSeconds, _end, _start, _frozen.TotalSeconds);
    public void Restore(Snapshot s)
    {
        Mode = s.Mode; State = s.State; Duration = TimeSpan.FromSeconds(Math.Max(1, s.DurationSec));
        _end = s.EndUtc; _start = s.StartUtc; _frozen = TimeSpan.FromSeconds(s.FrozenSec);
        if (State == TimerState.Running && Mode == TimerMode.Countdown && _end < DateTime.UtcNow.AddMinutes(-5)) { State = TimerState.Idle; return; }
        Tick(); // finished while the app was closed (<5 min ago) -> ring
    }
}

public static class Fmt
{
    public static string Clock(TimeSpan t, TimerMode mode)
    {
        long s = mode == TimerMode.Countdown ? (long)Math.Ceiling(t.TotalSeconds) : (long)Math.Floor(t.TotalSeconds);
        if (s < 0) s = 0;
        long h = s / 3600, m = s % 3600 / 60, sec = s % 60;
        return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m:00}:{sec:00}";
    }
    public static string Short(TimeSpan t) =>
        t.TotalSeconds >= 3600 ? $"{(int)t.TotalHours}h" :
        t.TotalSeconds >= 60 ? ((int)Math.Ceiling(t.TotalMinutes)).ToString() : ((int)Math.Ceiling(t.TotalSeconds)).ToString();
}
