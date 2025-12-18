using System.Text.Json.Serialization;
using TicketEasy.Models;

namespace TicketEasy.Services;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
[JsonSerializable(typeof(ApiResponse<TicketData>))]
[JsonSerializable(typeof(AppConfig))]
public partial class TicketJsonContext : JsonSerializerContext
{
}
