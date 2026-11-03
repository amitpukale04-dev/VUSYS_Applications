namespace VUSYSTimeProWebApp
{
    /// <summary>
    /// My Application Keys Options
    /// </summary>
    public class MyApplicationKeysOptions
    {
        /// <summary>
        /// Error Log
        /// </summary>
        public string ErrorLog { get; set; }

        /// <summary>
        /// App Name
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// DB Type
        /// </summary>
        public string DBType { get; set; }

        /// <summary>
        /// Request DB Script Logging Enabled
        /// </summary>
        public string RequestDBScriptLoggingEnabled { get; set; }

        /// <summary>
        /// Request DB Script Log
        /// </summary>
        public string RequestDBScriptLog { get; set; }

        /// <summary>
        /// Reset Password Expiry In Minutes
        /// </summary>
        public string ResetPasswordLinkExpiryInMinutes { get; set; }

        /// <summary>
        /// OTP Expiry In Minutes
        /// </summary>
        public string OTPExpiryInMinutes { get; set; }

        /// <summary>
        /// Resend OTP In Minutes
        /// </summary>
        public int ResendOTPInMinutes { get; set; }

        /// <summary>
        /// Connection String Key
        /// </summary>
        public string ConnectionStringKey { get; set; }

        /// <summary>
        /// Footer Content
        /// </summary>
        public string FooterContent { get; set; }

        /// <summary>
        /// Environment
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        public string Version { get; set; }
    }
}
