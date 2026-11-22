using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Authentication;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.OTPService;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Users;
using VUSYSTimeProWebAPI.Application.Abstractions.Security;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Users
{
    public sealed class UserLoginService : IUserLoginService
    {
        private const string UserLoginServiceLogClassName = "UserLoginService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly ITokenService _TokenSvc;
        private readonly ITokenStoreService _TokenStoreSvc;
        private readonly IPasswordHashService _PasswordHashSvc;
        private readonly IOTPService _OTPSvc;
        private readonly IDropdownService _DropdownSvc;
        private readonly IUserSharedService _UserSharedSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IContentTypeService _ContentTypeSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// User Login Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="TokenSvc"></param>
        /// <param name="TokenStoreSvc"></param>
        /// <param name="PasswordHashSvc"></param>
        /// <param name="OTPSvc"></param>
        /// <param name="DropdownSvc"></param>
        /// <param name="UserSharedSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="ContentTypeSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public UserLoginService(SQLDBHelper SQLDBHelper, ITokenService TokenSvc, ITokenStoreService TokenStoreSvc,
            IPasswordHashService PasswordHashSvc, IOTPService OTPSvc, IDropdownService DropdownSvc, IUserSharedService UserSharedSvc,
            IEmailService EmailSvc, IContentTypeService ContentTypeSvc, IWebHostEnvironment webHostEnvironment)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _TokenSvc = TokenSvc ?? throw new ArgumentNullException(nameof(TokenSvc));
            _TokenStoreSvc = TokenStoreSvc ?? throw new ArgumentNullException(nameof(TokenStoreSvc));
            _PasswordHashSvc = PasswordHashSvc ?? throw new ArgumentNullException(nameof(PasswordHashSvc));
            _OTPSvc = OTPSvc ?? throw new ArgumentNullException(nameof(OTPSvc));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
            _UserSharedSvc = UserSharedSvc ?? throw new ArgumentNullException(nameof(UserSharedSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _ContentTypeSvc = ContentTypeSvc ?? throw new ArgumentNullException(nameof(ContentTypeSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        /// <summary>
        /// This Method Is Used To Get User Login Details.
        /// </summary>
        /// <param name="objUserLogin"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> GetUserLoginDetails(UserLogin objUserLogin, CancellationToken cancellationToken = default)
        {
            DataTable dt = new DataTable();
            DataSet _ds = new DataSet();
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string ProfileImageBase64 = string.Empty;
            string EncodedOTP = string.Empty;
            string TableName = "USER_LOGIN_DETAILS";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ds = await _UserSharedSvc.GetUserDetails(objUserLogin.EmailID, "CheckUserExist", cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(_ds))
                {
                    dt = Helper.ConvertDataSetToDataTable(_ds);
                    if (Helper.CheckDataTableIsNullOrEmpty(dt))
                    {
                        string DBStatus = Helper.ToProperCase(Convert.ToString(dt.Rows[0]["Status"].ToString()));
                        string DBMessage = Convert.ToString(dt.Rows[0]["Message"].ToString());
                        if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                        {
                            bool OTPApplicable = Convert.ToBoolean(dt.Rows[0]["IsEnableTwoFactorAuthentication"].ToString());
                            string PasswordSalt = Convert.ToString(dt.Rows[0]["PasswordSalt"].ToString());
                            string DBPassword = Convert.ToString(dt.Rows[0]["Password"].ToString());
                            string strHashPassword = _PasswordHashSvc.ComputePasswordWithExistingSalt(objUserLogin.Password, PasswordSalt).Hash;
                            if (DBPassword == strHashPassword)
                            {
                                string FullName = Convert.ToString(dt.Rows[0]["FullName"].ToString());
                                string strEmailID = Convert.ToString(dt.Rows[0]["EmailID"].ToString());
                                int userId = dt.Columns.Contains("Id") ? Convert.ToInt32(dt.Rows[0]["Id"]) : 0;
                                string userName = dt.Columns.Contains("UserName") ? Convert.ToString(dt.Rows[0]["UserName"]) : string.Empty;
                                string EmployeeCode = dt.Columns.Contains("EmployeeCode") ? Convert.ToString(dt.Rows[0]["EmployeeCode"]) : string.Empty;
                                string UserRole = dt.Columns.Contains("UserRole") ? Convert.ToString(dt.Rows[0]["UserRole"]) : string.Empty;
                                string mobileNumber = dt.Columns.Contains("MobileNo") ? Convert.ToString(dt.Rows[0]["MobileNo"]) : string.Empty;
                                string userImageFileName = dt.Columns.Contains("UserImageFileName") ? Convert.ToString(dt.Rows[0]["UserImageFileName"]) : string.Empty;
                                int roleId = dt.Columns.Contains("RoleID") ? Convert.ToInt32(dt.Rows[0]["RoleID"]) : 0;
                                bool isActive = dt.Columns.Contains("Obsolete") ? Convert.ToBoolean(dt.Rows[0]["Obsolete"]) : true;
                                var contentType = _ContentTypeSvc.GetFileContentType(userImageFileName);
                                ProfileImageBase64 = ProfileImageBase64 = dt.Rows[0]["UserImage"] is byte[] imageBytes && imageBytes.Length > 0 ? $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}" : string.Empty;
                                Helper.RemoveColumnFromDataTable(dt, "Message");
                                Helper.RemoveColumnFromDataTable(dt, "Password");
                                Helper.RemoveColumnFromDataTable(dt, "PasswordSalt");
                                Helper.RemoveColumnFromDataTable(dt, "UserImage");

                                if (string.IsNullOrEmpty(ProfileImageBase64))
                                {
                                    string DefaultImageFileName = string.Empty;
                                    string strGenderName = await _DropdownSvc.GetGenderNameAsync(EmployeeCode);
                                    if (strGenderName == "MALE")
                                    {
                                        DefaultImageFileName = "Default_Male.png";
                                    }
                                    else if (strGenderName == "FEMALE")
                                    {
                                        DefaultImageFileName = "Default_Female.png";
                                    }
                                    else
                                    {
                                        DefaultImageFileName = "DefaultUserImage.png";
                                    }
                                    string DefaultUserImageFolderPath = Path.Combine(CommonHelper.GetActualPath(_webHostEnvironment, "Content"), "images", DefaultImageFileName);
                                    byte[] _myData = File.ReadAllBytes(DefaultUserImageFolderPath);

                                    cancellationToken.ThrowIfCancellationRequested();
                                    UsersM _objUsersM = new UsersM();
                                    _objUsersM.Id = userId;
                                    _objUsersM.EmployeeCode = EmployeeCode;
                                    _objUsersM.UserImage = _myData;
                                    _objUsersM.UserImageFileName = DefaultImageFileName;
                                    _objUsersM.Status = "UpdateProfileImage";
                                    var dsUpdatedProfileImage = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objUsersM, StoredProcedure.USER_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                                    cancellationToken.ThrowIfCancellationRequested();
                                    var DefaultImageContentType = _ContentTypeSvc.GetFileContentType(DefaultImageFileName);
                                    ProfileImageBase64 = $"data:{DefaultImageContentType};base64,{Convert.ToBase64String(_myData)}";
                                }

                                Helper.AddNewColumnToDataTable(dt, "is_enable_ai_assistant", typeof(bool));
                                Helper.AddNewColumnToDataTable(dt, "user_role", typeof(string));
                                Helper.AddNewColumnToDataTable(dt, "otp", typeof(string));
                                Helper.AddNewColumnToDataTable(dt, "security_access_ids_array", typeof(object));
                                Helper.AddNewColumnToDataTable(dt, "login_date_time", typeof(string));
                                Helper.AddNewColumnToDataTable(dt, "token_generated_date_time", typeof(string));
                                Helper.AddNewColumnToDataTable(dt, "accessToken", typeof(string));
                                Helper.AddNewColumnToDataTable(dt, "refreshToken", typeof(string));
                                Helper.AddNewColumnToDataTable(dt, "token_expiry_date_time", typeof(string));
                                Helper.AddNewColumnToDataTable(dt, "TokenExpirationInMinutes", typeof(int));
                                Helper.AddNewColumnToDataTable(dt, "RefreshTokenExpiresAt", typeof(int));
                                Helper.AddNewColumnToDataTable(dt, "UserImage", typeof(string));
                                bool IsOTPEmailSent = false;
                                if (OTPApplicable)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    var dsOTPDetails = await _OTPSvc.SendOTPEmail(strEmailID, FullName, cancellationToken);
                                    cancellationToken.ThrowIfCancellationRequested();
                                    if(Helper.CheckDataSetIsNullOrEmpty(dsOTPDetails))
                                    {
                                        string DBOTPStatus = Helper.ToProperCase(Convert.ToString(dsOTPDetails.Tables[0].Rows[0]["Status"].ToString()));
                                        if (DBOTPStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                                        {
                                            IsOTPEmailSent = true;
                                            EncodedOTP = Convert.ToString(dsOTPDetails.Tables[0].Rows[0]["otp"].ToString());
                                        }
                                    }
                                }

                                string accessToken = _TokenSvc.GenerateToken(FullName, strEmailID, EmployeeCode, Convert.ToString(roleId));
                                var RefreshAccessToken = _TokenSvc.GenerateRefreshToken(FullName, strEmailID, EmployeeCode, Convert.ToString(roleId));
                                var RefreshToken = _TokenSvc.GetRefreshTokenStore(FullName, strEmailID, EmployeeCode, Convert.ToString(roleId), RefreshAccessToken);

                                foreach (DataRow row in dt.Rows)
                                {
                                    row.SetField("is_enable_ai_assistant", Convert.ToBoolean(CommonHelper.KeyOption.IsEnableAIAssistant));
                                    row.SetField("user_role", UserRole);
                                    row.SetField("otp", IsOTPEmailSent ? EncodedOTP : string.Empty);
                                    row.SetField("UserImage", ProfileImageBase64);

                                    string securityAccessIdsStr = row.Field<string>("SecurityAccessIds");
                                    int[] securityAccessIdsArray = securityAccessIdsStr
                                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(int.Parse)
                                        .ToArray();

                                    row["security_access_ids_array"] = securityAccessIdsArray;
                                    row.SetField("login_date_time", DateTimeHelper.ConvertDateTimeStringToSpecificFormat(DateTime.Now.ToString("O"), DateTimeFormatConstants.MMDDYYYY_SLASH_12HSS_DATETIMEFORMAT));
                                    row.SetField("token_generated_date_time", Convert.ToString(DateTime.Now));
                                    row.SetField("accessToken", accessToken);
                                    row.SetField("refreshToken", RefreshAccessToken);
                                    row.SetField("token_expiry_date_time", Convert.ToString(DateTime.Now.AddMinutes(_TokenSvc.GetTokenExpirationInMinutes())));
                                    row.SetField("TokenExpirationInMinutes", _TokenSvc.GetTokenExpirationInMinutes());
                                    row.SetField("RefreshTokenExpiresAt", _TokenSvc.GetRefreshTokenExpirationInDays());
                                }
                                Helper.RemoveColumnFromDataTable(dt, "SecurityAccessIds");
                                Helper.RenameDataTableColumnName(dt, "security_access_ids_array", "security_access_ids");
                                TimeSpan time = TimeSpan.FromMinutes(_TokenSvc.GetTokenExpirationInMinutes());
                                var expirationTime = DateTime.Now.AddHours(time.TotalHours);
                                await _TokenStoreSvc.SaveTokenInfoAsync(FullName, strEmailID, EmployeeCode, Convert.ToString(roleId), accessToken, true, expirationTime);
                                _TokenStoreSvc.Save(RefreshToken);
                                var IsUpdated = await _UserSharedSvc.UpdateUserLoginOrLogoutDate(userId, "UpdateUserLogin", cancellationToken);
                                finalDataSet = Helper.ConvertDataTableToDataSet(dt.Copy());
                                finalDataSet.Tables[DataSetDataTableConstants.Subroot].TableName = TableName;
                            }
                            else
                            {
                                List<List<string>> rows = new List<List<string>>
                                {
                                    new List<string> { "0", Helper.ToProperCase(MessageTypeConstants.Error), "Enter The Valid Password !!!" }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                            }
                        }
                        else
                        {
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { "0", Helper.ToProperCase(MessageTypeConstants.Error), DBMessage }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { "0", Helper.ToProperCase(MessageTypeConstants.Error), "Enter The Valid EmailID and Password !!!" }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { "0", Helper.ToProperCase(MessageTypeConstants.Error), "Enter The Valid EmailID and Password !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserLoginServiceLogClassName}.GetUserLoginDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Scheduler User Login Details.
        /// </summary>
        /// <param name="objSchedulerUserLogin"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> GetSchedulerUserLoginDetails(SchedulerUserLogin objSchedulerUserLogin, CancellationToken cancellationToken = default)
        {
            DataTable dt = new DataTable();
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "USER_LOGIN_DETAILS";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (CommonHelper.KeyOption.SchedulerLoginID == objSchedulerUserLogin.LoginID)
                {
                    string ConfiguredDecryptedPassword = AESEncrytDecry.DecryptAES256(CommonHelper.KeyOption.SchedulerLoginPassword, Helper.EncryptDecryptKey);
                    if (objSchedulerUserLogin.Password == ConfiguredDecryptedPassword)
                    {
                        Helper.AddNewColumnToDataTable(dt, "Status", typeof(string));
                        Helper.AddNewColumnToDataTable(dt, "login_date_time", typeof(string));
                        Helper.AddNewColumnToDataTable(dt, "token_generated_date_time", typeof(string));
                        Helper.AddNewColumnToDataTable(dt, "accessToken", typeof(string));
                        Helper.AddNewColumnToDataTable(dt, "refreshToken", typeof(string));
                        Helper.AddNewColumnToDataTable(dt, "token_expiry_date_time", typeof(string));
                        Helper.AddNewColumnToDataTable(dt, "TokenExpirationInMinutes", typeof(int));
                        Helper.AddNewColumnToDataTable(dt, "RefreshTokenExpiresAt", typeof(int));
                        int RoleID = 1;
                        string accessToken = _TokenSvc.GenerateToken(objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, Convert.ToString(RoleID));
                        var RefreshAccessToken = _TokenSvc.GenerateRefreshToken(objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, Convert.ToString(RoleID));
                        var RefreshToken = _TokenSvc.GetRefreshTokenStore(objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, Convert.ToString(RoleID), RefreshAccessToken);

                        DataRow dr = dt.NewRow();
                        dr["Status"] = Helper.ToProperCase(MessageTypeConstants.Success);
                        dr["login_date_time"] = DateTimeHelper.ConvertDateTimeStringToSpecificFormat(DateTime.Now.ToString("O"), DateTimeFormatConstants.MMDDYYYY_SLASH_12HSS_DATETIMEFORMAT);
                        dr["token_generated_date_time"] = Convert.ToString(DateTime.Now);
                        dr["accessToken"] = accessToken;
                        dr["refreshToken"] = RefreshAccessToken;
                        dr["token_expiry_date_time"] = Convert.ToString(DateTime.Now.AddMinutes(_TokenSvc.GetTokenExpirationInMinutes()));
                        dr["TokenExpirationInMinutes"] = _TokenSvc.GetTokenExpirationInMinutes();
                        dr["RefreshTokenExpiresAt"] = _TokenSvc.GetRefreshTokenExpirationInDays();
                        dt.Rows.Add(dr);

                        TimeSpan time = TimeSpan.FromMinutes(_TokenSvc.GetTokenExpirationInMinutes());
                        var expirationTime = DateTime.Now.AddHours(time.TotalHours);
                        await _TokenStoreSvc.SaveTokenInfoAsync(objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, objSchedulerUserLogin.LoginID, Convert.ToString(RoleID), accessToken, true, expirationTime);
                        _TokenStoreSvc.Save(RefreshToken);

                        finalDataSet = Helper.ConvertDataTableToDataSet(dt.Copy());
                        finalDataSet.Tables["Table1"].TableName = TableName;
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { objSchedulerUserLogin.LoginID, Helper.ToProperCase(MessageTypeConstants.Error), "Invalid Password !!!" }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        return finalDataSet;
                    }
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { "0", Helper.ToProperCase(MessageTypeConstants.Error), "Invalid Login ID !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserLoginServiceLogClassName}.GetSchedulerUserLoginDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Change The Password.
        /// </summary>
        /// <param name="_objChangePassword"></param>
        /// <param name="Source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> ChangePassword(ChangePassword _objChangePassword, string Source, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            DataTable dt = new DataTable();
            string TableName = string.Empty;
            List<string> columns = new List<string> { "Status", "Message" };
            try
            {
                TableName = (Source == "ChangePassword" ? "CHANGE_PASSWORD_RESPONSE" : "RESET_PASSWORD_RESPONSE");
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _UserSharedSvc.GetUserDetails(_objChangePassword.EmailID, "CheckUserExist", cancellationToken);
                if(Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    string DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                    var row = ds.Tables[0].Rows[0];
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        string EmailID = Convert.ToString(row["EmailID"].ToString());

                        string CurrentPasswordSalt = Convert.ToString(row["PasswordSalt"]);
                        string CurrentPasswordEncrypted = Convert.ToString(row["Password"]);

                        string OldPlainPassword = string.Empty;
                        string NewPlainPassword = string.Empty;
                        string OldPasswordEncrypted = string.Empty;

                        if (Source == "ChangePassword")
                        {
                            OldPlainPassword = _objChangePassword.OldPassword;
                            // Verify Old Plain Password and Encrypt With Current Password Salt For Stored Procedure Check.
                            OldPasswordEncrypted = _PasswordHashSvc.ComputePasswordWithExistingSalt(OldPlainPassword, CurrentPasswordSalt).Hash;
                            TableName = "CHANGE_PASSWORD_RESPONSE";
                        }
                        else
                        {
                            OldPasswordEncrypted = CurrentPasswordEncrypted;
                            TableName = "RESET_PASSWORD_RESPONSE";
                        }
                        NewPlainPassword = _objChangePassword.NewPassword;

                        // Generate New Password Salt and Encrypted which will be as Level 1.
                        string NewPasswordEncryptedWithNewSalt = string.Empty;
                        string NewPasswordSalt = string.Empty;

                        (NewPasswordEncryptedWithNewSalt, NewPasswordSalt) = _PasswordHashSvc.GeneratePasswordHash(NewPlainPassword);

                        var parameters = new Dictionary<string, object>
                        {
                            {"@EmailID", EmailID },
                            {"@OldPassword", OldPasswordEncrypted },
                            {"@Password", NewPasswordEncryptedWithNewSalt },
                            {"@PasswordSalt", NewPasswordSalt },
                            {"@NumberofMinutes", 0 },
                            {"@ParamType", "ChangePassword" }
                        };
                        finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.CHANGE_PASSWORD, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                        cancellationToken.ThrowIfCancellationRequested();
                        finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), DBMessage }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserLoginServiceLogClassName}.ChangePassword()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Send Request For Reset Password.
        /// </summary>
        /// <param name="_objForgotPassword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> ForgotPassword(ForgotPassword _objForgotPassword, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            DataTable dt = new DataTable();
            List<string> columns = new List<string> { "EmailID", "Status", "Message" };
            string DBStatus = string.Empty;
            string DBMessage = string.Empty;
            string EmailID = string.Empty;
            string TableName = "FORGOT_PASSWORD_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var _ds = await _UserSharedSvc.GetUserDetails(_objForgotPassword.EmailID, "CheckUserExistsForForgotPassword", cancellationToken);
                if (Helper.CheckDataSetIsNullOrEmpty(_ds))
                {
                    dt = Helper.ConvertDataSetToDataTable(_ds);
                    if (Helper.CheckDataTableIsNullOrEmpty(dt))
                    {
                        DBStatus = Convert.ToString(dt.Rows[0]["Status"].ToString());
                        DBMessage = Convert.ToString(dt.Rows[0]["Message"].ToString());
                        if (DBStatus.ToUpper() == MessageTypeConstants.Success)
                        {
                            EmailID = Convert.ToString(dt.Rows[0]["EmailID"].ToString());
                            if (!string.IsNullOrEmpty(EmailID))
                            {
                                var parameters = new Dictionary<string, object>
                                {
                                    {"@EmailID", _objForgotPassword.EmailID },
                                    {"@OldPassword", string.Empty },
                                    {"@Password", string.Empty },
                                    {"@PasswordSalt", string.Empty },
                                    {"@NumberofMinutes", Convert.ToInt32(CommonHelper.KeyOption.ResetPasswordLinkExpiryInMinutes) },
                                    {"@ParamType", "UpdateResetPasswordRequest" }
                                };
                                cancellationToken.ThrowIfCancellationRequested();
                                var dsUpdateResetPassword = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.CHANGE_PASSWORD, parameters, CONST.DBRetType.DATASET) as DataSet;
                                cancellationToken.ThrowIfCancellationRequested();
                                if (Helper.CheckDataSetIsNullOrEmpty(dsUpdateResetPassword))
                                {
                                    string DBUpdateStatus = Helper.ToProperCase(dsUpdateResetPassword.Tables[0].Rows[0]["Status"].ToString());
                                    string DBUpdateMessage = Convert.ToString(dsUpdateResetPassword.Tables[0].Rows[0]["Message"].ToString());
                                    if (DBUpdateStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                                    {
                                        string FullName = Convert.ToString(dsUpdateResetPassword.Tables[0].Rows[0]["FullName"].ToString());
                                        string strEmail = Convert.ToString(dsUpdateResetPassword.Tables[0].Rows[0]["EmailID"].ToString());
                                        string PasswordExpiryDateTime = Convert.ToString(dsUpdateResetPassword.Tables[0].Rows[0]["PasswordExpiryDateTime"].ToString());
                                        if (!string.IsNullOrEmpty(FullName) && !string.IsNullOrEmpty(strEmail) && !string.IsNullOrEmpty(PasswordExpiryDateTime))
                                        {
                                            string LinkURL = CommonHelper.KeyOption.FrontEndBaseURL + "/reset-password?EmailID=" + Helper.EncodeStringToBase64(strEmail) + "&ResetPasswordLinkInMinutes=" + Helper.EncodeStringToBase64(CommonHelper.KeyOption.ResetPasswordLinkExpiryInMinutes) + "&PasswordExpiryDateTime=" + Helper.EncodeStringToBase64(PasswordExpiryDateTime);
                                            string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "ResetPasswordEmailTemplate.html");
                                            EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", FullName);
                                            EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", LinkURL);
                                            EmailBodyTemplate = EmailBodyTemplate.Replace("#PasswordLinkExpiryInMinutes#", CommonHelper.KeyOption.ResetPasswordLinkExpiryInMinutes);
                                            EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                                            string MailSubject = $"Time-Pro - Reset Password Request";

                                            var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, EmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                                            var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                                            string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                                            JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                                            string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                                            string EmailSentResponseReturnResult = string.Empty;

                                            if (resultjObjectEmail == "ok")
                                            {
                                                var EmailResponseData = jObjectEmail["data"][Helper.EMAIL_SENT_RESPONSE_TABLE_NAME].ToObject<SendEmailResponseParam[]>();
                                                if (EmailResponseData != null)
                                                {
                                                    List<SendEmailResponseParam> lstSendEmailResponse = null;
                                                    lstSendEmailResponse = EmailResponseData.ToList();
                                                    if (lstSendEmailResponse != null && lstSendEmailResponse.Count > 0)
                                                    {
                                                        string Message = lstSendEmailResponse[0].Message;
                                                        if (Message != null && Message == Helper.EmailSuccess)
                                                        {
                                                            DBUpdateMessage = DBUpdateMessage.Replace("!!!", "");
                                                            EmailSentResponseReturnResult = $"Reset Password Link Has Been Sent To Your Email ID !!!";

                                                            List<string> resetpasswordcolumns = new List<string> { "EmailID", "ResetPasswordLinkInMinutes", "ResetPasswordLink", "Status", "Message" };
                                                            List<List<string>> rows = new List<List<string>>
                                                            {
                                                                new List<string> { _objForgotPassword.EmailID, CommonHelper.KeyOption.ResetPasswordLinkExpiryInMinutes, LinkURL, DBUpdateStatus, EmailSentResponseReturnResult }
                                                            };
                                                            finalDataSet = Helper.GenerateCommonMessage(resetpasswordcolumns, rows, TableName);
                                                        }
                                                        else
                                                        {
                                                            EmailSentResponseReturnResult = Helper.Email_Failed;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        EmailSentResponseReturnResult = Helper.Email_Failed;
                                                    }
                                                }
                                                else
                                                {
                                                    EmailSentResponseReturnResult = Helper.Email_Failed;
                                                }
                                            }
                                            else
                                            {
                                                EmailSentResponseReturnResult = Helper.Email_Failed;
                                            }
                                        }
                                        else
                                        {
                                            List<List<string>> rows = new List<List<string>>
                                            {
                                                new List<string> { _objForgotPassword.EmailID, Helper.ToProperCase(MessageTypeConstants.Error), "Failed To Reset The Password !!!" }
                                            };
                                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                        }
                                    }
                                    else
                                    {
                                        List<List<string>> rows = new List<List<string>>
                                        {
                                            new List<string> { _objForgotPassword.EmailID, Helper.ToProperCase(MessageTypeConstants.Error), "Failed To Reset The Password !!!" }
                                        };
                                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                    }
                                }
                                else
                                {
                                    List<List<string>> rows = new List<List<string>>
                                    {
                                        new List<string> { _objForgotPassword.EmailID, Helper.ToProperCase(MessageTypeConstants.Error), "Failed To Reset The Password !!!" }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                }
                            }
                            else
                            {
                                List<List<string>> rows = new List<List<string>>
                                {
                                    new List<string> { _objForgotPassword.EmailID, Helper.ToProperCase(MessageTypeConstants.Error), "The Email ID is not mapped to your Email ID. Please update your email ID in the Employee Master and try again !!!" }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                            }
                        }
                        else
                        {
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { _objForgotPassword.EmailID, DBStatus, DBMessage }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { _objForgotPassword.EmailID, DBStatus, DBMessage }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objForgotPassword.EmailID, DBStatus, DBMessage }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserLoginServiceLogClassName}.ForgotPassword()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Check For Password Link Available.
        /// </summary>
        /// <param name="_objCheckPasswordLinkAvailableRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		public async Task<DataSet> CheckForPasswordLinkAvailable(CheckPasswordLinkAvailableRequestParam _objCheckPasswordLinkAvailableRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _DropdownSvc.CheckForPasswordLinkAvailable(_objCheckPasswordLinkAvailableRequestParam.EmailID, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserLoginServiceLogClassName}.CheckForPasswordLinkAvailable()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Refresh Token.
        /// </summary>
        /// <param name="objTokenRefreshRequest"></param>
        /// <returns></returns>
        public async Task<DataSet> GetRefreshToken(TokenRefreshRequest objTokenRefreshRequest)
        {
            DataTable dt = new DataTable();
            DataSet finalDataSet = new DataSet();
            string TableName = "REFRESH_TOKEN_RESPONSE";
            try
            {
                var token = _TokenStoreSvc.Get(objTokenRefreshRequest.RefreshToken);
                if (token == null)
                {
                    List<string> columns = new List<string> { "Status", "Message" };
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { "Error","Provided Refresh Token Is Invalid or Expired Token !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
                else
                {
                    _TokenStoreSvc.Revoke(objTokenRefreshRequest.RefreshToken);
                    string accessToken = _TokenSvc.GenerateToken(token.UserName, token.EmailID, token.EmployeeCode, token.RoleID);
                    var RefreshAccessToken = _TokenSvc.GenerateRefreshToken(token.UserName, token.EmailID, token.EmployeeCode, token.RoleID);
                    var RefreshToken = _TokenSvc.GetRefreshTokenStore(token.UserName, token.EmailID, token.EmployeeCode, token.RoleID, RefreshAccessToken);
                    _TokenStoreSvc.Save(RefreshToken);
                    Helper.AddNewColumnToDataTable(dt, "accessToken", typeof(string));
                    Helper.AddNewColumnToDataTable(dt, "refreshToken", typeof(string));
                    Helper.AddNewColumnToDataTable(dt, "TokenExpirationInMinutes", typeof(int));
                    Helper.AddNewColumnToDataTable(dt, "RefreshTokenExpiresAt", typeof(int));
                    Helper.AddNewColumnToDataTable(dt, "Status", typeof(string));
                    DataRow dr = null;
                    dr = dt.NewRow();
                    dr["accessToken"] = accessToken;
                    dr["refreshToken"] = RefreshAccessToken;
                    dr["TokenExpirationInMinutes"] = _TokenSvc.GetTokenExpirationInMinutes();
                    dr["RefreshTokenExpiresAt"] = _TokenSvc.GetRefreshTokenExpirationInDays();
                    dr["Status"] = Helper.ToProperCase(MessageTypeConstants.Success);
                    dt.Rows.Add(dr);
                    TimeSpan timeSpan = TimeSpan.FromMinutes(_TokenSvc.GetTokenExpirationInMinutes());
                    var expirationTime = DateTime.Now.AddHours(timeSpan.Hours);
                    await _TokenStoreSvc.SaveTokenInfoAsync(token.UserName, token.EmailID, token.EmployeeCode, token.RoleID, accessToken, true, expirationTime);
                    finalDataSet = Helper.ConvertDataTableToDataSet(dt.Copy());
                    finalDataSet.Tables[DataSetDataTableConstants.Table1].TableName = DataSetDataTableConstants.Table;
                    finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserLoginServiceLogClassName}.GetRefreshToken()", ex.Message, false);
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Logout.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> Logout(string EmailID, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string InvalidEmailIDErrorMessage = "Invalid Email ID. Enter The Valid Email ID !!!";
            string TableName = "USER_LOGOUT_RESPONSE";
            try
            {
                string strEmailID = string.Empty;
                cancellationToken.ThrowIfCancellationRequested();
                List<string> columns = new List<string> { "ID", "Status", "Message" };
                var ds = await _UserSharedSvc.GetUserDetails(EmailID, "CheckUserExist", cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(Convert.ToString(ds.Tables[0].Rows[0]["Status"].ToString()));
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        strEmailID = Convert.ToString(ds.Tables[0].Rows[0]["EmailID"].ToString());
                        int userId = Convert.ToInt32(ds.Tables[0].Rows[0]["Id"].ToString());
                        var TokenDetails = _TokenStoreSvc.GetTokenInfo(strEmailID);
                        if (TokenDetails == null)
                        {
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { EmailID, Helper.ToProperCase(MessageTypeConstants.Error), "Token Is Invalid or Expired Token !!!" }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        }
                        else
                        {
                            await _TokenStoreSvc.DeleteTokenAsync(EmailID);
                            var IsUpdated = await _UserSharedSvc.UpdateUserLoginOrLogoutDate(userId, "UpdateUserLogout", cancellationToken);
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { EmailID, Helper.ToProperCase(MessageTypeConstants.Success), "You Have Successfully Logged Out !!!" }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { EmailID, Helper.ToProperCase(MessageTypeConstants.Error), InvalidEmailIDErrorMessage }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { EmailID, Helper.ToProperCase(MessageTypeConstants.Error), InvalidEmailIDErrorMessage }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureServiceUsersLogNamespace, $"{UserLoginServiceLogClassName}.Logout()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }
    }
}
