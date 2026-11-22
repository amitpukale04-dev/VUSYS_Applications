namespace VUSYSTimeProScheduler.Repository
{
    public interface ITimeProSchedulerRepository
    {
        /// <summary>
        /// This Method Is Used To Get Employee Timesheet Not Filled List and Send Email Notification.
        /// </summary>
        /// <returns></returns>
        Task<string> GetEmployeesTimesheetNotFilledListAndSendEmailNotification();

        /// <summary>
        /// This Method Is Used To Get Employee Leave Holiday and Sync To Project Timesheet.
        /// </summary>
        /// <returns></returns>
        Task<string> GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet();
    }
}
