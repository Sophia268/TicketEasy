using System.Text.Json.Serialization;

namespace TicketEasy.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public T? Msg { get; set; }
}

public class ProductMsg
{
    [JsonPropertyName("ProductID")]
    public string? ProductId { get; set; }

    [JsonPropertyName("ProductName")]
    public string? ProductName { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    // For ticket verification
    [JsonPropertyName("expiredat")]
    public string? ExpiredAt { get; set; }
    
    [JsonPropertyName("checkedat")]
    public string? CheckedAt { get; set; }
}
