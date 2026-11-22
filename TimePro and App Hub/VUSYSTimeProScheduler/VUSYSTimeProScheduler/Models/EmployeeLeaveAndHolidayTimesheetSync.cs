namespace VUSYSTimeProScheduler.Models
{
    public class EmployeeLeaveAndHolidayTimesheetSync
    {
    }

    public class EmployeeLeaveAndHolidayTimesheetSyncData
    {
        /// <summary>
        /// EMPLOYEE_LEAVE_HOLIDAY_SYNC_PROJECT_TIMESHEET_RESPONSE
        /// </summary>
        public List<EmployeeLeaveAndHolidayTimesheetSyncItem> EMPLOYEE_LEAVE_HOLIDAY_SYNC_PROJECT_TIMESHEET_RESPONSE { get; set; }
    }

    public class EmployeeLeaveAndHolidayTimesheetSyncResponse
    {
        /// <summary>
        /// Message
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        public EmployeeLeaveAndHolidayTimesheetSyncData data { get; set; }
    }

    public class EmployeeLeaveAndHolidayTimesheetSyncItem
    {
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }
}
