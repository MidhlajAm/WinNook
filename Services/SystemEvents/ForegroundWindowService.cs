using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Project_Nook.Services.SystemEvents
{
    public class ForegroundWindowService : IDisposable
    {
        public event EventHandler<string>? ForegroundAppChanged;

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        
        private WinEventDelegate _winEventDelegate;
        private IntPtr _hook;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public ForegroundWindowService()
        {
            _winEventDelegate = new WinEventDelegate(WinEventProc);
            _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                GetWindowThreadProcessId(hwnd, out uint processId);
                if (processId > 0)
                {
                    try
                    {
                        var process = Process.GetProcessById((int)processId);
                        var exeName = process.ProcessName + ".exe";
                        ForegroundAppChanged?.Invoke(this, exeName);
                    }
                    catch
                    {
                        // Some protected processes can throw access denied
                        ForegroundAppChanged?.Invoke(this, string.Empty);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}
