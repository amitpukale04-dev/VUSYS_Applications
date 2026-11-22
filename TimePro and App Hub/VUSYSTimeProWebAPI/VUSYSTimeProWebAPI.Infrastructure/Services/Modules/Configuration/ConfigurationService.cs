using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Configuration;
using VUSYSTimeProWebAPI.Application.Abstractions.Security;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Configuration
{
    public sealed class ConfigurationService : IConfigurationService
    {
        private const string ConfigurationServiceLogClassName = "ConfigurationService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly IDropdownService _DropdownSvc;
        private readonly IPasswordHashService _PasswordHashSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Configuration Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="DropdownSvc"></param>
        /// <param name="PasswordHashSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConfigurationService(SQLDBHelper SQLDBHelper, IDropdownService DropdownSvc, IPasswordHashService PasswordHashSvc,
            IEmailService EmailSvc, IWebHostEnvironment webHostEnvironment)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
            _PasswordHashSvc = PasswordHashSvc ?? throw new ArgumentNullException(nameof(PasswordHashSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        #region [Email Configuration]

        /// <summary>
        /// This Method Is Used To Save Or Update Email Configuration Details.
        /// </summary>
        /// <param name="_objConfigureEmailM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateEmailConfigurationDetails(ConfigureEmailM _objConfigureEmailM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "EMAIL_CONFIGURATION_DETAILS_RESPONSE";
            try
            {
                _objConfigureEmailM.Password = AESEncrytDecry.EncryptAES256(_objConfigureEmailM.Password, Helper.EncryptDecryptKey);
                _objConfigureEmailM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objConfigureEmailM, StoredProcedure.EMAIL_CONFIGURATION, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureConfigurationLogNamespace, $"{ConfigurationServiceLogClassName}.SaveUpdateEmailConfigurationDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Email Configuration Detail Or List.
        /// </summary>
        /// <param name="_objConfigureEmailDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmailConfigurationDetailOrList(ConfigureEmailDetailsRequestParam _objConfigureEmailDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objConfigureEmailDetailsRequestParam.Id switch
                {
                    0 => "EMAIL_CONFIGURATION_LIST_RESPONSE",
                    _ => "EMAIL_CONFIGURATION_DETAILS_RESPONSE"
                };
                ParamType = _objConfigureEmailDetailsRequestParam.Id switch
                {
                    0 => "GetAllEmailConfigurationList",
                    _ => "GetEmailConfigurationDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objConfigureEmailDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMAIL_CONFIGURATION_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if(Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    if(DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        DataTable dt = ds.Tables[0];
                        if(Helper.CheckDataTableIsNullOrEmpty(dt))
                        {
                            Helper.AddNewColumnToDataTable(dt, "PlainPassword", typeof(string));
                            foreach (DataRow row in dt.Rows)
                            {
                                string Password = row.Field<string>("Password");
                                row.SetField("PlainPassword", AESEncrytDecry.DecryptAES256(Password, Helper.EncryptDecryptKey));
                            }
                            finalDataSet = Helper.ConvertDataTableToDataSet(dt.Copy());
                        }
                    }
                }
                else
                {
                    finalDataSet = ds;
                }
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureConfigurationLogNamespace, $"{ConfigurationServiceLogClassName}.GetEmailConfigurationDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Email Configuration]
    }
}
