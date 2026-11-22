using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Reports
{
    public interface IReportService
    {
        #region [Timesheet Report]

        /// <summary>
        /// This Method Is Used To Get Timesheet Report List.
        /// </summary>
        /// <param name="_objTimesheetReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetTimesheetReportList(TimesheetReportRequestParam _objTimesheetReportRequestParam, CancellationToken cancellationToken = default);

        #endregion [Timesheet Report]

        #region [Project Allocated Consumed Report]

        /// <summary>
        /// This Method Is Used To Get Project Allocated Consumed Report List.
        /// </summary>
        /// <param name="_objProjectAllocatedConsumedReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectAllocatedConsumedReportList(ProjectAllocatedConsumedReportRequestParam _objProjectAllocatedConsumedReportRequestParam, CancellationToken cancellationToken = default);

        #endregion [Project Allocated Consumed Report]

        #region [Employee Leave Report]

        /// <summary>
        /// This Method Is Used To Get Employee Leave Report List.
        /// </summary>
        /// <param name="_objEmployeeLeaveReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeLeaveReportList(EmployeeLeaveReportRequestParam _objEmployeeLeaveReportRequestParam, CancellationToken cancellationToken = default);

        #endregion [Employee Leave Report]
    }
}