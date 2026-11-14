namespace DatabaseBackupScheduler
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
        /// Database Name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// DB Backup Folder Path
        /// </summary>
        public string DBBackupFolderPath { get; set; }

        /// <summary>
        /// DB Backup File Name
        /// </summary>
        public string DBBackupFileName { get; set; }

        /// <summary>
        /// Connection String Key
        /// </summary>
        public string ConnectionStringKey { get; set; }
    }
}
