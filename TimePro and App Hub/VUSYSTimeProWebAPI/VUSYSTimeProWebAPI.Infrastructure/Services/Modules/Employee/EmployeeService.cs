using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Employee;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Employee
{
    public sealed class EmployeeService : IEmployeeService
    {
        private const string EmployeeServiceLogClassName = "EmployeeService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly IDropdownService _DropdownSvc;

        /// <summary>
        /// Employee Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="DropdownSvc"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EmployeeService(SQLDBHelper SQLDBHelper, IDropdownService DropdownSvc)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
        }

        #region [Employee Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Details.
        /// </summary>
        /// <param name="_objEmployeeM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> SaveUpdateEmployeeDetails(EmployeeM _objEmployeeM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "EMPLOYEE_DETAILS_RESPONSE";
            try
            {
                _objEmployeeM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objEmployeeM, StoredProcedure.EMPLOYEE_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{EmployeeServiceLogClassName}.SaveUpdateEmployeeDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Detail Or List.
        /// </summary>
        /// <param name="_objEmployeeDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmployeeDetailOrList(EmployeeDetailsRequestParam _objEmployeeDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objEmployeeDetailsRequestParam.Id switch
                {
                    0 => "EMPLOYEE_LIST_RESPONSE",
                    _ => "EMPLOYEE_DETAILS_RESPONSE"
                };
                ParamType = _objEmployeeDetailsRequestParam.Id switch
                {
                    0 => "GetAllEmployeeList",
                    _ => "GetEmployeeDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objEmployeeDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{EmployeeServiceLogClassName}.GetEmployeeDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Employee Master]

        #region [Employee Type Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Type Details.
        /// </summary>
        /// <param name="_objEmployeeTypeM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> SaveUpdateEmployeeTypeDetails(EmployeeTypeM _objEmployeeTypeM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _DropdownSvc.SaveAndUpdateEmployeeTypeDetails(_objEmployeeTypeM, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{EmployeeServiceLogClassName}.SaveUpdateEmployeeTypeDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Type Detail Or List.
        /// </summary>
        /// <param name="_objEmployeeTypeDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmployeeTypeDetailOrList(EmployeeTypeDetailsRequestParam _objEmployeeTypeDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _DropdownSvc.GetEmployeeTypeDataAsync(_objEmployeeTypeDetailsRequestParam, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{EmployeeServiceLogClassName}.GetEmployeeTypeDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Employee Type Master]
    }
}
