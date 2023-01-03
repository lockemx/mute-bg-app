using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using static Windows.Win32.PInvoke;

namespace MuteAppBG;

public static class User32Event
{
    private static UnhookWinEventSafeHandle? _hookHandle;
    public static Queue<uint> ActiveWindowQueue = new();
    public static event EventHandler<uint>? ActiveWindowChanged;

    public static void Start()
    {
        _hookHandle ??= SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, null, PfnWinEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    private static void PfnWinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, HWND hWnd, int objectId, int childId,
        uint eventThreadId, uint dwEventTimeMs)
    {
        try
        {
            uint pid;
            unsafe
            {
                uint pidTemp;
                GetWindowThreadProcessId(hWnd, &pidTemp);
                pid = pidTemp;
            }

            if (pid <= 0) return;
            ActiveWindowQueue.Enqueue(pid);
            Task.Run(() => ActiveWindowChanged?.Invoke(null, pid));
        }
        catch
        {
            // ignored
        }
    }
}