using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TicketEasy.Services;

public class TicketService
{
    private readonly HttpClient _httpClient;

    public TicketService()
    {
        _httpClient = new HttpClient();
        // Set a reasonable timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<bool> CheckConnectivityAsync(string productId)
    {
        try
        {
            // The user said: Input productId, click connect button, call API... then background judges if connected.
            // Using a dummy code "PING" to check connectivity if allowed, or maybe just checking if the server is reachable.
            // Based on prompt (4): "call API: https://www.80fafa.com/api/checkticket/{productiId}/{codeinfo}"
            // We'll use "CONNECT_CHECK" as the codeinfo for the connection test.
            
            string url = $"https://www.80fafa.com/api/checkticket/{productId}/CONNECT_CHECK";
            var response = await _httpClient.GetAsync(url);
            
            // We assume 200 OK means connected. The API might return specific JSON, but for now 200 is a good check.
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
            // (5) Whether scanning QR or manual input, call .../{productiId}/{codeinfo}
            // If manual input, other parameters are null (handled by caller logic, here we just send the codeinfo string).
            // If codeInfo is a JSON string from QR, it is passed directly? 
            // The prompt says: "If it is scanning QR, pass complete restful api parameters" -> this implies the codeinfo in the URL *is* the parameter.
            // But codeInfo is a JSON string. Putting JSON in URL path is risky. 
            // Maybe the prompt means the {codeinfo} part IS the code?
            // "The QR code information is a json string: {"code":"xxxx", ...}"
            // "Call .../{productiId}/{codeinfo}"
            // If I put the whole JSON in the URL, it needs encoding.
            // "If it is manual input code, then set other parameters to null and pass." -> This suggests the API might expect a structured object, 
            // but the URL pattern looks like a GET with path parameters. 
            // Let's assume we pass the 'code' value if manual, or the full JSON if scanned?
            // "If scanning QR, pass complete restful api parameters" -> This phrasing is tricky.
            // Maybe it means: Pass the 'code' from the JSON?
            // Or pass the JSON string encoded?
            // Let's assume we pass the JSON string (URL encoded) if scanned, and constructed JSON (with nulls) if manual?
            // Or maybe the {codeinfo} is just the "code" field?
            // Let's re-read (2): QR Info is JSON.
            // (5): "If scan QR, pass complete restful api parameters".
            // "If manual input code, set other parameters to null".
            // This strongly suggests {codeinfo} is a serialized JSON object.
            
            string payload = codeInfo ?? "";
            // Ensure it's URL encoded
            string encodedPayload = System.Net.WebUtility.UrlEncode(payload);

            string url = $"https://www.80fafa.com/api/checkticket/{productId}/{encodedPayload}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                // "If yes, return OK"
                // We'll return the content.
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
