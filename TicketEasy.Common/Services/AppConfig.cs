using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TicketEasy.Services
{
    public class AppConfig
    {
        public string Password { get; set; } = string.Empty;
        public string URI { get; set; } = string.Empty;
    }

    public static class ConfigProvider
    {
        private static AppConfig? _cachedConfig;

        public static AppConfig Get()
        {
            if (_cachedConfig != null) return _cachedConfig;

            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "config.json");

            // Try to look in project root if debugging (optional, but helpful)
            if (!File.Exists(path))
            {
                // Go up levels to find source if running from bin
                // This is a heuristic and might need adjustment or removal for production
            }

            try
            {
                var json = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "{}";
                _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppConfig();
            }
            catch
            {
                _cachedConfig = new AppConfig();
            }
            return _cachedConfig;
        }
    }
}
