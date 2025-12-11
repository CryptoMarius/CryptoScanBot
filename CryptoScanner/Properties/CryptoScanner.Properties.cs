using System.Text.Json;

namespace CryptoScanner.Properties
{
    public class Settings
    {
        private static Settings? _default;
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CryptoScanner",
            "settings.json"
        );

        public static Settings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = Load();
                }
                return _default;
            }
        }

        // Properties voor je instellingen
        public string GridColumnConfig { get; set; } = string.Empty;

        // Voeg hier meer settings toe als je wilt
        public int WindowWidth { get; set; } = 1000;
        public int WindowHeight { get; set; } = 800;
        public string Theme { get; set; } = "Default";

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error maar crash niet
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    return settings ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                // Log error maar gebruik default settings
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            return new Settings();
        }

        public void Reset()
        {
            GridColumnConfig = string.Empty;
            WindowWidth = 1000;
            WindowHeight = 800;
            Theme = "Default";
            Save();
        }
    }
}