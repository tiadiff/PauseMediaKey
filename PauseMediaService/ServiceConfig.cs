using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Win32;

namespace PauseMediaKey
{
    public class ServiceConfig
    {
        public const string EventName = "Local\\PauseMediaService_ConfigChanged_Event";
        public const string MutexName = "Local\\PauseMediaService_SingleInstance_Mutex";
        public const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public const string RunValueName = "PauseMediaService";

        public bool IsEnabled { get; set; } = true;
        public bool Autostart { get; set; } = true;
        public DateTime? LastTriggered { get; set; }

        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PauseMediaKey"
        );

        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "settings.json");

        public static ServiceConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<ServiceConfig>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch
            {
                // Fallback to default on read error
            }

            var defaultConfig = new ServiceConfig();
            defaultConfig.Save();
            return defaultConfig;
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Ignore disk write errors
            }
        }

        public static void NotifyService()
        {
            try
            {
                if (EventWaitHandle.TryOpenExisting(EventName, out var handle))
                {
                    using (handle)
                    {
                        handle.Set();
                    }
                }
            }
            catch
            {
                // Ignore if service is not running
            }
        }

        public static bool IsAutostartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(RunValueName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void SetAutostart(bool enable, string serviceExePath)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key != null)
                {
                    if (enable)
                    {
                        key.SetValue(RunValueName, $"\"{serviceExePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(RunValueName, false);
                    }
                }
            }
            catch
            {
                // Ignore registry permission errors
            }
        }
    }
}

