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
        private volatile bool _isEnabled = true;
        private long _lastTick = 0;
        private bool _disposed = false;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                StatusChanged?.Invoke(_isEnabled);
            }
        }

        public bool IsHookInstalled => _hookId != IntPtr.Zero;

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

        public KeyboardHook(bool initialEnabled = true)
        {
            _isEnabled = initialEnabled;
            _proc = HookCallback;
            InstallHook();
        }

        private void InstallHook()
        {
            if (_hookId != IntPtr.Zero) return;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            IntPtr hMod = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isEnabled)
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

                            // Send media play/pause key
                            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);

                            MediaKeyTriggered?.Invoke();
                        }
                    }

                    // Swallow the Pause key so default OS action is blocked
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _isEnabled = false;
                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
                _disposed = true;
            }
        }
    }
}

