using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VUSYSTimeProScheduler.Models
{
    public class UserLogin
    {
        /// <summary>
        /// Login ID
        /// </summary>
        [DefaultValue("")]
        public string loginID { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [DefaultValue("")]
        public string password { get; set; }
    }

    public class UserLoginResponse
    {
        /// <summary>
        /// Message
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        public UserLoginData data { get; set; }
    }

    public class UserLoginItem
    {
        /// <summary>
        /// Access Token
        /// </summary>
        public string accessToken { get; set; }

        /// <summary>
        /// Token Expiration In Minutes
        /// </summary>
        public int TokenExpirationInMinutes { get; set; }

        /// <summary>
        /// Login Date Time
        /// </summary>
        [JsonPropertyName("login_date_time")]
        public string LoginDateTime { get; set; }

        /// <summary>
        /// Token Generated Date Time
        /// </summary>
        [JsonPropertyName("token_generated_date_time")]
        [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
        public DateTimeOffset? TokenGeneratedDateTime { get; set; }

        /// <summary>
        /// Token Expiry Date Time
        /// </summary>
        [JsonPropertyName("token_expiry_date_time")]
        [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
        public DateTimeOffset? TokenExpiryDateTime { get; set; }
    }

    public class UserLoginData
    {
        /// <summary>
        /// Login Response
        /// </summary>
        public List<UserLoginItem> USER_LOGIN_DETAILS { get; set; }
    }

    public sealed class FlexibleDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        private static readonly string[] Formats =
        {
        "dd-MMM-yyyy HH:mm:ss",          // 13-Feb-2026 14:55:45
        "dd-MMM-yyyy H:mm:ss",           // just in case
        "dd-MMM-yyyy hh:mm:ss tt",       // if it ever comes with AM/PM
        "MM/dd/yyyy hh:mm:ss tt",        // 02/13/2026 01:55:45 PM (your login_date_time pattern)
        "yyyy-MM-ddTHH:mm:ss",           // ISO without offset
        "yyyy-MM-ddTHH:mm:ss.fff",       // ISO with milliseconds
        "yyyy-MM-ddTHH:mm:ssK",          // ISO with timezone
        "yyyy-MM-ddTHH:mm:ss.fffK"       // ISO with ms + timezone
    };

        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;

                // First try the exact known formats
                if (DateTimeOffset.TryParseExact(s, Formats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var dto))
                    return dto;

                // Fallback: Try normal parsing (handles many ISO variants)
                if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dto))
                    return dto;

                // Last fallback: try DateTime -> DateTimeOffset
                if (DateTime.TryParseExact(s, Formats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var dt))
                    return new DateTimeOffset(dt);

                if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt))
                    return new DateTimeOffset(dt);

                return null;
            }

            // If it comes as number (unix seconds) later, you can add handling here.
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.ToString("O", CultureInfo.InvariantCulture));
        }
    }
}
