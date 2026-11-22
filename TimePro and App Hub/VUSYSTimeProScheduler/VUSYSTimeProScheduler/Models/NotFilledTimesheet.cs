namespace VUSYSTimeProScheduler.Models
{
    public class NotFilledTimesheet
    {
    }

    public class NotFilledTimesheetData
    {
        /// <summary>
        /// TIMESHEET_NOT_FILLED_RESPONSE
        /// </summary>
        public List<NotFilledTimesheetItem> TIMESHEET_NOT_FILLED_RESPONSE { get; set; }
    }

    public class NotFilledTimesheetResponse
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
        public NotFilledTimesheetData data { get; set; }
    }

    public class NotFilledTimesheetItem
    {
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }
}
