using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class CommonDateTimeDataTypeConverter : JsonConverter<DateTime>
    {
        private static readonly string[] ValidDateFormats =
        {
        // Date-only formats
        "yyyy-MM-dd",
        "MM/dd/yyyy",
        "yyyy/MM/dd",
        "dd/MM/yyyy",

        // Date + Time (no seconds)
        "yyyy-MM-dd HH:mm",
        "MM/dd/yyyy hh:mm tt",
        "yyyy/MM/dd HH:mm",
        "dd/MM/yyyy HH:mm",
        "yyyy-MM-ddTHH:mm",

        // Date + Time with seconds
        "yyyy-MM-dd HH:mm:ss",
        "MM/dd/yyyy hh:mm:ss tt",
        "yyyy/MM/dd HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",

        // Date + Time with milliseconds
        "yyyy-MM-dd HH:mm:ss.fff",
        "MM/dd/yyyy hh:mm:ss.fff tt",
        "yyyy/MM/dd HH:mm:ss.fff",
        "dd/MM/yyyy HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fff"
    };

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Enter The Valid Date.");
            }

            string dateStr = reader.GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dateStr))
            {
                throw new JsonException("Enter The Valid Date.");
            }

            if (DateTime.TryParseExact(
                dateStr,
                ValidDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate))
            {
                return parsedDate;
            }

            throw new JsonException("Enter The Valid Date.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(
                value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
    }
}
