using System;
using System.IO;
using System.Text.Json;

public class AppSettings
{
    private static AppSettings? _instance;
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BoxyTube",
        "settings.json"
    );

    public string ApiHost { get; set; } = "192.168.1.3:3000";
    public bool UseHttps { get; set; } = false;
    public int DefaultQuality { get; set; } = 1080;

    [System.Text.Json.Serialization.JsonIgnore]
    public string ApiBaseUrl => $"{(UseHttps ? "https" : "http")}://{ApiHost}";

    public static AppSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Load();
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Force reload settings from disk
    /// </summary>
    public static void Reload()
    {
        _instance = Load();
    }

    public static AppSettings Load()
    {
        Console.WriteLine($"Loading settings from: {SettingsPath}");
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                Console.WriteLine($"Loaded settings: {json}");
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                Console.WriteLine($"Parsed API host: {settings.ApiHost}");
                return settings;
            }
            else
            {
                Console.WriteLine("Settings file does not exist, using defaults");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
        return new AppSettings();
    }

    public void Save()
    {
        Console.WriteLine($"Saving settings to: {SettingsPath}");
        Console.WriteLine($"ApiHost: {ApiHost}, UseHttps: {UseHttps}");
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            Console.WriteLine($"Directory: {dir}");
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Console.WriteLine("Creating directory...");
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"JSON to save: {json}");
            File.WriteAllText(SettingsPath, json);
            Console.WriteLine($"Settings saved successfully to {SettingsPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public event Action? SettingsChanged;

    public void NotifySettingsChanged()
    {
        SettingsChanged?.Invoke();
    }
}
