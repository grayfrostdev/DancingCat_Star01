using System;
using System.IO;
using System.Text.Json;

namespace DancingCat
{
    public class AppSettings
    {
        public double SpeedSensitivity { get; set; } = 0.6;
        public double CatSize { get; set; } = 200.0;
        public bool ShowStatusText { get; set; } = false;
        public int SelectedCatType { get; set; } = 1;
        public bool ReverseRotation { get; set; } = false;
        public bool RunOnStartup { get; set; } = false;
        
        // Window position
        public double WindowLeft { get; set; } = double.NaN;
        public double WindowTop { get; set; } = double.NaN;
        
        private static string GetSettingsPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        }

        public static AppSettings Load()
        {
            string path = GetSettingsPath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    // Ignore parsing errors and return default
                }
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetSettingsPath(), json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
