using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TicketEasy.Services;

public class TicketService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigService _configService;

    public TicketService(ConfigService configService)
    {
        _httpClient = new HttpClient();
        // Set a reasonable timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _configService = configService;
    }

    private string GetBaseUrl()
    {
        var url = _configService.CurrentConfig.BaseUrl;
        return url.EndsWith("/") ? url.TrimEnd('/') : url;
    }

    public async Task<bool> CheckConnectivityAsync(string productId)
    {
        try
        {
            string baseUrl = GetBaseUrl();
            // Connect: {BaseUrl}/api/verifyTicket/{productId}
            string url = $"{baseUrl}/api/verifyTicket/{productId}";
            var response = await _httpClient.GetAsync(url);
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> ValidateTicketAsync(string productId, string? codeInfo)
    {
        try
        {
            string baseUrl = GetBaseUrl();
            string url;

            if (string.IsNullOrEmpty(codeInfo))
            {
                // Optional code
                url = $"{baseUrl}/api/verifyTicket/{productId}";
            }
            else
            {
                // Check: {BaseUrl}/api/verifyTicket/{productId}/{code}
                string encodedPayload = System.Net.WebUtility.UrlEncode(codeInfo);
                url = $"{baseUrl}/api/verifyTicket/{productId}/{encodedPayload}";
            }
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }
}
