# Taskbar Timer (Windows 11)

Shows a countdown/stopwatch inside the Windows 11 taskbar (via Deskband11Lib: SetParent into Shell_TrayWnd +
UI Automation layout). Click it for the control popup. Tray icon with menu is always present and doubles as a
fallback (shows minutes as icon text) if taskbar hosting fails.

## Build
Push to GitHub -> Actions -> artifact `TaskbarTimerSetup`. Or locally (.NET 10 SDK + Inno Setup 6):
    dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
    iscc installer.iss

## Known limitations
- Windows 11 only. Not an official taskbar API: a community technique that can break with Windows updates.
- Not yet compiled or run by the author. Expect to fix small compile/API mismatches against Deskband11Lib.Wpf.Sample.
- Not implemented: launch-minimized, show-tray toggle, auto-start timer, default-mode setting, custom preset editor.
- Popup positions on the primary monitor work area only.

## Manual test checklist
1. Install, launch: timer text appears near the notification area; tray icon present.
2. Click it, set 1 min, start: counts down, sound + notification + flash at 00:00.
3. Pause/resume/reset from popup and tray.
4. Accuracy: 5 min timer under CPU load vs a phone stopwatch.
5. Task Manager -> restart Windows Explorer: widget reappears, timer unaffected (log: %AppData%\TaskbarTimer\log.txt).
6. Toggle taskbar alignment (Settings > Personalization > Taskbar behaviors), fill taskbar with apps.
7. Second monitor: Settings > Taskbar monitor = 1.
8. Fullscreen YouTube/game: timer keeps running, no focus steal.
9. Quit mid-timer, relaunch: remaining time restored.
10. Start with Windows + reboot; then uninstall and confirm folder, Run entry and %AppData%\TaskbarTimer are gone.
