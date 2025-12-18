using System;
using System.Globalization;
using System.Text.Json;
using TicketEasy.Models;

namespace TicketEasy.Services
{
    public class RegistrationService
    {
        public bool TryDecodeRegistrationCode(string regCode, out RegistrationInfo? info, out string error)
        {
            info = null;
            error = string.Empty;

            string decoded = string.Empty;
            regCode = regCode.Trim();
            try
            {
                decoded = EncryptService.DecryptText(regCode);
            }
            catch
            {
                error = "Base64 decoding failed or invalid key";
                return false;
            }

            try
            {
                var text = decoded.Trim();
                if (text.StartsWith("{"))
                {
                    var json = JsonSerializer.Deserialize<RegistrationInfo>(text, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (json is null) throw new Exception("JSON is empty");
                    info = json;
                    // Recalculate ExpiredTime if based on PeriodType logic
                    // Legacy code: info.ExpiredTime = info.CreateTime.AddMonths(info.PeriodType);
                    // Check if PeriodType > 0 and ExpiredTime is not set correctly?
                    // Assuming ExpiredTime is already set if JSON, but legacy code overwrites it?
                    // Legacy: info.ExpiredTime = info.CreateTime.AddMonths(info.PeriodType);
                    if (info.PeriodType > 0)
                    {
                        info.ExpiredTime = info.CreateTime.AddMonths(info.PeriodType);
                    }

                    return ValidateInfo(info, out error);
                }
                else
                {
                    var parts = text.Split('|');
                    if (parts.Length < 4) throw new Exception($"Insufficient fields: {text}");
                    if (!DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var create))
                        throw new Exception($"Invalid create time: {parts[2]}");
                    if (!DateTime.TryParse(parts[3], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expire))
                        throw new Exception($"Invalid expire time: {parts[3]}");
                    if (!int.TryParse(parts[1], out var periodType))
                        throw new Exception($"Invalid period type: {parts[1]}");

                    info = new RegistrationInfo
                    {
                        MachineID = parts[0],
                        PeriodType = periodType,
                        CreateTime = create,
                        ExpiredTime = expire
                    };
                    return ValidateInfo(info, out error);
                }
            }
            catch (Exception ex)
            {
                error = $"Parsing failed: {ex.Message}";
                return false;
            }
        }

        private bool ValidateInfo(RegistrationInfo info, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(info.MachineID)) { error = "Machine ID missing"; return false; }
            if (info.ExpiredTime <= info.CreateTime) { error = "Expired time is invalid"; return false; }
            return true;
        }
    }
}
