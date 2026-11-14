namespace VUSYSTimeProWebAPI.Application.Common.Options
{
    /// <summary>
    /// My Application Keys Options
    /// </summary>
    public sealed class MyApplicationKeysOptions
    {
        /// <summary>
        /// Section Name
        /// </summary>
        public const string SectionName = "MyApplicationKeys";

        /// <summary>
        /// API Error Log
        /// </summary>
        public string APIErrorLog { get; set; }

        /// <summary>
        /// Request Logging Enabled
        /// </summary>
        public string RequestLoggingEnabled { get; set; }

        /// <summary>
        /// Global Error Logging Enabled
        /// </summary>
        public string GlobalErrorLoggingEnabled { get; set; }

        /// <summary>
        /// API Global Error Log
        /// </summary>
        public string APIGlobalErrorLog { get; set; }

        /// <summary>
        /// API Request Body Log
        /// </summary>
        public string APIRequestBodyLog { get; set; }

        /// <summary>
        /// Database WarmUp Log
        /// </summary>
        public string DataBaseWarmUpLog { get; set; }

        /// <summary>
        /// Is Database WarmUp Log Enabled
        /// </summary>
        public string IsDataBaseWarmUpLogEnabled { get; set; }

        /// <summary>
        /// Front End User Tabs
        /// </summary>
        public string FrontEndUserTabs { get; set; }

        /// <summary>
        /// Enable Cache
        /// </summary>
        public string EnableCache { get; set; }

        /// <summary>
        /// Cache Time Span In Hours
        /// </summary>
        public string CacheTimeSpanInHours { get; set; }

        /// <summary>
        /// DB Environment
        /// </summary>
        public string DBEnvironment { get; set; }

        /// <summary>
        /// File Uploaded Modified Date Format
        /// </summary>
        public string FileUploadedModifiedDateFormat { get; set; }

        /// <summary>
        /// Saanvi Tech To EmailID
        /// </summary>
        public string SaanviTechToEmailID { get; set; }

        /// <summary>
        /// Saanvi Tech CC EmailID
        /// </summary>
        public string SaanviTechCCEmailID { get; set; }

        /// <summary>
        /// Connection String Key
        /// </summary>
        public string ConnectionStringKey { get; set; }

        /// <summary>
        /// OTP Expiry In Minutes
        /// </summary>
        public string OTPExpiryInMinutes { get; set; }

        /// <summary>
        /// Resend OTP In Minutes
        /// </summary>
        public string ResendOTPInMinutes { get; set; }

        /// <summary>
        /// API Type
        /// </summary>
        public string? APIType { get; set; }

        /// <summary>
        /// Front End Base URL
        /// </summary>
        public string? FrontEndBaseURL { get; set; }

        /// <summary>
        /// Reset Password Expiry In Minutes
        /// </summary>
        public string ResetPasswordLinkExpiryInMinutes { get; set; }

        /// <summary>
        /// Is Enable AI Assistant
        /// </summary>
        public string IsEnableAIAssistant { get; set; }

        /// <summary>
        /// Request DB Script Logging Enabled
        /// </summary>
        public string RequestDBScriptLoggingEnabled { get; set; }

        /// <summary>
        /// API Request DB Script Log
        /// </summary>
        public string APIRequestDBScriptLog { get; set; }

        /// <summary>
        /// Scheduler Login ID
        /// </summary>
        public string SchedulerLoginID { get; set; }

        /// <summary>
        /// Scheduler Login Password
        /// </summary>
        public string SchedulerLoginPassword { get; set; }
    }
}
