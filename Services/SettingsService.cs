using tool_cantam.Core.Contracts; // ISettingsService burada olduğu için şart!
using tool_cantam.Core.Models;
using System;
using System.IO;
using System.Text.Json;

namespace tool_cantam.Services;

// KRİTİK NOKTA: Sınıf isminin yanındaki ": ISettingsService" 
// App.xaml.cs'deki o hatalı satırı düzelten tek şeydir.
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var defaultSettings = new AppSettings();
                Save(defaultSettings);
                return defaultSettings;
            }

            string json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ayarlar kaydedilemedi: {ex.Message}");
        }
    }
}