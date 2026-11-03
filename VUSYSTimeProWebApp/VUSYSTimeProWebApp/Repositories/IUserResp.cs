using System.Data;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class IUserResp:IUser
    {
        private readonly DAC objDAC;

        public IUserResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get User List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<UsersM>> GetUserList(int ID, string Type)
        {
            List<UsersM> lstUsers = new List<UsersM>();
            DataTable dt = new DataTable();
            try
            {
                dt = await GetUsers(ID, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IUserResp.GetUserList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstUsers = Helper.ConvertDataTableToList<UsersM>(dt);
            }
            return lstUsers;
        }

        /// <summary>
        /// This Method is Used to Get User Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<UsersM> GetUserDetails(int? id)
        {
            UsersM _objUsersM = new UsersM();
            List<UsersM> lstUser = new List<UsersM>();
            DataTable dt = new DataTable();
            try
            {
                string Type = "GetUserDetails";
                dt = await GetUsers((int)id, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IUserResp.GetUserDetails()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstUser = Helper.ConvertDataTableToList<UsersM>(dt);
                _objUsersM = lstUser[0];
            }
            return _objUsersM;
        }

        /// <summary>
        /// This Method is Used to Get User List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<DataTable> GetUsers(int ID, string Type)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ID },
                    { "@Type", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetUsersList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IUserResp.GetUsers()", ex.Message, false);
                throw;
            }
            return dt;
        }

        /// <summary>
        /// This Method is Used to Save Update User Details.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="_objUsersM"></param>
        /// <returns></returns>
        public async Task<string> SaveUpdateUserDetails(AppDbContext db, IWebHostEnvironment webHostEnvironment, UsersM _objUsersM)
        {
            string Message = string.Empty;
            DataSet ds = new DataSet();
            try
            {
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objUsersM, "usp_VUSYS_SaveUpdateUserDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                    string EmailResponseMessage = string.Empty;
                    if (_objUsersM.Id == 0 && Message != "The User Details Already Exists.")
                    {
                        var dtEmailConfigurationSettings = CommonHelper.GetEmailConfigurationSettings(db);
                        if (dtEmailConfigurationSettings.Rows.Count > 0)
                        {
                            string UserRegistrationEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                            string UserRegistrationEmailTemplatePath = UserRegistrationEmailTemplateFolderPath + "\\UserRegistrationEmailTemplate.html";
                            StreamReader rdrEmailBody = new StreamReader(UserRegistrationEmailTemplatePath);
                            string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                            string EmailBodyTemplate = txthtmlEmailBody;
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", _objUsersM.FullName);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#Password#", _objUsersM.PlainPassword);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", _objUsersM.LinkURL);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                            string MailSubject = "Timesheet Pro - User Registered";
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
                            objSendEmailRequestParam.ToEmail = _objUsersM.UserEmailID;
                            objSendEmailRequestParam.EmailSubject = MailSubject;
                            objSendEmailRequestParam.EmailBody = MailBody;
                            bool IsEmailSent = Helper.SendEmail(objSendEmailRequestParam);
                            if (!IsEmailSent)
                            {
                                EmailResponseMessage = Message + " " + "But Failed To Send The Password. Please Contact Your Administrator !!!";
                            }
                            else
                            {
                                EmailResponseMessage = Message + " " + "Password Has Been Sent To Your Registered Email ID !!!";
                            }
                        }
                        Message = EmailResponseMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IUserResp.SaveUpdateUserDetails()", ex.Message, false);
                throw;
            }
            finally
            {
                objDAC.Dispose();
            }
            return Message;
        }
    }
}
