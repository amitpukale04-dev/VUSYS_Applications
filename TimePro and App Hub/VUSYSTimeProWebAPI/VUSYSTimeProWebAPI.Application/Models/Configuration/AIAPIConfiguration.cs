namespace VUSYSTimeProWebAPI.Application.Models.Configuration
{
    public class AIAPIConfiguration
    {
        /// <summary>
        /// AI Provider Name
        /// </summary>
        public string AIProviderName { get; set; }

        /// <summary>
        /// AI API Key
        /// </summary>
        public string AIAPIKey { get; set; }

        /// <summary>
        /// AI Models
        /// </summary>
        public string AIModels { get; set; }

        /// <summary>
        /// AI API URL
        /// </summary>
        public string AIAPIURL { get; set; }

        /// <summary>
        /// Error Message
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
