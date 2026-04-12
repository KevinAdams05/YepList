using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Krypton.Toolkit;
using ToDoList.Windows.ApiClient;
using ToDoList.Windows.Forms;

namespace ToDoList.Windows
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string? serverUrl = LoadServerUrl();
            if (string.IsNullOrEmpty(serverUrl))
            {
                KryptonMessageBox.Show(
                    "Server URL not configured.\n\nCreate a settings.json file next to the executable with:\n{\"ServerUrl\": \"http://192.168.74.122:5000\"}",
                    "Configuration Required",
                    KryptonMessageBoxButtons.OK,
                    KryptonMessageBoxIcon.Warning);
                return;
            }

            TodoApiClient apiClient = new TodoApiClient(serverUrl);
            Application.Run(new MainForm(apiClient));
        }

        private static string? LoadServerUrl()
        {
            string settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings?.ServerUrl;
                }
                catch
                {
                    return null;
                }
            }

            // Default for development
            return "http://192.168.74.122:5000";
        }
    }

    public class AppSettings
    {
        public string ServerUrl { get; set; } = string.Empty;
    }
}
