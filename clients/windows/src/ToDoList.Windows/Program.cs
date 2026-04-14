using System.Windows.Forms;
using Krypton.Toolkit;
using ToDoList.Windows.ApiClient;
using ToDoList.Windows.Debug;
using ToDoList.Windows.Forms;

namespace ToDoList.Windows
{
    static class Program
    {
        private static KryptonManager? kryptonManager;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Set Krypton theme globally before creating any forms
            kryptonManager = new KryptonManager();
            if (AppTheme.IsDark)
            {
                kryptonManager.GlobalPaletteMode = PaletteMode.Office2007Black;
            }

            AppSettings settings = AppSettings.Load();
            string serverUrl = settings.ServerUrl;

            if (string.IsNullOrEmpty(serverUrl))
            {
                // Default for development
                serverUrl = "http://192.168.74.122:5000";
                settings.ServerUrl = serverUrl;
            }

            RemoteLogger.Init(serverUrl);
            RemoteLogger.Info("App", $"YepList Windows started, server={serverUrl}");

            TodoApiClient apiClient = new TodoApiClient(serverUrl);
            Application.Run(new MainForm(apiClient, settings));
        }
    }
}
