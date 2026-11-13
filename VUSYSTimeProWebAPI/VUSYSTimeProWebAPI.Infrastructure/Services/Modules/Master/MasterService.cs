using System.Data;
using System.Text;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Master;
using VUSYSTimeProWebAPI.Application.Abstractions.Security;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;
using static CommonLibCore10.CONST;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Master
{
    public sealed class MasterService : IMasterService
    {
        private const string MasterServiceLogClassName = "MasterService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly IDropdownService _DropdownSvc;
        private readonly IPasswordHashService _PasswordHashSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IContentTypeService _ContentTypeSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Master Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="DropdownSvc"></param>
        /// <param name="PasswordHashSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="ContentTypeSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MasterService(SQLDBHelper SQLDBHelper, IDropdownService DropdownSvc, IPasswordHashService PasswordHashSvc, 
            IEmailService EmailSvc, IContentTypeService ContentTypeSvc, IWebHostEnvironment webHostEnvironment)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
            _PasswordHashSvc = PasswordHashSvc ?? throw new ArgumentNullException(nameof(PasswordHashSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _ContentTypeSvc = ContentTypeSvc ?? throw new ArgumentNullException(nameof(ContentTypeSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        #region [User Master]

        /// <summary>
        /// This Method Is Used To Save Or Update User Details.
        /// </summary>
        /// <param name="_objUsersM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> SaveUpdateUserDetails(UsersM _objUsersM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "USER_DETAILS_RESPONSE";
            try
            {
                if(_objUsersM.Id == 0)
                {
                    string PlainPassword = CommonHelper.GenerateNewPassword(13);
                    string NewPasswordEncryptedWithNewSalt = string.Empty;
                    string NewPasswordSalt = string.Empty;
                    _objUsersM.PlainPassword = PlainPassword;
                    (NewPasswordEncryptedWithNewSalt, NewPasswordSalt) = _PasswordHashSvc.GeneratePasswordHash(PlainPassword);
                    _objUsersM.Password = NewPasswordEncryptedWithNewSalt;
                    _objUsersM.PasswordSalt = NewPasswordSalt;
                    _objUsersM.IsEmailVerified = true;
                    _objUsersM.IsReset = true;
                }
                _objUsersM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objUsersM, StoredProcedure.USER_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    string DBMessage = Convert.ToString(ds.Tables[0].Rows[0]["Message"].ToString());
                    int UserId = Convert.ToInt32(ds.Tables[0].Rows[0]["Id"].ToString());

                    if(_objUsersM.Id == 0 && DBMessage != "The User Details Already Exists.")
                    {
                        string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "UserRegistrationEmailTemplate.html");
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", _objUsersM.FullName);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Password#", _objUsersM.PlainPassword);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", CommonHelper.KeyOption.FrontEndBaseURL);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                        string MailSubject = $"Time-Pro - User Registered";
                        var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, _objUsersM.UserEmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                        var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                        string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                        JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                        string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                        string EmailSentResponseReturnResult = string.Empty;
                        string EmailSentFailedResponseReturnResult = DBMessage + " " + "Password Has Been Sent To Your Registered Email ID !!!";

                        if (resultjObjectEmail == "ok")
                        {
                            var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();

                            if (emailResponse?.Message == Helper.EmailSuccess)
                            {
                                EmailSentResponseReturnResult = $"{DBMessage} Password Has Been Sent To Your Registered Email ID !!!";

                                List<List<string>> rows = new List<List<string>>
                                {
                                    new List<string> { UserId.ToString(), DBStatus, EmailSentResponseReturnResult }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                return finalDataSet;
                            }
                        }
                        else
                        {
                            List<List<string>> ErrorRows = new List<List<string>>
                            {
                                new List<string> { UserId.ToString(), DBStatus, EmailSentFailedResponseReturnResult }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                        }
                    }
                    else
                    {
                        finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateUserDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get User Details Or List.
        /// </summary>
        /// <param name="_objUserDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetUserDetailOrList(UserDetailsRequestParam _objUserDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objUserDetailsRequestParam.Id switch
                {
                    0 => "USER_LIST_RESPONSE",
                    _ => "USER_DETAILS_RESPONSE"
                };
                ParamType = _objUserDetailsRequestParam.Id switch
                {
                    0 => "GetAllUserList",
                    _ => "GetUserDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objUserDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.USER_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetUserDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [User Master]

        #region [Role Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Role Details.
        /// </summary>
        /// <param name="_objRolesM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateRoleDetails(RolesM _objRolesM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "ROLE_DETAILS_RESPONSE";
            try
            {
                _objRolesM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objRolesM, StoredProcedure.ROLE_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateRoleDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Role Details Or List.
        /// </summary>
        /// <param name="_objRoleDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetRoleDetailOrList(RoleDetailsRequestParam _objRoleDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objRoleDetailsRequestParam.Id switch
                {
                    0 => "ROLE_LIST_RESPONSE",
                    _ => "ROLE_DETAILS_RESPONSE"
                };
                ParamType = _objRoleDetailsRequestParam.Id switch
                {
                    0 => "GetAllRoleList",
                    _ => "GetRoleDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objRoleDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.ROLE_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetRoleDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Role Master]

        #region [Security Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Security Details.
        /// </summary>
        /// <param name="_objSecurityMasterParameterCommon"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateSecurityDetails(SecurityMasterParameterCommon _objSecurityMasterParameterCommon, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "SECURITY_MASTER_RESPONSE";
            try
            {
                SecurityMaster _objSecurityMaster = Helper.ToCopyClassProperties<SecurityMasterParameterCommon, SecurityMaster>(_objSecurityMasterParameterCommon);
                _objSecurityMaster.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objSecurityMaster, StoredProcedure.SECURITY_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateSecurityDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Security Details Or List.
        /// </summary>
        /// <param name="_objSecurityMasterDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetSecurityDetailOrList(SecurityMasterDetailsRequestParam _objSecurityMasterDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objSecurityMasterDetailsRequestParam.SecurityId switch
                {
                    0 => "SECURITY_LIST_RESPONSE",
                    _ => "SECURITY_DETAILS_RESPONSE"
                };
                ParamType = _objSecurityMasterDetailsRequestParam.SecurityId switch
                {
                    0 => "GetAllSecurityMasterList",
                    _ => "GetSecurityMasterDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@SecurityId", _objSecurityMasterDetailsRequestParam.SecurityId },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.SECURITY_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetSecurityDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Delete Security Details.
        /// </summary>
        /// <param name="_objSecurityMasterDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> DeleteSecurityDetails(SecurityMasterDetailsRequestParam _objSecurityMasterDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@SecurityId", _objSecurityMasterDetailsRequestParam.SecurityId },
                    { "@Type", "DeleteSecurityMasterDetails" }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.SECURITY_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, "SECURITY_DELETE_RESPONSE");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.DeleteSecurityDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Security Master]

        #region [User Security Access]

        /// <summary>
        /// This Method Is Used To Save Or Update User Security Access Details.
        /// </summary>
        /// <param name="_lstUserSecurityAccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateUserSecurityAccessDetails(List<UserSecurityAccess> _lstUserSecurityAccess, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "USER_SECURITY_ACCESS_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLListParametersAsync(_lstUserSecurityAccess, _lstUserSecurityAccess.GetType(), _lstUserSecurityAccess, StoredProcedure.USER_SECURITY_ACCESS, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateUserSecurityAccessDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get User Security Access List.
        /// </summary>
        /// <param name="_objUserSecurityAccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetUserSecurityAccessList(UserSecurityAccess _objUserSecurityAccess, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@SecurityAccessId", _objUserSecurityAccess.SecurityAccessId },
                    { "@SecurityId", _objUserSecurityAccess.SecurityID },
                    { "@RoleId", _objUserSecurityAccess.RoleID },
                    { "@UserId", _objUserSecurityAccess.UserID },
                    { "@Type", "GetAllSecurityAccessList" }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.USER_SECURITY_ACCESS_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateMultipleDatasetTable(finalDataSet, (DataSetDataTableConstants.Subroot1, "USER_SECURITY_ACCESS_SELECTED_LIST"), (DataSetDataTableConstants.Subroot, "USER_SECURITY_ACCESS_LIST"));
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetUserSecurityAccessList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get User Security Access List.
        /// </summary>
        /// <param name="_objUserSecurityAccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> DeleteUserSecurityAccessDetails(UserSecurityAccess _objUserSecurityAccess, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@SecurityAccessId", _objUserSecurityAccess.SecurityAccessId },
                    { "@SecurityId", _objUserSecurityAccess.SecurityID },
                    { "@RoleId", _objUserSecurityAccess.RoleID },
                    { "@UserId", _objUserSecurityAccess.UserID },
                    { "@Type", "DeleteUserSecurityAccess" }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.USER_SECURITY_ACCESS_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, "USER_SECURITY_DELETE_RESPONSE");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.DeleteUserSecurityAccessDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [User Security Access]

        #region [User Menu Access]

        /// <summary>
        /// This Method Is Used To Get User Menu Access.
        /// </summary>
        /// <param name="_objUserMenuAccessRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetUserMenuAccess(UserMenuAccessRequestParam _objUserMenuAccessRequestParam, CancellationToken cancellationToken = default)
        {
            string jsonResponse = string.Empty;
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objUserMenuAccessRequestParam.ParamType switch
                {
                    "Menu" => "USER_MENU_ACCESS_RESPONSE",
                    "Action" => "USER_ACTION_ACCESS_RESPONSE",
                    "RoleMenuAccess" => "USER_ROLE_MENU_ACCESS_RESPONSE",
                    _ => "USER_MENU_ACCESS_RESPONSE"
                };
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@UserId", _objUserMenuAccessRequestParam.UserId },
                    { "@RoleId", _objUserMenuAccessRequestParam.RoleID },
                    { "@ParamType", _objUserMenuAccessRequestParam.ParamType }
                };
                var finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.USER_MENU_ACCESS, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if(Helper.CheckDataSetIsNullOrEmpty(finalDataSet))
                {
                    DataTable dt = finalDataSet.Tables[0];

                    string status = Convert.ToString(dt.Rows[0]["Status"]) ?? string.Empty;

                    var menuAccessData = dt.AsEnumerable()
                        .GroupBy(row => new
                        {
                            MainMenuId = Convert.ToInt32(row["MainMenuId"]),
                            MainMenuName = Convert.ToString(row["MainMenuName"]) ?? string.Empty
                        })
                        .Select(mainMenuGroup => new
                        {
                            MainMenuId = mainMenuGroup.Key.MainMenuId,

                            MainMenuName = mainMenuGroup.Key.MainMenuName,

                            SubMenus = mainMenuGroup
                                .GroupBy(row => new
                                {
                                    SubMenuId = Convert.ToInt32(row["SubMenuId"]),
                                    SubMenu = Convert.ToString(row["SubMenu"]) ?? string.Empty,

                                    RoutePath = Convert.ToString(row["RoutePath"]) ?? string.Empty,
                                    SvgIcon = Convert.ToString(row["SvgIcon"]) ?? string.Empty,
                                    Tone = Convert.ToString(row["Tone"]) ?? string.Empty,
                                    SearchKeywords = Convert.ToString(row["SearchKeywords"]) ?? string.Empty
                                })
                                .Select(subMenuGroup => new
                                {
                                    SubMenuId = subMenuGroup.Key.SubMenuId,

                                    SubMenu = subMenuGroup.Key.SubMenu,

                                    RoutePath = subMenuGroup.Key.RoutePath,

                                    SvgIcon = subMenuGroup.Key.SvgIcon,

                                    Tone = subMenuGroup.Key.Tone,

                                    SearchKeywords = subMenuGroup.Key.SearchKeywords
                                        .Split(
                                            ',',
                                            StringSplitOptions.RemoveEmptyEntries |
                                            StringSplitOptions.TrimEntries)
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .ToList(),

                                    IsSelected = subMenuGroup
                                        .Any(row => Convert.ToBoolean(row["IsSelected"])),

                                    SecurityAccessIds = subMenuGroup
                                        .Where(row =>
                                            (Convert.ToString(row["SecurityName"]) ?? string.Empty)
                                            .StartsWith(
                                                "Allow To Open",
                                                StringComparison.OrdinalIgnoreCase))
                                        .Select(row => Convert.ToInt32(row["SecurityId"]))
                                        .Distinct()
                                        .OrderBy(securityId => securityId)
                                        .ToList(),

                                    ActionSecurityAccessIds = subMenuGroup
                                        .Where(row =>
                                            !(Convert.ToString(row["SecurityName"]) ?? string.Empty)
                                            .StartsWith(
                                                "Allow To Open",
                                                StringComparison.OrdinalIgnoreCase))
                                        .Select(row => Convert.ToInt32(row["SecurityId"]))
                                        .Distinct()
                                        .OrderBy(securityId => securityId)
                                        .ToList()
                                })
                                .OrderBy(x => x.SubMenuId)
                                .ToList()
                        })
                        .OrderBy(x => x.MainMenuId)
                        .ToList();

                    var response = new
                    {
                        message = "OK",
                        Status = status,
                        data = new Dictionary<string, object>
                        {
                            { TableName, menuAccessData }
                        }
                    };

                    return JsonConvert.SerializeObject(response);
                }
                else
                {
                    var emptyResponse = new
                    {
                        message = "OK",
                        Status = Helper.ToProperCase(MessageTypeConstants.Error),
                        data = new Dictionary<string, object>
                        {
                            { TableName, Array.Empty<object>() }
                        }
                    };

                    return JsonConvert.SerializeObject(emptyResponse);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetUserMenuAccess()", ex.Message, false);
                throw;
            }
        }

        #endregion [User Menu Access]

        #region [Role Menu Access]

        /// <summary>
        /// This Method Is Used To Save Or Update Role Menu Access Details.
        /// </summary>
        /// <param name="_lstRoleMenuAccessFrontEnd"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateRoleMenuAccessDetails(List<RoleMenuAccessFrontEnd> _lstRoleMenuAccessFrontEnd, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "ROLE_MENU_ACCESS_INSERT_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLListParametersAsync(_lstRoleMenuAccessFrontEnd, _lstRoleMenuAccessFrontEnd.GetType(), _lstRoleMenuAccessFrontEnd, StoredProcedure.ROLE_MENU_ACCESS_INSERT, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateRoleMenuAccessDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Role Menu Access]

        #region [Dropdown Items]

        /// <summary>
        /// This Method Is Used To Get Dropdown Items.
        /// </summary>
        /// <param name="_objDropdownRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetDropdownItems(DropdownRequestParam _objDropdownRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(_objDropdownRequestParam.SourceTable == "Employee")
                {
                    finalDataSet = await _DropdownSvc.GetEmployeeListAsync(cancellationToken);
                }
                else if(_objDropdownRequestParam.SourceTable == "User")
                {
                    finalDataSet = await _DropdownSvc.GetUserListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "Role")
                {
                    finalDataSet = await _DropdownSvc.GetRoleListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "UserAndRole")
                {
                    finalDataSet = await _DropdownSvc.GetUserAndRoleListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "Gender")
                {
                    finalDataSet = await _DropdownSvc.GetGenderListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "Country")
                {
                    finalDataSet = await _DropdownSvc.GetCountryListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "State")
                {
                    finalDataSet = await _DropdownSvc.GetStateListAsync(Convert.ToInt32(_objDropdownRequestParam.ArgumentValue), cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "EmployeeType")
                {
                    finalDataSet = await _DropdownSvc.GetEmployeeTypeListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "Project")
                {
                    finalDataSet = await _DropdownSvc.GetProjectListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "ProjectBasedOnEmployeeCode")
                {
                    finalDataSet = await _DropdownSvc.GetProjectListBasedOnEmployeeCodeAsync(_objDropdownRequestParam.ArgumentValue, cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "AllProject")
                {
                    finalDataSet = await _DropdownSvc.GetProjectListWithAllAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "AllClient")
                {
                    finalDataSet = await _DropdownSvc.GetClientListWithAllAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "ProjectStatus")
                {
                    finalDataSet = await _DropdownSvc.GetProjectStatusListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "ProjectActivity")
                {
                    finalDataSet = await _DropdownSvc.GetProjectActivityListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "ProjectActivityForLeave")
                {
                    finalDataSet = await _DropdownSvc.GetProjectActivityListForOnLeaveAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "EmployeeProjectWithAll")
                {
                    finalDataSet = await _DropdownSvc.EmployeesWorkingOnProjectWithAllListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "Year")
                {
                    finalDataSet = await _DropdownSvc.GetYearListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "Month")
                {
                    finalDataSet = await _DropdownSvc.GetMonthListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "LeaveType")
                {
                    finalDataSet = await _DropdownSvc.GetLeaveTypeListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "LeaveApproveRejectStatus")
                {
                    finalDataSet = await _DropdownSvc.GetLeaveApproveRejectStatusListAsync(cancellationToken);
                }
                else if (_objDropdownRequestParam.SourceTable == "LeaveStatus")
                {
                    finalDataSet = await _DropdownSvc.GetLeaveStatusListAsync(cancellationToken);
                }
                else
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "@SourceTable", _objDropdownRequestParam.SourceTable },
                        { "@ArgumentValue", _objDropdownRequestParam.ArgumentValue },
                        { "@ArgumentValue1", _objDropdownRequestParam.ArgumentValue1 }
                    };
                    finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.DROPDOWN_ITEMS, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                    cancellationToken.ThrowIfCancellationRequested();
                    finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, CommonHelper.DROPDOWN_LIST_RESPONSE);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetDropdownItems()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Dropdown Items]

        #region [Gender Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Gender Details.
        /// </summary>
        /// <param name="_objGenderM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateGenderDetails(GenderM _objGenderM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "GENDER_DETAILS_RESPONSE";
            try
            {
                _objGenderM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objGenderM, StoredProcedure.GENDER_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateGenderDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Gender Details Or List.
        /// </summary>
        /// <param name="_objGenderDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetGenderDetailOrList(GenderDetailsRequestParam _objGenderDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objGenderDetailsRequestParam.Id switch
                {
                    0 => "GENDER_LIST_RESPONSE",
                    _ => "GENDER_DETAILS_RESPONSE"
                };
                ParamType = _objGenderDetailsRequestParam.Id switch
                {
                    0 => "GetAllGenderList",
                    _ => "GetGenderDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objGenderDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.GENDER_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetGenderDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Gender Master]

        #region [Country Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Country Details.
        /// </summary>
        /// <param name="_objCountryM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateCountryDetails(CountryM _objCountryM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "COUNTRY_DETAILS_RESPONSE";
            try
            {
                _objCountryM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objCountryM, StoredProcedure.COUNTRY_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateCountryDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Country Details Or List.
        /// </summary>
        /// <param name="_objCountryDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetCountryDetailOrList(CountryDetailsRequestParam _objCountryDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objCountryDetailsRequestParam.Id switch
                {
                    0 => "COUNTRY_LIST_RESPONSE",
                    _ => "COUNTRY_DETAILS_RESPONSE"
                };
                ParamType = _objCountryDetailsRequestParam.Id switch
                {
                    0 => "GetAllCountryList",
                    _ => "GetCountryDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objCountryDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.COUNTRY_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetCountryDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Country Master]

        #region [State Master]

        /// <summary>
        /// This Method Is Used To Save Or Update State Details.
        /// </summary>
        /// <param name="_objStateM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateStateDetails(StateM _objStateM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "STATE_DETAILS_RESPONSE";
            try
            {
                _objStateM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objStateM, StoredProcedure.STATE_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateStateDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get State Details Or List.
        /// </summary>
        /// <param name="_objStateDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetStateDetailOrList(StateDetailsRequestParam _objStateDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objStateDetailsRequestParam.Id switch
                {
                    0 => "STATE_LIST_RESPONSE",
                    _ => "STATE_DETAILS_RESPONSE"
                };
                ParamType = _objStateDetailsRequestParam.Id switch
                {
                    0 => "GetAllStateList",
                    _ => "GetStateDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objStateDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.STATE_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetStateDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [State Master]

        #region [Holiday Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Holiday Details.
        /// </summary>
        /// <param name="_objHolidayM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateHolidayDetails(HolidayM _objHolidayM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "HOLIDAY_DETAILS_RESPONSE";
            try
            {
                _objHolidayM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objHolidayM, StoredProcedure.HOLIDAY_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateHolidayDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Holiday Details Or List.
        /// </summary>
        /// <param name="_objHolidayDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetHolidayDetailOrList(HolidayDetailsRequestParam _objHolidayDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objHolidayDetailsRequestParam.Id switch
                {
                    0 => "HOLIDAY_LIST_RESPONSE",
                    _ => "HOLIDAY_DETAILS_RESPONSE"
                };
                ParamType = _objHolidayDetailsRequestParam.Id switch
                {
                    0 => "GetAllHolidayList",
                    _ => "GetHolidayDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objHolidayDetailsRequestParam.Id },
                    { "@HolidayYear", _objHolidayDetailsRequestParam.HolidayYear },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.HOLIDAY_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetHolidayDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Export Holiday List To PDF.
        /// </summary>
        /// <param name="_objHolidayDetailsExportToPDFRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> ExportHolidayListToPDF(HolidayDetailsExportToPDFRequestParam _objHolidayDetailsExportToPDFRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "HolidayYear", "Status", "Message", "HolidayListPDFFileName", "HolidayListPDFBase64" };
            string TableName = "HOLIDAY_LIST_PDF_RESPONSE";
            string TempHolidayListHtmlFilePath = string.Empty;
            string HolidayListPDFDocumentBase64 = string.Empty;
            string HolidayYear = _objHolidayDetailsExportToPDFRequestParam.HolidayYear.ToString();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", 0 },
                    { "@HolidayYear", _objHolidayDetailsExportToPDFRequestParam.HolidayYear },
                    { "@Type", "GetAllHolidayListForExport" }
                };
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.HOLIDAY_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    var row = ds.Tables[0].Rows[0];
                    string DBStatus = Helper.ToProperCase(Convert.ToString(row["Status"]));
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        string HolidayMasterListPDFTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "HolidayMasterListPDFTemplate.html");
                        StringBuilder holidayRows = new StringBuilder();

                        foreach (DataTable table in ds.Tables)
                        {
                            foreach (DataRow HolidayRow in table.Rows)
                            {
                                holidayRows.Append($@"
                                <tr>
                                    <td>{Helper.EncodeHTMLText(Convert.ToString(HolidayRow["HolidayDate"]))}</td>
                                    <td>{Helper.EncodeHTMLText(Convert.ToString(HolidayRow["DayName"]))}</td>
                                    <td>{Helper.EncodeHTMLText(Convert.ToString(HolidayRow["HolidayName"]))}</td>
                                    <td>{Helper.EncodeHTMLText(Convert.ToString(HolidayRow["Remarks"]))}</td>
                                </tr>");
                            }
                        }

                        HolidayMasterListPDFTemplate = HolidayMasterListPDFTemplate.Replace("#CompanyName#", "VUSYS TECHNOLOGY")
                            .Replace("#HolidayYear#", HolidayYear)
                            .Replace("#FooterText#", $"Holiday Calendar - {HolidayYear}")
                            .Replace("#HolidayRows#", holidayRows.ToString());

                        string WaterMarkLogoFileName = "VUSYS_WaterMarkLogo.png";
                        string WaterMarklogoPath = Path.Combine(CommonHelper.GetActualPath(_webHostEnvironment, "Content"), "Images", WaterMarkLogoFileName);
                        if (File.Exists(WaterMarklogoPath))
                        {
                            string WaterMarkLogoBase64 = FileHelper.ConvertFileToBase64(WaterMarklogoPath);
                            if (!string.IsNullOrEmpty(WaterMarkLogoBase64))
                            {
                                string WaterMarkLogoImageFileContentType = _ContentTypeSvc.GetFileContentType(WaterMarkLogoFileName);
                                string WaterMarkimgSrc = $"data:{WaterMarkLogoImageFileContentType};base64,{WaterMarkLogoBase64}";
                                string WaterMarkLogoImage = $@"<img src='{WaterMarkimgSrc}' alt='' aria-hidden='true'/>";
                                HolidayMasterListPDFTemplate = HolidayMasterListPDFTemplate.Replace("#CompanyWatermarkLogo#", WaterMarkLogoImage);
                            }
                        }

                        string LogoFileName = "VUSYS_HeaderLogo.png";
                        string logoPath = Path.Combine(CommonHelper.GetActualPath(_webHostEnvironment, "Content"), "Images", LogoFileName);
                        if (File.Exists(logoPath))
                        {
                            string LogoBase64 = FileHelper.ConvertFileToBase64(logoPath);
                            if (!string.IsNullOrEmpty(LogoBase64))
                            {
                                string LogoImageFileContentType = _ContentTypeSvc.GetFileContentType(LogoFileName);
                                string imgSrc = $"data:{LogoImageFileContentType};base64,{LogoBase64}";
                                string LogoImage = $@"<img src='{imgSrc}' alt='VUSYS Technology Logo'/>";
                                HolidayMasterListPDFTemplate = HolidayMasterListPDFTemplate.Replace("#CompanyLogo#", LogoImage);
                            }
                        }

                        string tempFolderPath = CommonHelper.GetActualPath(_webHostEnvironment, "TempPDFFiles");
                        var TempfileName = "TempHolidayMasterListPDFTemplate.html";
                        string PDFFileName = $"VUSYS_HolidayList_{HolidayYear}";
                        TempHolidayListHtmlFilePath = Path.Combine(tempFolderPath, TempfileName);
                        Helper.DeleteFile(TempHolidayListHtmlFilePath);
                        File.WriteAllText(TempHolidayListHtmlFilePath, HolidayMasterListPDFTemplate);
                        byte[] pdfBytes = PDFHelper.ConvertHTMLToPDF(TempHolidayListHtmlFilePath, true, false, null, false, null, false, null, null);
                        if (pdfBytes.Length > 0)
                        {
                            string pdfBase64 = Convert.ToBase64String(pdfBytes);
                            if (!string.IsNullOrEmpty(pdfBase64))
                            {
                                //string Folder = CommonHelper.SaveFileToFolder(pdfBase64, FileTypes.Pdf, _webHostEnvironment);
                                var result = new FileHelper.Base64File(pdfBase64, PDFFileName, FileHelper.GetFileExtension(FileTypes.Pdf));
                                HolidayListPDFDocumentBase64 = result.Base64;
                            }
                        }
                        if (!string.IsNullOrEmpty(HolidayListPDFDocumentBase64))
                        {
                            HolidayListPDFDocumentBase64 = Helper.CleanBase64(HolidayListPDFDocumentBase64);
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { HolidayYear, Helper.ToProperCase(MessageTypeConstants.Success), $"Holiday List {HolidayYear} PDF Downloaded Successfully !!!", $"{PDFFileName}.{FileTypes.Pdf}", HolidayListPDFDocumentBase64 }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        }
                        else
                        {
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { HolidayYear, Helper.ToProperCase(MessageTypeConstants.Error), $"Failed To Download Holiday List {HolidayYear} PDF !!!", string.Empty, string.Empty }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        }
                    }
                    else
                    {
                        string Message = ds.Tables[0].Rows[0]["Message"].ToString();
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { HolidayYear, Helper.ToProperCase(MessageTypeConstants.Error), Message, string.Empty, string.Empty }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { HolidayYear, Helper.ToProperCase(MessageTypeConstants.Error), ResponseMessageConstants.NoDataFound, string.Empty, string.Empty }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.ExportHolidayListToPDF()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Holiday Master]

        #region [Leave Type Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Leave Type Details.
        /// </summary>
        /// <param name="_objLeaveTypeM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateLeaveTypeDetails(LeaveTypeM _objLeaveTypeM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "LEAVE_TYPE_DETAILS_RESPONSE";
            try
            {
                _objLeaveTypeM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objLeaveTypeM, StoredProcedure.LEAVE_TYPE_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.SaveUpdateLeaveTypeDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Leave Type Details Or List.
        /// </summary>
        /// <param name="_objLeaveTypeDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetLeaveTypeDetailOrList(LeaveTypeDetailsRequestParam _objLeaveTypeDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objLeaveTypeDetailsRequestParam.Id switch
                {
                    0 => "LEAVE_TYPE_LIST_RESPONSE",
                    _ => "LEAVE_TYPE_DETAILS_RESPONSE"
                };
                ParamType = _objLeaveTypeDetailsRequestParam.Id switch
                {
                    0 => "GetAllLeaveTypeList",
                    _ => "GetLeaveTypeDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objLeaveTypeDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.LEAVE_TYPE_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureMasterLogNamespace, $"{MasterServiceLogClassName}.GetLeaveTypeDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Leave Type Master]
    }
}
