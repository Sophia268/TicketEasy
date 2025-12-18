using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TicketEasy.Models;

namespace TicketEasy.Services;

public class TicketService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://www.80fafa.com";

    private readonly ConfigService _configService;

    public TicketService(ConfigService configService)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _configService = configService;
    }

    private string GetBaseUrl()
    {
        var url = _configService.CurrentConfig.BaseUrl;
        if (string.IsNullOrEmpty(url)) url = "https://www.80fafa.com";
        return url.EndsWith("/") ? url.TrimEnd('/') : url;
    }

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ApiResponse<TicketData>?> CheckConnectivityAsync(string productId)
    {
        try
        {
            // Mode 1: Product Check
            // GET /api/verifyTicket/{productId}
            string baseUrl = GetBaseUrl();
            string url = $"{baseUrl}/api/verifyTicket/{productId}";
            var response = await _httpClient.GetAsync(url);

            // Even if 404, the API returns a JSON body we want to parse
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<TicketData>>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResponse<TicketData>?> ValidateTicketAsync(string productId, string codeInfo)
    {
        try
        {
            // Mode 2: Ticket Verification
            // GET /api/verifyTicket/{productId}/{code}

            // Handle codeInfo. If it comes from QR code as JSON, extract "code" field if possible.
            // Based on previous instructions, QR might contain: {"code":"xxxx", ...}
            // Or it might be just the code string.
            // Let's try to parse it as JSON first to see if it has a "code" property.

            string actualCode = codeInfo;
            try
            {
                if (codeInfo.Trim().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(codeInfo);
                    if (doc.RootElement.TryGetProperty("code", out var codeProp))
                    {
                        actualCode = codeProp.GetString() ?? codeInfo;
                    }
                }
            }
            catch
            {
                // Not valid JSON or error parsing, treat as raw code
            }

            string encodedCode = System.Net.WebUtility.UrlEncode(actualCode);
            string baseUrl = GetBaseUrl();
            string url = $"{baseUrl}/api/verifyTicket/{productId}/{encodedCode}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse<TicketData>>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            return new ApiResponse<TicketData>
            {
                Status = "error",
                Code = 999,
                Msg = $"Exception: {ex.Message}"
            };
        }
    }
}
