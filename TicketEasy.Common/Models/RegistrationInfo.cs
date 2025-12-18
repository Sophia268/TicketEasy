using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TicketEasy.Models
{
    public class RegistrationInfo
    {
        [JsonPropertyName("machineId")]
        public string MachineID { get; set; } = string.Empty;

        [JsonPropertyName("period")]
        public int PeriodType { get; set; }

        [JsonPropertyName("ts")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime CreateTime { get; set; }

        public DateTime ExpiredTime { get; set; }
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format = "yyyy-MM-dd HH:mm:ss";
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str)) return default;
            // Try parse exact, fallback to standard if needed
            if (DateTime.TryParseExact(str, _format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt))
            {
                return dt;
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }
}
