namespace VUSYSTimeProWebAPI.Application.Common.Options
{
    public sealed class GeminiAIKeysOptions
    {
        /// <summary>
        /// Section Name
        /// </summary>
        public const string SectionName = "GeminiAIKeys";

        /// <summary>
        /// Gemini AI ApiKey
        /// </summary>
        public string GeminiAIApiKey { get; set; }

        /// <summary>
        /// Gemini AI Model
        /// </summary>
        public string GeminiAIModel { get; set; }

        /// <summary>
        /// Gemini AI Url
        /// </summary>
        public string GeminiAIUrl { get; set; }
    }
}
