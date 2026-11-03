using Microsoft.AspNetCore.DataProtection;
using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface ITimeSheet
    {
        /// <summary>
        /// This Method is Used to Get Timesheet List.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="FromTaskDate"></param>
        /// <param name="ToTaskDate"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<ProjectTimesheet>> GetTimesheetList(int ProjectTimeSheetID, string EmployeeCode, string FromTaskDate, string ToTaskDate, string Type);

        /// <summary>
        /// This Method is Used to Save Timesheet Details.
        /// </summary>
        /// <param name="lstProjectTimesheet"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        string SaveTimesheet(List<ProjectTimesheet> lstProjectTimesheet, ref string Status);

        /// <summary>
        /// This Method is Used to Get Timesheet Submission List.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<ProjectTimesheetSubmissionApproveRejectReportList>> GetTimesheetSubmissionList(string EmployeeCode, string TimesheetMonth, int TimesheetYear, string Type);

        /// <summary>
        /// This Method is Used to Submit Timesheet Details For Approval.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="session"></param>
        /// <param name="protector"></param>
        /// <param name="db"></param>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="ApproveStatus"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        string SubmitTimesheetDetailsForApproval(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, ISession session, IDataProtector protector, AppDbContext db, int ProjectTimeSheetID, string TimesheetMonth, int TimesheetYear, string ApproveStatus, out string Status, out string Message);

        /// <summary>
        /// This Method is Used to Send Timesheet Submission Reminder Notification To Employee.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        string SendTimesheetSubmissionReminderNotificationToEmployee(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, AppDbContext db, string EmployeeCode, string TimesheetMonth, int TimesheetYear, out string Status, out string Message);

        /// <summary>
        /// This Method is Used to Check Timesheet Approval Status.
        /// </summary>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        string CheckTimesheetApprovalStatus(ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, out string Status, out string Message);

        /// <summary>
        /// This Method is Used to Approve Reject Timesheet.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="session"></param>
        /// <param name="db"></param>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        string ApproveRejectTimesheet(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, ISession session, AppDbContext db, ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, out string Status, out string Message);

        /// <summary>
        /// This Method is Used to Download Timesheet.
        /// </summary>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="ParamType"></param>
        /// <returns></returns>
        Task<List<ProjectTimesheetDownloadList>> DownloadTimeSheet(ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, string ParamType);

        /// <summary>
        /// This Method is Used to Get Project Allocated Consumed Report List.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <returns></returns>
        Task<List<ProjectAllocatedConsumedReportList>> GetProjectAllocatedConsumedReportList(int ProjectTimeSheetID);

        /// <summary>
        /// This Method is Used to Get Leave Report List.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<LeaveReportList>> GetLeaveReportList(string EmployeeCode, string TimesheetMonth, int TimesheetYear, string Type);
    }
}
