using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PauseMediaKey
{
    public sealed class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int VK_CANCEL = 0x03; // Ctrl + Break
        private const int VK_PAUSE = 0x13;  // Pause / Interr
        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private long _lastTick = 0;
        private bool _disposed = false;

        public bool IsActive => _hookId != IntPtr.Zero;

        public event Action? MediaKeyTriggered;
        public event Action<bool>? StatusChanged;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public bool Start()
        {
            if (IsActive) return true;

            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                IntPtr hMod = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
            }

            bool success = IsActive;
            StatusChanged?.Invoke(success);
            return success;
        }

        public void Stop()
        {
            if (!IsActive) return;

            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            StatusChanged?.Invoke(false);
        }

        public void Toggle()
        {
            if (IsActive)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                int vkCode = Marshal.ReadInt32(lParam);

                bool isPauseKey = (vkCode == VK_PAUSE) || 
                                  (vkCode == VK_CANCEL && (GetKeyState(0x11) & 0x8000) != 0);

                if (isPauseKey)
                {
                    if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    {
                        long now = Environment.TickCount64;
                        if (now - _lastTick > 250 || now < _lastTick)
                        {
                            _lastTick = now;

                            // Send media play/pause key event
                            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);

                            MediaKeyTriggered?.Invoke();
                        }
                    }

                    // Block the default Windows Pause/Break handling
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}
