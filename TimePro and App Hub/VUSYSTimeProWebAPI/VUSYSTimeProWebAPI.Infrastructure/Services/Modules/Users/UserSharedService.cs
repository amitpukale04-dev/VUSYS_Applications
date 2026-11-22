using System.Data;
using CommonLibCore10;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Users;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Users
{
    public sealed class UserSharedService : IUserSharedService
    {
        private const string UserSharedServiceLogClassName = "UserSharedService";
        private readonly SQLDBHelper _objSQLDBHelper;

        /// <summary>
        /// User Shared Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public UserSharedService(SQLDBHelper SQLDBHelper)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
        }

        /// <summary>
        /// This Method Is Used To Get User Details.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="ParamType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetUserDetails(string EmailID, string ParamType, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    {"@EmailID", EmailID },
                    {"@ParamType", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.USER_LOGIN, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserSharedServiceLogClassName}.GetUserDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method is Used to Update User Login or Logout Date.
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="Param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> UpdateUserLoginOrLogoutDate(int UserID, string Param, CancellationToken cancellationToken = default)
        {
            bool IsUpdated = false;
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", UserID },
                    { "@Status", Param }
                };
                dt = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.USER_LAST_LOGIN_LOGOUT, parameters, CONST.DBRetType.DATATABLE, cancellationToken) as DataTable;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataTableIsNullOrEmpty(dt))
                {
                    IsUpdated = true;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserSharedServiceLogClassName}.UpdateUserLoginOrLogoutDate()", ex.Message, false);
                throw;
            }
            return IsUpdated;
        }
    }
}
