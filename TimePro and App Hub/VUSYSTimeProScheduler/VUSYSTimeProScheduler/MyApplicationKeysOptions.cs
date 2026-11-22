namespace VUSYSTimeProScheduler
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
        /// Notification Log
        /// </summary>
        public string NotificationLog { get; set; }

        /// <summary>
        /// Log File Name
        /// </summary>
        public string LogFileName { get; set; }

        /// <summary>
        /// API Base URL
        /// </summary>
        public string ApiBaseUrl { get; set; }

        /// <summary>
        /// Login ID
        /// </summary>
        public string LoginID { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Jobs
        /// </summary>
        public Dictionary<string, JobLogConfiguration> Jobs { get; set; } = new();

        /// <summary>
        /// Returns the configuration for the specified job.
        /// </summary>
        /// <param name="jobName">Job Name</param>
        /// <returns>Job Configuration</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public JobLogConfiguration GetJob(string jobName)
        {
            if (Jobs != null && Jobs.TryGetValue(jobName, out var job))
                return job;

            throw new KeyNotFoundException($"Job '{jobName}' is not configured in appsettings.json.");
        }
    }

    public class JobLogConfiguration
    {
        /// <summary>
        /// Notification Log
        /// </summary>
        public string NotificationLog { get; set; }

        /// <summary>
        /// Error Log
        /// </summary>
        public string ErrorLog { get; set; }

        /// <summary>
        /// Log File Name
        /// </summary>
        public string LogFileName { get; set; }
    }
}
