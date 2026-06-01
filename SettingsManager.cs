using System;
using System.IO;
using System.Text.Json;

namespace BlueLockScreen
{
    public class AppSettings
    {
        public string DeviceAddress { get; set; } = "";
        public int RssiThreshold { get; set; } = -70;
        public int LostTimeoutSeconds { get; set; } = 10;
        public bool EnableLogOnExit { get; set; } = false;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Settings { get; private set; } = new AppSettings();

        public static void Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch { Settings = new AppSettings(); }
            }
        }

        public static void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Settings, options);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
