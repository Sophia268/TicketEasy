using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TicketEasy.Models;

namespace TicketEasy.Services;

public class ConfigService
{
    private const string ConfigFileName = "config.json";
    private readonly string _writablePath;
    private readonly string _bundledPath;

    public AppConfig CurrentConfig { get; private set; } = new();

    public ConfigService()
    {
        string personalFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        _writablePath = Path.Combine(personalFolder, ConfigFileName);
        
        // For bundled path, we look in the base directory
        _bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
    }

    public async Task LoadConfigAsync()
    {
        try
        {
            // 1. Try to load from writable location (user modified)
            if (File.Exists(_writablePath))
            {
                string json = await File.ReadAllTextAsync(_writablePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    CurrentConfig = config;
                    return;
                }
            }

            // 2. Try to load from bundled location (initial)
            if (File.Exists(_bundledPath))
            {
                string json = await File.ReadAllTextAsync(_bundledPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    CurrentConfig = config;
                    // Copy to writable path for future updates
                    await SaveConfigInternalAsync(); 
                }
            }
            else
            {
                // If neither exists, we might be in an environment where direct file access to bundle isn't allowed (like raw Assets in Android)
                // But since we used CopyToOutputDirectory, it *should* be in the app directory.
                // If not, we just use defaults.
                System.Diagnostics.Debug.WriteLine($"Config not found at {_bundledPath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
        }
    }

    public async Task SaveConfigAsync(string productId)
    {
        CurrentConfig.ProductId = productId;
        await SaveConfigInternalAsync();
    }

    private async Task SaveConfigInternalAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(CurrentConfig, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_writablePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
        }
    }
}
