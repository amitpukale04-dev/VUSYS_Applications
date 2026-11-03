using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Timesheet
{
    public interface ITimesheetService
    {
        #region [Timesheet]

        /// <summary>
        /// This Method Is Used To Get Timesheet Submission List.
        /// </summary>
        /// <param name="_objTimesheetSubmissionRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetTimesheetSubmissionList(TimesheetSubmissionRequestParam _objTimesheetSubmissionRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Log Timesheet List.
        /// </summary>
        /// <param name="_objLogTimesheetListRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetLogTimesheetList(LogTimesheetListRequestParam _objLogTimesheetListRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Timesheet Details.
        /// </summary>
        /// <param name="_objGetTimesheetDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetTimesheetDetails(GetTimesheetDetailsRequestParam _objGetTimesheetDetailsRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Save Update Timesheet Details.
        /// </summary>
        /// <param name="lstProjectTimesheet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateTimesheetDetails(List<ProjectTimesheet> lstProjectTimesheet, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Send Timesheet Submission Reminder Notification To Employee.
        /// </summary>
        /// <param name="_objTimesheetReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SendTimesheetSubmissionReminderNotificationToEmployee(TimesheetReportRequestParam _objTimesheetReportRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get or Export Employee Timesheet List.
        /// </summary>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetOrExportEmployeeTimesheetList(ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check Timesheet Approval Status.
        /// </summary>
        /// <param name="_objCheckTimesheetApprovalStatusRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> CheckTimesheetApprovalStatus(CheckTimesheetApprovalStatusRequestParam _objCheckTimesheetApprovalStatusRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Submit Timesheet Details For Approval.
        /// </summary>
        /// <param name="_objTimesheetSubmitForApprovalRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SubmitTimesheetDetailsForApproval(TimesheetSubmitForApprovalRequestParam _objTimesheetSubmitForApprovalRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Approve Or Reject Timesheet.
        /// </summary>
        /// <param name="_objTimesheetApproveRejectRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> ApproveOrRejectTimesheet(TimesheetApproveRejectRequestParam _objTimesheetApproveRejectRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Timesheet Not Filled List and Send Email Notification.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeesTimesheetNotFilledListAndSendEmailNotification(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employees Leave, Holiday and Sync To Project Timesheet.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet(CancellationToken cancellationToken = default);

        #endregion [Timesheet]
    }
}