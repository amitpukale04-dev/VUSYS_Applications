namespace DatabaseBackupScheduler
{
    public static class CommonHelper
    {
        /// <summary>
        /// Key Option
        /// </summary>
        public static MyApplicationKeysOptions KeyOption { get; set; } = new MyApplicationKeysOptions();

        /// <summary>
        /// This Method Is Used To Write Log For Scheduler.
        /// </summary>
        /// <param name="NotificationLogPath"></param>
        /// <param name="LogFileName"></param>
        /// <param name="Text"></param>
        /// <param name="Location"></param>
        /// <param name="Message"></param>
        public static void SchedulerWriteLog(string NotificationLogPath, string LogFileName, string Text, string Location, string Message)
        {
            try
            {
                if (!Directory.Exists(NotificationLogPath))
                {
                    Directory.CreateDirectory(NotificationLogPath);
                }
                string spath = Path.Combine(NotificationLogPath, LogFileName);
                Text += "- " + Location + "- " + Message;
                using (StreamWriter writer = new StreamWriter(spath, true))
                {
                    writer.WriteLine(("[" + (DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss") + ("] " + Text))));
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// This Method Is Used To Get Actual Path.
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public static string GetActualPath(string folderName)
        {
            string rootPath = Directory.GetCurrentDirectory(); // or AppContext.BaseDirectory
            string fullPath = Path.Combine(rootPath, folderName);

            // Ensure directory exists
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            return fullPath;
        }
    }
}
