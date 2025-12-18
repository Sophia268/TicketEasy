using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TicketEasy.Models;
using Avalonia.Platform;

namespace TicketEasy.Services;

public class ConfigService
{
    private const string ConfigFileName = "config.json";
    private readonly string _writablePath;

    // Resource URI for Avalonia AssetLoader
    // Format: avares://Assembly.Name/Path/To/Resource
    private readonly Uri _resourceUri = new Uri("avares://TicketEasy.Common/config.json");

    public AppConfig CurrentConfig { get; private set; } = new();

    public ConfigService()
    {
        string personalFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        _writablePath = Path.Combine(personalFolder, ConfigFileName);
    }

    public async Task LoadConfigAsync()
    {
        try
        {
            // 1. Try to load from writable location (user modified)
            if (File.Exists(_writablePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(_writablePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        CurrentConfig = config;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load writable config: {ex.Message}");
                }
            }

            // 2. Try to load from embedded Avalonia Resource (initial default)
            if (AssetLoader.Exists(_resourceUri))
            {
                using var stream = AssetLoader.Open(_resourceUri);
                using var reader = new StreamReader(stream);
                string json = await reader.ReadToEndAsync();

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
                System.Diagnostics.Debug.WriteLine($"Config resource not found at {_resourceUri}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
        }
    }

    public async Task SaveConfigAsync(string productId, string productHashcode = "")
    {
        CurrentConfig.ProductId = productId;
        if (!string.IsNullOrEmpty(productHashcode))
        {
            CurrentConfig.ProductHashcode = productHashcode;
        }
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
