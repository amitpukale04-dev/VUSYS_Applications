using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.LeaveManagement
{
    public interface ILeaveManagementService
    {
        #region [Employee Leave Balance]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Leave Balance Details.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateEmployeeLeaveBalanceDetails(EmployeeLeaveBalanceM _objEmployeeLeaveBalanceM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Leave Balance Detail Or List.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeLeaveBalanceDetailOrList(EmployeeLeaveBalanceDetailsRequestParam _objEmployeeLeaveBalanceDetailsRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Save Employee Leave Balance Initialize.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceInitializeRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveEmployeeLeaveBalanceInitialize(EmployeeLeaveBalanceInitializeRequestParam _objEmployeeLeaveBalanceInitializeRequestParam, CancellationToken cancellationToken = default);

        #endregion [Employee Leave Balance]

        #region [Leave Application]

        /// <summary>
        /// This Method Is Used To Save Or Update Leave Application Details.
        /// </summary>
        /// <param name="_objLeaveApplicationM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateLeaveRequestDetails(LeaveApplicationM _objLeaveApplicationM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Leave Request Details Or List.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetLeaveRequestDetailOrList(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Pending Leave Request List.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetPendingLeaveRequestList(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check For Leave Request Approval Status.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> CheckLeaveRequestApprovalStatus(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Approve Leave Request.
        /// </summary>
        /// <param name="_objLeaveApplicationApproveRejectCancelRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> ApproveLeaveRequest(LeaveApplicationApproveRejectCancelRequestParam _objLeaveApplicationApproveRejectCancelRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Reject Leave Request.
        /// </summary>
        /// <param name="_objLeaveApplicationApproveRejectCancelRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> RejectLeaveRequest(LeaveApplicationApproveRejectCancelRequestParam _objLeaveApplicationApproveRejectCancelRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Cancel Leave Request.
        /// </summary>
        /// <param name="_objLeaveApplicationApproveRejectCancelRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> CancelLeaveRequest(LeaveApplicationApproveRejectCancelRequestParam _objLeaveApplicationApproveRejectCancelRequestParam, CancellationToken cancellationToken = default);

        #endregion [Leave Application]
    }
}