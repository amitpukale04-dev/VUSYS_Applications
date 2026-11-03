using CommonLib;
using VUSYSTimeProScheduler.Repository;

namespace VUSYSTimeProScheduler
{
    public class MainApp
    {
        private readonly ITimeProSchedulerRepository _TimeProSchedulerRepository;

        /// <summary>
        /// MainApp Class Constructor
        /// </summary>
        /// <param name="TimeProSchedulerRepository"></param>
        public MainApp(ITimeProSchedulerRepository TimeProSchedulerRepository)
        {
            _TimeProSchedulerRepository = TimeProSchedulerRepository;
        }

        public async Task Run(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0)
                {
                    Helper.SchedulerWriteLog(CommonHelper.KeyOption.NotificationLog, CommonHelper.KeyOption.LogFileName, "Timepro Notification Scheduler", "MainApp.Run()", "No argument provided.");
                    return;
                }

                string jobType = args[0]?.Trim() ?? string.Empty;
                switch (jobType)
                {
                    case CommonHelper.EMPLOLYEE_TIMESHEET_REMINDER_SCHEDULER_NOTIFICATION_JOB:
                        await SendEmployeeTimesheetReminderSchedulerEmailNotification();
                        break;

                    case CommonHelper.EMPLOLYEE_LEAVE_AND_HOLIDAY_TIMESHEET_SYNC_SCHEDULER_JOB:
                        await SyncEmployeeLeaveAndHolidayTimesheetScheduler();
                        break;

                    default:
                        Helper.SchedulerWriteLog(CommonHelper.KeyOption.NotificationLog, CommonHelper.KeyOption.LogFileName, "Timepro Notification Scheduler", "MainApp.Run()", $"Invalid argument provided: {jobType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Helper.SchedulerWriteLog(CommonHelper.KeyOption.NotificationLog, CommonHelper.KeyOption.LogFileName, "Timepro Notification Scheduler", "MainApp.Run()", ex.ToString());
            }
        }

        /// <summary>
        /// This Method Is Used Send Employee Timesheet Reminder Scheduler Email Notification.
        /// </summary>
        /// <returns></returns>
        public async Task SendEmployeeTimesheetReminderSchedulerEmailNotification()
        {
            string JobName = CommonHelper.EMPLOLYEE_TIMESHEET_REMINDER_SCHEDULER_NOTIFICATION_JOB;
            CommonHelper.WriteNotificationLog(JobName, "Employee Timesheet Reminder Scheduler", "MainApp.SendEmployeeTimesheetReminderSchedulerEmailNotification()", "- Employee Timesheet Reminder Notification Scheduler Service Started");
            // Call method in repository
            string ReturnMessage = await _TimeProSchedulerRepository.GetEmployeesTimesheetNotFilledListAndSendEmailNotification();
            CommonHelper.WriteNotificationLog(JobName, "Employee Timesheet Reminder Scheduler", "MainApp.SendEmployeeTimesheetReminderSchedulerEmailNotification()", ReturnMessage);
            CommonHelper.WriteNotificationLog(JobName, "Employee Timesheet Reminder Scheduler", "MainApp.SendEmployeeTimesheetReminderSchedulerEmailNotification()", "- Employee Timesheet Reminder Notification Scheduler Service Ended");
        }

        /// <summary>
        /// This Method Is Used Sync Employee Leave And Holiday Timesheet Scheduler.
        /// </summary>
        /// <returns></returns>
        public async Task SyncEmployeeLeaveAndHolidayTimesheetScheduler()
        {
            string JobName = CommonHelper.EMPLOLYEE_LEAVE_AND_HOLIDAY_TIMESHEET_SYNC_SCHEDULER_JOB;
            CommonHelper.WriteNotificationLog(JobName, "Sync Employee Leave and Holiday Timesheet Scheduler", "MainApp.SyncEmployeeLeaveAndHolidayTimesheetScheduler()", "- Sync Employee Leave and Holiday Timesheet Scheduler Service Started");
            // Call method in repository
            string ReturnMessage = await _TimeProSchedulerRepository.GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet();
            CommonHelper.WriteNotificationLog(JobName, "Sync Employee Leave and Holiday Timesheet Scheduler", "MainApp.SyncEmployeeLeaveAndHolidayTimesheetScheduler()", ReturnMessage);
            CommonHelper.WriteNotificationLog(JobName, "Sync Employee Leave and Holiday Timesheet Scheduler", "MainApp.SyncEmployeeLeaveAndHolidayTimesheetScheduler()", "- Sync Employee Leave and Holiday Timesheet Scheduler Service Ended");
        }
    }
}
