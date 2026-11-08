using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PauseMediaKey
{
    internal static class Program
    {
        private const string MutexName = "Local\\PauseMediaKey_UI_Mutex";

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            using (var mutex = new Mutex(true, MutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    // Bring existing Form to front
                    var current = Process.GetCurrentProcess();
                    foreach (var process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                        {
                            ShowWindow(process.MainWindowHandle, SW_RESTORE);
                            SetForegroundWindow(process.MainWindowHandle);
                            break;
                        }
                    }
                    return;
                }

                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
        }
    }
}
