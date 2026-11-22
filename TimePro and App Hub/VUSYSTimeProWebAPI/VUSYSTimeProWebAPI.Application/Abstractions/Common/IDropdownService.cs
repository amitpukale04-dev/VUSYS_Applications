using System.Data;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Common
{
    public interface IDropdownService
    {
        #region [Custom Dropdown Methods]

        /// <summary>
        /// This Method Is Used To Get Approver Count.
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> GetApproverCountAsync(int UserID, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check For Password Link Available.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> CheckForPasswordLinkAvailable(string EmailID, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Gender Name.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetGenderNameAsync(string EmployeeCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Gender Name.
        /// </summary>
        /// <param name="GenderID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetGenderNameAsync(int GenderID, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get User List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetUserListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Role List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetRoleListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get User And Role List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetUserAndRoleListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Gender List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetGenderListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Country List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetCountryListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get State List.
        /// </summary>
        /// <param name="CountryID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetStateListAsync(int CountryID, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Type List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeTypeListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Project List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Project List Based on Employee Code.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectListBasedOnEmployeeCodeAsync(string EmployeeCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get All Project List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectListWithAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get All Client List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetClientListWithAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Project Status List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectStatusListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Project Activity List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectActivityListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Project Activity List For On Leave Async.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectActivityListForOnLeaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employees Working On Project With All List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> EmployeesWorkingOnProjectWithAllListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Year List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetYearListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Month List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetMonthListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Leave Type List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetLeaveTypeListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Available Leave Type List For AI.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<AILeaveTypeOptionM>> GetAvailableLeaveTypesForAIAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Leave Balance Based on Leave Type, Employee Code and Leave Year.
        /// </summary>
        /// <param name="LeaveTypeID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="LeaveYear"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetLeaveBalanceBasedOnLeaveTypeAndEmployeeCode(int LeaveTypeID, string EmployeeCode, int LeaveYear, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Leave Approve Reject Status List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetLeaveApproveRejectStatusListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Leave Status List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetLeaveStatusListAsync(CancellationToken cancellationToken = default);

        #endregion [Custom Dropdown Methods]

        #region [Custom Methods]

        /// <summary>
        /// This Method Is Used To Get Approver Details.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<UsersM>> GetApproverDetails(CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get User Details By Employee Code.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<UsersM>> GetUserDetailsByEmployeeCode(string EmployeeCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check Timesheet Approved.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TaskDate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CheckTimesheetApprovedAsync(string EmployeeCode, DateTime? TaskDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check Timesheet Approved.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CheckTimesheetApprovedAsync(string EmployeeCode, string TimesheetMonth, int TimesheetYear, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check Timesheet Sent For Approval.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TaskDate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CheckTimesheetSentForApprovalAsync(string EmployeeCode, DateTime? TaskDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check Timesheet Sent For Approval.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CheckTimesheetSentForApprovalAsync(string EmployeeCode, string TimesheetMonth, int TimesheetYear, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check Current Month Timesheet Closed Status.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CheckCurrentMonthTimesheetClosedStatusAsync(string EmployeeCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check Leave Request Status.
        /// </summary>
        /// <param name="LeaveRequestID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> CheckLeaveRequestStatusAsync(int LeaveRequestID, CancellationToken cancellationToken = default);

        #endregion [Custom Methods]

        #region [Employee Type]

        /// <summary>
        /// This Method Is Used To Save And Update Employee Type Details.
        /// </summary>
        /// <param name="_objEmployeeTypeM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveAndUpdateEmployeeTypeDetails(EmployeeTypeM _objEmployeeTypeM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Type Data.
        /// </summary>
        /// <param name="_objEmployeeTypeDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeTypeDataAsync(EmployeeTypeDetailsRequestParam _objEmployeeTypeDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Employee Type]

        #region [Employee Project Mapping]

        /// <summary>
        /// This Method Is Used To Save And Update Employee Project Mapping Details.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveAndUpdateEmployeeProjectMappingDetails(EmployeeProjectMappingM _objEmployeeProjectMappingM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Project Mapping Data.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeProjectMappingDataAsync(EmployeeProjectMappingDetailsRequestParam _objEmployeeProjectMappingDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Employee Project Mapping]
    }
}
