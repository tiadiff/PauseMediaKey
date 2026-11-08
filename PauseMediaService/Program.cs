using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace PauseMediaKey
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Handle command line flags if invoked to control running service
            if (args.Length > 0)
            {
                var arg = args[0].ToLowerInvariant();
                var currentConfig = ServiceConfig.Load();

                if (arg == "--toggle")
                {
                    currentConfig.IsEnabled = !currentConfig.IsEnabled;
                    currentConfig.Save();
                    ServiceConfig.NotifyService();
                    return;
                }
                else if (arg == "--enable")
                {
                    currentConfig.IsEnabled = true;
                    currentConfig.Save();
                    ServiceConfig.NotifyService();
                    return;
                }
                else if (arg == "--disable")
                {
                    currentConfig.IsEnabled = false;
                    currentConfig.Save();
                    ServiceConfig.NotifyService();
                    return;
                }
                else if (arg == "--stop")
                {
                    // Kill existing service
                    var current = Process.GetCurrentProcess();
                    foreach (var p in Process.GetProcessesByName("PauseMediaService"))
                    {
                        if (p.Id != current.Id)
                        {
                            try { p.Kill(); } catch { }
                        }
                    }
                    return;
                }
            }

            // Single instance protection for the service
            using (var mutex = new Mutex(true, ServiceConfig.MutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    return; // Service already running in background
                }

                var config = ServiceConfig.Load();

                // Ensure registry autostart matches config
                if (config.Autostart)
                {
                    ServiceConfig.SetAutostart(true, Application.ExecutablePath);
                }

                using var hook = new KeyboardHook(config.IsEnabled);

                hook.MediaKeyTriggered += () =>
                {
                    config.LastTriggered = DateTime.Now;
                    config.Save();
                };

                // Thread to listen for configuration changes triggered by the management Form
                using var stopEvent = new ManualResetEvent(false);
                using var changeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ServiceConfig.EventName);

                var listenerThread = new Thread(() =>
                {
                    var handles = new WaitHandle[] { stopEvent, changeEvent };
                    while (true)
                    {
                        int index = WaitHandle.WaitAny(handles);
                        if (index == 0) break; // Stop requested

                        // Config changed signal received
                        var updated = ServiceConfig.Load();
                        hook.IsEnabled = updated.IsEnabled;

                        if (updated.Autostart != ServiceConfig.IsAutostartEnabled())
                        {
                            ServiceConfig.SetAutostart(updated.Autostart, Application.ExecutablePath);
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "ConfigChangeListener"
                };
                listenerThread.Start();

                // Run message pump for low-level hook
                ApplicationConfiguration.Initialize();
                Application.Run();

                stopEvent.Set();
            }
        }
    }
}
