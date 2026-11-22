using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Employee
{
    public interface IEmployeeService
    {
        #region [Employee Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Details.
        /// </summary>
        /// <param name="_objEmployeeM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateEmployeeDetails(EmployeeM _objEmployeeM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Detail Or List.
        /// </summary>
        /// <param name="_objEmployeeDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeDetailOrList(EmployeeDetailsRequestParam _objEmployeeDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Employee Master]

        #region [Employee Type Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Type Details.
        /// </summary>
        /// <param name="_objEmployeeTypeM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateEmployeeTypeDetails(EmployeeTypeM _objEmployeeTypeM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Type Detail Or List.
        /// </summary>
        /// <param name="_objEmployeeTypeDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeTypeDetailOrList(EmployeeTypeDetailsRequestParam _objEmployeeTypeDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Employee Type Master]
    }
}