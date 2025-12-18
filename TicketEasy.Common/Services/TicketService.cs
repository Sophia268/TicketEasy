using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TicketEasy.Models;

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

    public async Task<ApiResponse<ProductMsg>?> CheckConnectivityAsync(string productId)
    {
        try
        {
            string baseUrl = GetBaseUrl();
            // Connect: {BaseUrl}/api/verifyTicket/{productId}
            // API Doc: Returns status="ok", code=200 if exists.

            string url = $"{baseUrl}/api/verifyTicket/{productId}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResp = JsonSerializer.Deserialize<ApiResponse<ProductMsg>>(json);
                return apiResp;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResponse<ProductMsg>?> ValidateTicketAsync(string productId, string? codeInfo)
    {
        try
        {
            string baseUrl = GetBaseUrl();
            string url;

            if (string.IsNullOrEmpty(codeInfo))
            {
                // Fallback to product check if code is empty, though caller should usually provide code for validation
                url = $"{baseUrl}/api/verifyTicket/{productId}";
            }
            else
            {
                // Check: {BaseUrl}/api/verifyTicket/{productId}/{code}
                // Note: codeInfo might need careful handling if it's JSON from QR vs plain string.
                // The API expects just the code string in the URL path.
                // If codeInfo is JSON (e.g. {"code":"..."}), we should extract it before calling this, 
                // OR we assume codeInfo passed here is ALREADY the raw code.
                // Based on previous context, we will assume codeInfo passed here is the raw code string.

                string encodedCode = System.Net.WebUtility.UrlEncode(codeInfo);
                url = $"{baseUrl}/api/verifyTicket/{productId}/{encodedCode}";
            }

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            // The API might return 404 or 400 with a JSON body, so we should try to parse JSON even if !IsSuccessStatusCode
            // However, HttpClient.GetAsync doesn't throw on 404 unless EnsureSuccessStatusCode is called.

            try
            {
                return JsonSerializer.Deserialize<ApiResponse<ProductMsg>>(json);
            }
            catch
            {
                // If parsing fails (e.g. server error HTML), return a generic error wrapper
                return new ApiResponse<ProductMsg>
                {
                    Status = "error",
                    Code = (int)response.StatusCode,
                    Msg = new ProductMsg { Error = $"Network/Parse Error: {response.StatusCode}" }
                };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<ProductMsg>
            {
                Status = "error",
                Code = 999,
                Msg = new ProductMsg { Error = ex.Message }
            };
        }
    }
}
