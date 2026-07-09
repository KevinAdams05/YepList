using System;
using System.IO;
using System.Text.Json;

namespace ToDoList.Windows
{
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");

        public string ServerUrl { get; set; } = string.Empty;
        public long? DefaultListId { get; set; }

        // Stable per-install identifier sent to the server (X-Device-Id) so
        // sync activity and deletions can be attributed to this device.
        public string DeviceId { get; set; } = string.Empty;

        // Generates and persists a DeviceId on first use.
        public string EnsureDeviceId()
        {
            if (string.IsNullOrEmpty(DeviceId))
            {
                DeviceId = Guid.NewGuid().ToString("N");
                Save();
            }
            return DeviceId;
        }

        public static AppSettings Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    return new AppSettings();
                }
            }

            return new AppSettings();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
}
