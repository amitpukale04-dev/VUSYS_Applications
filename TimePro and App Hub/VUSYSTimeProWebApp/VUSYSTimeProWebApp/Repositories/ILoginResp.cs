using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using SimpleCrypto;
using System.Data;
using VUSYSTimeProWebApp.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class ILoginResp : ILogin
    {
        private readonly DAC objDAC;
        private readonly ICryptoService _cryptoService = new PBKDF2();
        private const int SALT_SIZE = 16;
        private const int HASH_ITERATIONS = 100;

        public ILoginResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get User Login Details.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="session"></param>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="Password"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        public async Task<string> GetUserLoginDetails(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, ISession session, string UserName, string EmailID, string Password, string Status)
        {
            string strMessage = string.Empty;

            List<UsersM> lstUsers = new List<UsersM>();
            DataTable dt = new DataTable();
            DataTable _dt = new DataTable();
            try
            {
                DataSet ds = GetPassword(UserName, EmailID, string.Empty);
                _dt = ds.Tables[0];
                if (_dt.Rows.Count > 0)
                {
                    bool IsUserActive = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsUserActive"]);
                    bool IsEmailVerified = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsEmailVerified"]);
                    if (IsUserActive && IsEmailVerified)
                    {
                        string PasswordSalt = Convert.ToString(ds.Tables[0].Rows[0]["PasswordSalt"].ToString());
                        string DBPassword = Convert.ToString(ds.Tables[0].Rows[0]["Password"].ToString());
                        string strHashPassword = _cryptoService.Compute(Password, PasswordSalt);
                        if (DBPassword == strHashPassword)
                        {
                            string strStatus = "GetAll";
                            strMessage = await ValidateAndGetUserLoginDetails(webHostEnvironment, httpContextAccessor, session, UserName, EmailID, strHashPassword, strStatus);
                        }
                        else
                        {
                            strMessage = GetLoginMessage(IsUserActive, IsEmailVerified, false);
                        }
                    }
                    else
                    {
                        strMessage = GetLoginMessage(IsUserActive, IsEmailVerified, false);
                    }
                }
                else
                {
                    strMessage = GetLoginMessage(false, false, false);
                }
            }
            catch (Exception ex)
            {
                strMessage = "Unable to Connect DB/Unexepcted Error Occurred.";
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.GetUserLoginDetails()", ex.Message, false);
            }
            return strMessage;
        }

        /// <summary>
        /// This Method Is Used To Get Login Message.
        /// </summary>
        /// <param name="IsUserActive"></param>
        /// <param name="IsEmailVerified"></param>
        /// <param name="HasData"></param>
        /// <returns></returns>
        public string GetLoginMessage(bool IsUserActive, bool IsEmailVerified, bool HasData)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (IsUserActive == false && IsEmailVerified == false)
                {
                    ReturnResult = "InValid User/In-Active User !!!";
                }
                if (IsUserActive == false && IsEmailVerified == true)
                {
                    ReturnResult = "In-Active User !!!";
                }
                if (IsUserActive == true && IsEmailVerified == false)
                {
                    ReturnResult = "Email ID is Not Verified. Please Verify Your Email ID !!!";
                }
                if (IsUserActive == true && IsEmailVerified == true && HasData == false)
                {
                    ReturnResult = "Invalid Password. Enter The Valid Password !!!";
                }
            }
            catch (Exception ex)
            {
                ReturnResult = string.Empty;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.GetLoginMessage()", ex.Message, false);
            }
            return ReturnResult;
        }

        /// <summary>
        /// This Method is Used to Validate and Get User Login Details
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="session"></param>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="strHashPassword"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        public async Task<string> ValidateAndGetUserLoginDetails(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, ISession session,  string UserName, string EmailID, string strHashPassword, string Status)
        {
            string Message = string.Empty;
            try
            {
                DataTable dt = new DataTable();
                List<UsersM> lstUsers = new List<UsersM>();
                var parameters = new Dictionary<string, object>
                {
                    { "@UserName", UserName },
                    { "@EmailID", EmailID },
                    { "@Password", strHashPassword },
                    { "@Status", Status }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_Users", parameters, CONST.DBRetType.DATATABLE) as DataTable;
                if(dt.Rows.Count > 0)
                {
                    lstUsers = Helper.ConvertDataTableToList<UsersM>(dt);
                    var userModel = lstUsers;
                    if (userModel.Count > 0)
                    {
                        UsersM _objUser = new UsersM();
                        _objUser.Id = userModel[0].Id;
                        _objUser.UserID = userModel[0].UserID;
                        _objUser.UserName = userModel[0].UserName;
                        _objUser.Password = userModel[0].Password;
                        _objUser.FullName = userModel[0].FullName;
                        _objUser.MobileNo = userModel[0].MobileNo;
                        _objUser.UserEmailID = userModel[0].UserEmailID;
                        _objUser.CreatedDate = userModel[0].CreatedDate;
                        _objUser.UpdatedDate = userModel[0].UpdatedDate;
                        _objUser.IsDeleted = userModel[0].IsDeleted;
                        _objUser.RoleID = userModel[0].RoleID;
                        _objUser.IsReset = userModel[0].IsReset;
                        _objUser.IsEnableTwoFactorAuthentication = userModel[0].IsEnableTwoFactorAuthentication;
                        _objUser.IsPasswordExpiryLinkAvailable = userModel[0].IsPasswordExpiryLinkAvailable;
                        _objUser.LoginDate = userModel[0].LoginDate;
                        _objUser.LogoutDate = userModel[0].LogoutDate;
                        _objUser.LastLoginDate = userModel[0].LastLoginDate;
                        _objUser.LastLogoutDate = userModel[0].LastLogoutDate;
                        _objUser.UserRole = userModel[0].UserRole;
                        _objUser.EmployeeCode = userModel[0].EmployeeCode;
                        _objUser.IsApprover = userModel[0].IsApprover;
                        if (userModel[0].UserImageFileName == "")
                        {
                            _objUser.UserImageFileName = "Default_Male.png";
                        }
                        else
                        {
                            _objUser.UserImageFileName = userModel[0].UserImageFileName;
                        }
                        if (userModel[0].UserImage.Length > 0)
                        {
                            _objUser.UserImage = userModel[0].UserImage;
                        }
                        else
                        {
                            string DefaultUserImageFileName = "Default_Male.png";
                            string webRootPath = webHostEnvironment.WebRootPath;
                            var fullPath = webRootPath + "\\Images\\" + DefaultUserImageFileName;
                            _objUser.UserImage = File.ReadAllBytes(fullPath);
                        }
                        _objUser.IsUser = userModel[0].IsUser == true ? true : false;
                        var IsUpdated = await UpdateUserLoginOrLogoutDate(userModel[0].Id, "UpdateUserLogin");
                        var DisplayMenuList = await GetDisplayMenuList(Convert.ToInt32(userModel[0].RoleID));
                        var AllMenuList = await GetAllMenuList(Convert.ToInt32(userModel[0].RoleID));
                        string strBaseURL = CommonHelper.GetBaseURL(httpContextAccessor);
                        CommonHelper.SetJsonSessionObject(session, "VUSYSTimeProBaseURL", strBaseURL);
                        if (DisplayMenuList.Count > 0 && AllMenuList.Count > 0)
                        {
                            List<MainMenu> MenuList = AllMenuList.ToList();
                            List<MenusM> SubMenuList = new List<MenusM>();
                            MenusM _objMenus;
                            foreach (var subList in MenuList)
                            {
                                foreach (var item in subList.SubMenuList)
                                {
                                    _objMenus = new MenusM();
                                    _objMenus.Id = item.Id;
                                    _objMenus.MenuName = item.MenuName;
                                    _objMenus.Controller = item.Controller;
                                    _objMenus.Action = item.Action;
                                    _objMenus.MainMenuId = item.MainMenuId;
                                    _objMenus.MenuTitle = item.MenuTitle;
                                    _objMenus.MenuDescription = item.MenuDescription;
                                    _objMenus.MenuDisplayText = item.MenuDisplayText;
                                    _objMenus.MenuCssFont = item.MenuCssFont;
                                    _objMenus.MenuListClassName = item.MenuListClassName;
                                    _objMenus.MenuLinkClassName = item.MenuLinkClassName;
                                    _objMenus.MenuIClassName = item.MenuIClassName;
                                    _objMenus.MenuParam = item.MenuParam;
                                    _objMenus.UserId = item.UserId;
                                    _objMenus.AssignedBy = item.AssignedBy;
                                    _objMenus.CreatedDate = item.CreatedDate;
                                    _objMenus.MMode = item.MMode;
                                    _objMenus.Category = item.Category;
                                    _objMenus.AccessId = item.AccessId;
                                    _objMenus.ToolTipText = item.ToolTipText;
                                    _objMenus.SortOrder = item.SortOrder;
                                    _objMenus.IsDisplayedInMenu = item.IsDisplayedInMenu;
                                    SubMenuList.Add(_objMenus);
                                }
                            }
                            CommonHelper.SetJsonSessionObject(session, "UserMenuSession", DisplayMenuList);
                            CommonHelper.SetJsonSessionObject(session, "SubMenuListSession", SubMenuList);
                            bool IsChangePasswordAccess = CommonHelper.CheckPagePermission(session, "ChangePassword", "ChangePassword");
                            CommonHelper.SetJsonSessionObject(session, "IsChangePasswordAccess", IsChangePasswordAccess);
                            CommonHelper.SetJsonSessionObject(session, "UserSession", _objUser);
                            Message = "Success";
                        }
                        else
                        {
                            Message = "Failed";
                        }
                    }
                    else
                    {
                        Message = "InValid User/In-Active User.";
                    }
                }
                else
                {
                    Message = "InValid User/In-Active User.";
                }
            }
            catch (Exception ex)
            {
                Message = "Unable to Connect DB/Unexepcted Error Occurred.";
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.ValidateAndGetUserLoginDetails()", ex.Message, false);
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Reset The Password.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="_objForgotPassword"></param>
        /// <returns></returns>
		public async Task<string> ResetPassword(AppDbContext db, IWebHostEnvironment webHostEnvironment, ForgotPassword _objForgotPassword)
        {
            DataTable dt = new DataTable();
            string Message = string.Empty;
            try
            {
                dt = UpdatePassword(_objForgotPassword.UserEmailID, string.Empty, string.Empty, "ResetPassword");
                if (dt.Rows.Count > 0)
                {
                    Message = dt.Rows[0]["Message"].ToString();
                    string FullName = string.Empty;
                    int UserID = 0;
                    if (Message == "Success")
                    {
                        FullName = dt.Rows[0]["FullName"].ToString();
                        UserID = Convert.ToInt32(dt.Rows[0]["Id"].ToString());
                        var dtEmailConfigurationSettings = CommonHelper.GetEmailConfigurationSettings(db);
                        if(dtEmailConfigurationSettings.Rows.Count > 0)
                        {
                            string ResetPasswordEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                            string ResetPasswordEmailTemplatePath = ResetPasswordEmailTemplateFolderPath + "\\ResetPasswordEmailTemplate.html";

                            StreamReader rdrEmailBody = new StreamReader(ResetPasswordEmailTemplatePath);
                            string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                            string EmailBodyTemplate = txthtmlEmailBody;
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", FullName);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", _objForgotPassword.LinkURL);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#PasswordLinkExpiryInMinutes#", CommonHelper.KeyOption.ResetPasswordLinkExpiryInMinutes);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                            string MailSubject = "Timesheet Pro - Reset Password Request";
                            string MailBody = EmailBodyTemplate;
                            string FromEmailID = dtEmailConfigurationSettings.Rows[0]["FromEmail"].ToString();
                            string Password = dtEmailConfigurationSettings.Rows[0]["Password"].ToString();
                            string SmtpHost = dtEmailConfigurationSettings.Rows[0]["SmtpHost"].ToString();
                            string PortNo = dtEmailConfigurationSettings.Rows[0]["PortNo"].ToString();

                            SendEmailRequestParam objSendEmailRequestParam = new SendEmailRequestParam();
                            objSendEmailRequestParam.FromDisplayName = CommonHelper.EMAIL_DISPLAY_NAME;
                            objSendEmailRequestParam.FromEmailID = FromEmailID;
                            objSendEmailRequestParam.Password = Password;
                            objSendEmailRequestParam.SmtpHost = SmtpHost;
                            objSendEmailRequestParam.PortNo = PortNo;
                            objSendEmailRequestParam.ToEmail = _objForgotPassword.UserEmailID;
                            objSendEmailRequestParam.EmailSubject = MailSubject;
                            objSendEmailRequestParam.EmailBody = MailBody;
                            bool IsEmailSent = Helper.SendEmail(objSendEmailRequestParam);
                            if(!IsEmailSent)
                            {
                                Message = Helper.EmailFailed;
                            }
                            else
                            {
                                var rowsAffected = db.Database.ExecuteSqlRaw("UPDATE VUSYS_Users SET IsPasswordExpiryLinkAvailable = {0}, PasswordExpiryLinkDateTime = {1} WHERE EmailID = {2} AND IsDeleted = {3}", true, DateTime.Now.AddMinutes(Convert.ToDouble(CommonHelper.KeyOption.ResetPasswordLinkExpiryInMinutes)), _objForgotPassword.UserEmailID, false);
                                Message = "Success";
                            }
                        }
                    }
                    else
                    {
                        return Message;
                    }
                }
            }
            catch (Exception ex)
            {
                Message = Helper.ErrorOccurred;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.ResetPassword()", ex.Message, false);
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Send OTP For Authentication.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="db"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="_objUsers"></param>
        /// <returns></returns>
		public async Task<string> OTPAuthentication(ISession session, AppDbContext db, IWebHostEnvironment webHostEnvironment, UsersM _objUsers)
        {
            string Message = string.Empty;
            try
            {
                var dtEmailConfigurationSettings = CommonHelper.GetEmailConfigurationSettings(db);
                if (dtEmailConfigurationSettings.Rows.Count > 0)
                {
                    string OTPAuthenticationEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                    string OTPAuthenticationEmailTemplatePath = OTPAuthenticationEmailTemplateFolderPath + "\\OTPAuthenticationEmailTemplate.html";
                    string OTP = Helper.GenerateOTP(4);
                    StreamReader rdrEmailBody = new StreamReader(OTPAuthenticationEmailTemplatePath);
                    string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                    string EmailBodyTemplate = txthtmlEmailBody;
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", _objUsers.FullName);
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#OTP#", OTP);
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#OTPExpiryInMinutes#", CommonHelper.KeyOption.OTPExpiryInMinutes);
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                    string MailSubject = "Timesheet Pro - OTP Authentication";
                    string MailBody = EmailBodyTemplate;
                    string FromEmailID = dtEmailConfigurationSettings.Rows[0]["FromEmail"].ToString();
                    string Password = dtEmailConfigurationSettings.Rows[0]["Password"].ToString();
                    string SmtpHost = dtEmailConfigurationSettings.Rows[0]["SmtpHost"].ToString();
                    string PortNo = dtEmailConfigurationSettings.Rows[0]["PortNo"].ToString();

                    SendEmailRequestParam objSendEmailRequestParam = new SendEmailRequestParam();
                    objSendEmailRequestParam.FromDisplayName = CommonHelper.EMAIL_DISPLAY_NAME;
                    objSendEmailRequestParam.FromEmailID = FromEmailID;
                    objSendEmailRequestParam.Password = Password;
                    objSendEmailRequestParam.SmtpHost = SmtpHost;
                    objSendEmailRequestParam.PortNo = PortNo;
                    objSendEmailRequestParam.ToEmail = _objUsers.UserEmailID;
                    objSendEmailRequestParam.EmailSubject = MailSubject;
                    objSendEmailRequestParam.EmailBody = MailBody;
                    bool IsEmailSent = Helper.SendEmail(objSendEmailRequestParam);
                    if (!IsEmailSent)
                    {
                        Message = Helper.EmailFailed;
                    }
                    else
                    {
                        Message = "OTP Has Been Sent To Your Registered Email ID !!!";
                        CommonHelper.SetJsonSessionObject(session, $"OTP_User_" + _objUsers.Id, OTP);
                        CommonHelper.SetJsonSessionObject(session, $"OTP_User_" + _objUsers.Id + "_ExpiryTime", DateTime.Now.AddMinutes(Convert.ToDouble(CommonHelper.KeyOption.OTPExpiryInMinutes)));
                    }
                }
            }
            catch (Exception ex)
            {
                Message = Helper.ErrorOccurred;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.OTPAuthentication()", ex.Message, false);
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Change The Password.
        /// </summary>
        /// <param name="_objChangePassword"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        public async Task<string> ChangePassword(ChangePassword _objChangePassword, string Status)
		{
			DataTable dt = new DataTable();
			string NewPasswordEncrypted = string.Empty;
			string OldPasswordEncrypted = string.Empty;
			string UserOldPassword = string.Empty;
			string strPasswordSalt = string.Empty;
			string Message = string.Empty;
			try
			{
				DataSet _dsUserPassword;
				DataTable _dtUserPassword;
				_dsUserPassword = GetPassword(_objChangePassword.UserName, _objChangePassword.EmailID, string.Empty);
				_dtUserPassword = _dsUserPassword.Tables[0];
				foreach (DataRow row in _dtUserPassword.Rows)
				{
					UserOldPassword = row["Password"].ToString();
					strPasswordSalt = row["PasswordSalt"].ToString();
				}
				OldPasswordEncrypted = _cryptoService.Compute(_objChangePassword.OldPassword, strPasswordSalt);
				if (UserOldPassword == OldPasswordEncrypted)
				{
					NewPasswordEncrypted = _cryptoService.Compute(_objChangePassword.NewPassword, SALT_SIZE, HASH_ITERATIONS);
                    string PasswordSalt = _cryptoService.Salt;
                    dt = UpdatePassword(_objChangePassword.EmailID, NewPasswordEncrypted, PasswordSalt, Status);
                    if(dt.Rows.Count > 0)
                    {
                        Message = dt.Rows[0]["Message"].ToString();
                    }
                }
				else
				{
					Message = "Old Password Is Incorrect.";
				}
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.ChangePassword()", ex.Message, false);
				throw;
			}
			return Message;
		}

        /// <summary>
        /// This Method is Used to Change The Password On Reset.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="NewPassword"></param>
        /// <returns></returns>
        public async Task<string> ChangePasswordOnReset(string EmailID, string NewPassword)
        {
            DataTable dt = new DataTable();
            string NewPasswordEncrypted = string.Empty;
            string OldPasswordEncrypted = string.Empty;
            string UserOldPassword = string.Empty;
            string strPasswordSalt = string.Empty;
            string Message = string.Empty;
            try
            {
                NewPasswordEncrypted = _cryptoService.Compute(NewPassword, SALT_SIZE, HASH_ITERATIONS);
                string PasswordSalt = _cryptoService.Salt;
                dt = UpdatePassword(EmailID, NewPasswordEncrypted, PasswordSalt, "Single");
                if (dt.Rows.Count > 0)
                {
                    Message = dt.Rows[0]["Message"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.ChangePasswordOnReset()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Reset The Password.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <returns></returns>
		public DataTable UpdatePassword(string EmailID, string NewPasswordEncrypted, string PasswordSalt, string Status)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@EmailID", EmailID },
                    { "@Password", NewPasswordEncrypted },
                    { "@PasswordSalt", PasswordSalt },
                    { "@Param", Status }
                };
                dt = objDAC.ExecuteStoredProcedureWithMultipleParameters("usp_VUSYS_UpdateResetPassword", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.UpdatePassword()", ex.Message, false);
                throw;
            }
            return dt;
        }

        /// <summary>
        /// This Method is Used to Get the Password.
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        public DataSet GetPassword(string UserName, string EmailID, string Password)
        {
            DataSet ds = new DataSet();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@UserName", UserName },
                    { "@EmailID", EmailID },
                    { "@Password", Password },
                    { "@Status", "GetPassword" }
                };
                ds = objDAC.ExecuteStoredProcedureWithMultipleParameters("usp_VUSYS_Users", parameters, CONST.DBRetType.DATASET) as DataSet;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.GetPassword()", ex.Message, false);
                throw;
            }
            return ds;
        }

        /// <summary>
        /// This Method is Used to Get Main Menu List for Display.
        /// </summary>
        /// <param name="RoleID"></param>
        /// <returns></returns>
        public async Task<List<MainMenu>> GetDisplayMenuList(int RoleID)
        {
            List<MainMenu> lstMainMenu = new List<MainMenu>();
            MainMenu _obj = new MainMenu();
            DataTable dt = new DataTable();
            try
            {
                int ID = 0;
                SqlConnection con = new SqlConnection(objDAC.connString);
                SqlCommand cmd = new SqlCommand("usp_VUSYS_GetMainMenuList", con);
                cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new SqlParameter("@ID", ID));
				cmd.Parameters.Add(new SqlParameter("@RoleId", RoleID));
				cmd.Parameters.Add(new SqlParameter("@Param", "GetMainMenuMasterListByRoleId"));
				con.Open();
                SqlDataReader rd = await cmd.ExecuteReaderAsync();
                while (rd.Read())
                {
                    _obj = new MainMenu();
                    _obj.Id = Convert.ToInt32(rd["MainMenuId"]);
                    _obj.MainMenuName = rd["MainMenuName"].ToString();
                    _obj.MainMenuListClassName = rd["MainMenuListClassName"].ToString();
                    _obj.MainMenuLinkClassName = rd["MainMenuLinkClassName"].ToString();
                    _obj.MainMenuIClassName = rd["MainMenuIClassName"].ToString();
                    _obj.MainMenuDisplayText = rd["MainMenuDisplayText"].ToString();
                    _obj.MainMenuIClassNameDisplayText = rd["MainMenuIClassNameDisplayText"].ToString();
                    _obj.MainMenuSpanClass = rd["MainMenuSpanClass"].ToString();
                    _obj.IsDisplayedInMenu = Convert.ToBoolean(rd["IsDisplayedInMenu"]);
                    _obj.SubMenuList = await GetSubMenuList(RoleID, _obj.Id, _obj.MainMenuName, "GetSubMenuList");
                    lstMainMenu.Add(_obj);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.GetDisplayMenuList()", ex.Message, false);
                throw;
            }
            return lstMainMenu;
        }

        /// <summary>
        /// This Method is Used to Get All Sub Menu List.
        /// </summary>
        /// <param name="RoleID"></param>
        /// <returns></returns>
        public async Task<List<MainMenu>> GetAllMenuList(int RoleID)
        {
            List<MainMenu> lstMainMenu = new List<MainMenu>();
            MainMenu _obj = new MainMenu();
            DataTable dt = new DataTable();
            int ID = 0;
            SqlConnection con = new SqlConnection(objDAC.connString);
            try
            {
                SqlCommand cmd = new SqlCommand("usp_VUSYS_GetMainMenuList", con);
                cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new SqlParameter("@ID", ID));
				cmd.Parameters.Add(new SqlParameter("@RoleId", RoleID));
				cmd.Parameters.Add(new SqlParameter("@Param", "GetMainMenuMasterListByRoleId"));
				con.Open();
                SqlDataReader rd = await cmd.ExecuteReaderAsync();
                while (rd.Read())
                {
                    _obj = new MainMenu();
                    _obj.Id = Convert.ToInt32(rd["MainMenuId"]);
                    _obj.MainMenuName = rd["MainMenuName"].ToString();
                    _obj.MainMenuListClassName = rd["MainMenuListClassName"].ToString();
                    _obj.MainMenuLinkClassName = rd["MainMenuLinkClassName"].ToString();
                    _obj.MainMenuIClassName = rd["MainMenuIClassName"].ToString();
                    _obj.MainMenuDisplayText = rd["MainMenuDisplayText"].ToString();
                    _obj.MainMenuIClassNameDisplayText = rd["MainMenuIClassNameDisplayText"].ToString();
                    _obj.MainMenuSpanClass = rd["MainMenuSpanClass"].ToString();
                    _obj.SubMenuList = await GetSubMenuList(RoleID, _obj.Id, _obj.MainMenuName, "GetAllSubMenuList");
                    lstMainMenu.Add(_obj);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.GetAllMenuList()", ex.Message, false);
                throw;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return lstMainMenu;
        }

        /// <summary>
        /// This Method is Used to Get Sub Menu List Based on Role and Main Menu.
        /// </summary>
        /// <param name="RoleId"></param>
        /// <param name="MainMenuId"></param>
        /// <param name="MainMenuName"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<MenusM>> GetSubMenuList(int RoleId, int MainMenuId, string MainMenuName, string Type)
        {
            List<MenusM> lstMenu = new List<MenusM>();
            MenusM _obj = new MenusM();
            SqlConnection con = new SqlConnection(objDAC.connString);
            try
            {
                SqlCommand cmd = new SqlCommand("usp_VUSYS_GetMenuList", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@RoleId", RoleId));
                cmd.Parameters.Add(new SqlParameter("@MainMenuId", MainMenuId));
                cmd.Parameters.Add(new SqlParameter("@Controller", string.Empty));
                cmd.Parameters.Add(new SqlParameter("@Action", string.Empty));
                cmd.Parameters.Add(new SqlParameter("@Type", Type));
                con.Open();
                SqlDataReader rd = await cmd.ExecuteReaderAsync();
                while (rd.Read())
                {
                    _obj = new MenusM();
                    _obj.Id = Convert.ToInt32(rd["MenuID"]);
                    _obj.MenuName = rd["MenuName"].ToString();
                    _obj.Controller = rd["Controller"].ToString();
                    _obj.Action = rd["Action"].ToString();
                    _obj.MenuTitle = rd["Title"].ToString();
                    _obj.MenuDescription = rd["Description"].ToString();
                    _obj.MenuDisplayText = rd["DisplayText"].ToString();
                    _obj.MenuCssFont = rd["CSSFont"].ToString();
                    _obj.MenuListClassName = rd["ListClassName"].ToString();
                    _obj.MenuLinkClassName = rd["LinkClassName"].ToString();
                    _obj.MenuIClassName = rd["IClassName"].ToString();
                    _obj.MainMenuId = rd["MainMenuId"].ToString();
                    _obj.MainMenuName = MainMenuName;
                    _obj.ToolTipText = rd["TOOLTIPTEXT"].ToString();
                    _obj.SortOrder = rd["SortOrder"].ToString();
                    _obj.IsDisplayedInMenu = Convert.ToBoolean(rd["IsDisplayedInMenu"]);
                    lstMenu.Add(_obj);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.GetSubMenuList()", ex.Message, false);
                throw;

            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return lstMenu;
        }

        /// <summary>
        /// This Method is Used to Update User Login or Logout Date.
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="Param"></param>
        /// <returns></returns>
        public async Task<bool> UpdateUserLoginOrLogoutDate(int UserID, string Param)
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
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_UserLastLoginLogout", parameters, CONST.DBRetType.DATATABLE) as DataTable;
                if(dt.Rows.Count > 0)
                {
                    IsUpdated = true;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ILoginResp.UpdateUserLoginOrLogoutDate()", ex.Message, false);
                throw;
            }
            return IsUpdated;
        }
    }
}
