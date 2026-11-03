using CommonLib;

namespace VUSYSTimeProScheduler
{
    public static class CommonHelper
    {
        static CommonHelper()
        {
            Helper.EncryptDecryptKey = "VUSYSTimePro@123456$Key@1912$@^!";
            Helper.DefaultValue = "VUSYSTimeProWebApp@123456$";
        }

        /// <summary>
        /// EMAIL_DISPLAY_NAME
        /// </summary>
        public static readonly string EMAIL_DISPLAY_NAME = "Time-Pro Notification";

        /// <summary>
        /// Key Option
        /// </summary>
        public static MyApplicationKeysOptions KeyOption { get; set; } = new MyApplicationKeysOptions();

        /// <summary>
        /// EMPLOLYEE_TIMESHEET_REMINDER_SCHEDULER_NOTIFICATION_JOB
        /// </summary>
        public const string EMPLOLYEE_TIMESHEET_REMINDER_SCHEDULER_NOTIFICATION_JOB = "EmployeeTimesheetReminderSchedulerNotification";

        /// <summary>
        /// EMPLOLYEE_LEAVE_AND_HOLIDAY_TIMESHEET_SYNC_SCHEDULER_JOB
        /// </summary>
        public const string EMPLOLYEE_LEAVE_AND_HOLIDAY_TIMESHEET_SYNC_SCHEDULER_JOB = "EmployeeLeaveAndHolidayTimesheetSyncScheduler";

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

        /// <summary>
        /// This Method Is Used To Write Error To Both Global and Job Error Logs.
        /// </summary>
        /// <param name="JobName"></param>
        /// <param name="ModuleName"></param>
        /// <param name="MethodName"></param>
        /// <param name="Message"></param>
        public static void WriteErrorLog(string JobName, string ModuleName, string MethodName, string Message)
        {
            try
            {
                // Global Error Log
                Helper.SchedulerWriteLog(KeyOption.ErrorLog, KeyOption.LogFileName, ModuleName, MethodName, Message);

                // Job Error Log
                JobLogConfiguration job = KeyOption.GetJob(JobName);
                Helper.SchedulerWriteLog(job.ErrorLog, job.LogFileName, ModuleName, MethodName, Message);
            }
            catch
            {
                // Never throw exception from logging.
                // Ignore any logging failures.
            }
        }

        /// <summary>
        /// This Method Is Used To Write Notification To Both Global and Job notification Logs.
        /// </summary>
        /// <param name="JobName"></param>
        /// <param name="ModuleName"></param>
        /// <param name="MethodName"></param>
        /// <param name="Message"></param>
        public static void WriteNotificationLog(string JobName, string ModuleName, string MethodName, string Message)
        {
            try
            {
                // Global Notification Log
                Helper.SchedulerWriteLog(KeyOption.NotificationLog, KeyOption.LogFileName, ModuleName, MethodName, Message);

                // Job Notification Log
                JobLogConfiguration job = KeyOption.GetJob(JobName);
                Helper.SchedulerWriteLog(job.NotificationLog, job.LogFileName, ModuleName, MethodName, Message);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
