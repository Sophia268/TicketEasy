using System.Text.Json.Serialization;

namespace TicketEasy.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = "";

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class TicketData
{
    public string? Secret { get; set; }
    public string? OrderNo { get; set; }
    public string? ProductId { get; set; }
    public string? ProductHashcode { get; set; }
    public string? Category { get; set; }
    public string? PeriodType { get; set; }
    public string? CreateTime { get; set; }
    public string? ExpiredTime { get; set; }
}
