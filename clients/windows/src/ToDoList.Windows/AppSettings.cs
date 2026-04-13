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
