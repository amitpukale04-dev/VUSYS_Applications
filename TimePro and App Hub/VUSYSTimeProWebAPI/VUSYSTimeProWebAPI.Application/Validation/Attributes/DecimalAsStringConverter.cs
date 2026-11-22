using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public sealed class DecimalAsStringConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.String => decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture),
                JsonTokenType.Number => reader.GetDecimal(),
                _ => throw new JsonException("Expected string or number for decimal.")
            };

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }

    public sealed class NullableDecimalAsStringConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.String => decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture),
                JsonTokenType.Number => reader.GetDecimal(),
                _ => throw new JsonException("Expected string, number, or null for decimal.")
            };

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
