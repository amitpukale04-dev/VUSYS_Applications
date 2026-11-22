using System.Data;
using Microsoft.AspNetCore.DataProtection;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class ITimeSheetResp:ITimeSheet
    {
        private readonly DAC objDAC;

        public ITimeSheetResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get Timesheet List.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="FromTaskDate"></param>
        /// <param name="ToTaskDate"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<ProjectTimesheet>> GetTimesheetList(int ProjectTimeSheetID, string EmployeeCode, string FromTaskDate, string ToTaskDate, string Type)
        {
            List<ProjectTimesheet> lstProjectTimesheet = new List<ProjectTimesheet>();
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", ProjectTimeSheetID },
                    { "@EmployeeCode", EmployeeCode },
                    { "@FromTaskDate", FromTaskDate },
                    { "@ToTaskDate", ToTaskDate },
                    { "@ParamType", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetEmployeeProjectTimesheet", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.GetTimesheetList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProjectTimesheet = Helper.ConvertDataTableToList<ProjectTimesheet>(dt);
            }
            return lstProjectTimesheet;
        }

        /// <summary>
        /// This Method is Used to Save Timesheet Details.
        /// </summary>
        /// <param name="lstProjectTimesheet"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        public string SaveTimesheet(List<ProjectTimesheet> lstProjectTimesheet, ref string Status)
        {
            string Message = string.Empty;
            DataSet ds = new DataSet();
            try
            {
                if (lstProjectTimesheet.Count > 0)
                {
                    ds = objDAC.ExecuteStoredProcedureWithXMLListParameters(lstProjectTimesheet, lstProjectTimesheet.GetType(), lstProjectTimesheet, "usp_VUSYS_SaveUpdateProjectTimeSheetDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        Message = ds.Tables[0].Rows[0]["Message"].ToString();
                        Status = ds.Tables[0].Rows[0]["Status"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.SaveTimesheet()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Get Timesheet Submission List.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<ProjectTimesheetSubmissionApproveRejectReportList>> GetTimesheetSubmissionList(string EmployeeCode, string TimesheetMonth, int TimesheetYear, string Type)
        {
            List<ProjectTimesheetSubmissionApproveRejectReportList> lstProjectTimesheetSubmissionApproveRejectReport = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", 0 },
                    { "@EmployeeCode", EmployeeCode },
                    { "@TimeSheetMonth", TimesheetMonth },
                    { "@TimeSheetYear", TimesheetYear },
                    { "@ApproverEmailID", string.Empty },
                    { "@UserAccessToken", string.Empty },
                    { "@ParamType", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_EmployeeProjectTimesheetForSubmission", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.GetTimesheetSubmissionList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProjectTimesheetSubmissionApproveRejectReport = Helper.ConvertDataTableToList<ProjectTimesheetSubmissionApproveRejectReportList>(dt);
            }
            return lstProjectTimesheetSubmissionApproveRejectReport;
        }

        /// <summary>
        /// This Method is Used to Submit Timesheet Details For Approval.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="session"></param>
        /// <param name="protector"></param>
        /// <param name="db"></param>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="ApproveStatus"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public string SubmitTimesheetDetailsForApproval(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, ISession session, IDataProtector protector, AppDbContext db, int ProjectTimeSheetID, string TimesheetMonth, int TimesheetYear, string ApproveStatus, out string Status, out string Message)
        {
            DataSet ds = new DataSet();
            Status = string.Empty;
            Message = string.Empty;
            try
            {
                string ParamType = string.Empty;
                if(ApproveStatus == "Resend For Approval")
                {
                    ParamType = "ReSubmitTimesheetForApproval";
                }
                else
                {
                    ParamType = "SubmitTimesheetForApproval";
                }
                List<UsersM> UserDetails = DropdownHelper.GetApproverDetails(db);
                if (UserDetails != null && UserDetails.Count > 0)
                {
                    Guid newGuid = Guid.NewGuid();
                    var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(session, "UserSession");
                    var parameters = new Dictionary<string, object>
                    {
                        { "@ProjectTimeSheetID", ProjectTimeSheetID },
                        { "@EmployeeCode", SessionDetails.EmployeeCode },
                        { "@TimeSheetMonth", TimesheetMonth },
                        { "@TimeSheetYear", TimesheetYear },
                        { "@ApproverEmailID", UserDetails[0].EmailID },
                        { "@UserAccessToken", newGuid.ToString() },
                        { "@ParamType", ParamType }
                    };

                    ds = objDAC.ExecuteStoredProcedureWithMultipleParameters("usp_VUSYS_EmployeeProjectTimesheetForSubmission", parameters, CONST.DBRetType.DATASET) as DataSet;
                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        Message = ds.Tables[0].Rows[0]["Message"].ToString();
                        Status = ds.Tables[0].Rows[0]["Status"].ToString();
                        if (Status == "Success")
                        {
                            string EmailResponseMessage = string.Empty;
                            var dtEmailConfigurationSettings = CommonHelper.GetEmailConfigurationSettings(db);
                            if (dtEmailConfigurationSettings.Rows.Count > 0)
                            {
                                string strBaseURL = CommonHelper.GetBaseURL(httpContextAccessor);
                                strBaseURL = strBaseURL + "/TimesheetApprovalRejection/ApproveReject?PTSID=" + protector.Protect(ProjectTimeSheetID.ToString()) + "&S=" + protector.Protect("Email") + "&ID=" + protector.Protect(newGuid.ToString()) + "&Code=" + protector.Protect(SessionDetails.EmployeeCode) + "&M=" + protector.Protect(TimesheetMonth) + "&Y=" + protector.Protect(TimesheetYear.ToString());
                                string TimesheetPendingEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                                string TimesheetPendingEmailTemplatePath = TimesheetPendingEmailTemplateFolderPath + "\\TimesheetPendingEmailTemplate.html";
                                StreamReader rdrEmailBody = new StreamReader(TimesheetPendingEmailTemplatePath);
                                string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                                string EmailBodyTemplate = txthtmlEmailBody;
                                EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverName#", UserDetails[0].FullName);
                                EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", SessionDetails.FullName);
                                EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonth#", TimesheetMonth);
                                EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetYear#", TimesheetYear.ToString());
                                EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", strBaseURL);
                                EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeEmailID#", SessionDetails.UserEmailID);
                                EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                                string MailSubject = "Timesheet Approval Request for " + SessionDetails.FullName + "–" + TimesheetMonth + " " + TimesheetYear;
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
                                objSendEmailRequestParam.ToEmail = UserDetails[0].EmailID;
                                objSendEmailRequestParam.EmailSubject = MailSubject;
                                objSendEmailRequestParam.EmailBody = MailBody;
                                bool IsEmailSent = Helper.SendEmail(objSendEmailRequestParam);
                                if (!IsEmailSent)
                                {
                                    EmailResponseMessage = Message + ". " + "However, We Encountered An Issue Sending The Approval Request Email To The Approver. Please Contact Your Administrator For Assistance !!!";
                                }
                                else
                                {
                                    EmailResponseMessage = Message + ", " + "and an Email Notification Has Been Sent to the Approver !!!";
                                }
                            }
                            Message = EmailResponseMessage;
                        }
                    }
                }
                else
                {
                    Message = "You Cannot Submit The Timesheet. Since Approver Is Not Available In the System. Please Contact Administrator.";
                    Status = "Error";
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.SubmitTimesheetDetailsForApproval()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Send Timesheet Submission Reminder Notification To Employee.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public string SendTimesheetSubmissionReminderNotificationToEmployee(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, AppDbContext db, string EmployeeCode, string TimesheetMonth, int TimesheetYear, out string Status, out string Message)
        {
            DataSet ds = new DataSet();
            Status = string.Empty;
            Message = string.Empty;
            try
            {
                List<UsersM> UserDetails = DropdownHelper.GetUserDetailsByEmployeeCode(db, EmployeeCode);
                if (UserDetails != null && UserDetails.Count > 0)
                {
                    var dtEmailConfigurationSettings = CommonHelper.GetEmailConfigurationSettings(db);
                    if (dtEmailConfigurationSettings.Rows.Count > 0)
                    {
                        string TimesheetSubmissionReminderNotificationEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                        string TimesheetSubmissionReminderNotificationEmailTemplatePath = TimesheetSubmissionReminderNotificationEmailTemplateFolderPath + "\\TimesheetSubmissionReminderNotificationEmailTemplate.html";
                        StreamReader rdrEmailBody = new StreamReader(TimesheetSubmissionReminderNotificationEmailTemplatePath);
                        string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                        string EmailBodyTemplate = txthtmlEmailBody;
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonth#", TimesheetMonth);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetYear#", TimesheetYear.ToString());
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                        string MailSubject = "Reminder: Timesheet Submission Required for " + TimesheetMonth + " " + TimesheetYear;
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
                        objSendEmailRequestParam.ToEmail = UserDetails[0].EmailID;
                        objSendEmailRequestParam.EmailSubject = MailSubject;
                        objSendEmailRequestParam.EmailBody = MailBody;
                        bool IsEmailSent = Helper.SendEmail(objSendEmailRequestParam);
                        if (!IsEmailSent)
                        {
                            Status = "Error";
                            Message = "We Encountered An Issue Sending An Email Notification To The Employee !!!";
                        }
                        else
                        {
                            Status = "Success";
                            Message = "An Email Notification Has Been Sent to the Employee !!!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.SendTimesheetSubmissionReminderNotificationToEmployee()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Check Timesheet Approval Status.
        /// </summary>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public string CheckTimesheetApprovalStatus(ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, out string Status, out string Message)
        {
            DataSet ds = new DataSet();
            Status = string.Empty;
            Message = string.Empty;
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", 0 },
                    { "@EmployeeCode", _objProjectTimesheetApproveRejection.EmployeeCode },
                    { "@TimeSheetMonth", _objProjectTimesheetApproveRejection.TimesheetMonth },
                    { "@TimeSheetYear", _objProjectTimesheetApproveRejection.TimesheetYear },
                    { "@IsApproved", 0 },
                    { "@Remarks", string.Empty },
                    { "@ApprovedOrRejectedBy", string.Empty },
                    { "@UserAccessToken", string.Empty },
                    { "@Source", string.Empty },
                    { "@ParamType", "CheckApprovalStatus" }
                };
                ds = objDAC.ExecuteStoredProcedureWithMultipleParameters("usp_VUSYS_EmployeeProjectTimesheetApproveReject", parameters, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                    Status = ds.Tables[0].Rows[0]["Status"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.ApproveRejectTimesheet()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Approve Reject Timesheet.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="session"></param>
        /// <param name="db"></param>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="Status"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public string ApproveRejectTimesheet(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, ISession session, AppDbContext db, ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, out string Status, out string Message)
        {
            DataSet ds = new DataSet();
            Status = string.Empty;
            Message = string.Empty;
            try
            {
                List<UsersM> ApproverDetails = DropdownHelper.GetApproverDetails(db);
                if (ApproverDetails != null && ApproverDetails.Count > 0)
                {
                    bool IsApproved = (_objProjectTimesheetApproveRejection.ProjectTimeSheetApprovalStatusID == "1") ? true : false;
                    List<UsersM> UserDetails = DropdownHelper.GetUserDetailsByEmployeeCode(db, _objProjectTimesheetApproveRejection.EmployeeCode);
                    if (UserDetails != null && UserDetails.Count > 0)
                    {
                        var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(session, "UserSession");
                        var parameters = new Dictionary<string, object>
                        {
                            { "@ProjectTimeSheetID", _objProjectTimesheetApproveRejection.ProjectTimeSheetID },
                            { "@EmployeeCode", _objProjectTimesheetApproveRejection.EmployeeCode },
                            { "@TimeSheetMonth", _objProjectTimesheetApproveRejection.TimesheetMonth },
                            { "@TimeSheetYear", _objProjectTimesheetApproveRejection.TimesheetYear },
                            { "@IsApproved", IsApproved },
                            { "@Remarks", _objProjectTimesheetApproveRejection.Remarks },
                            { "@ApprovedOrRejectedBy", ApproverDetails[0].Id },
                            { "@UserAccessToken",  _objProjectTimesheetApproveRejection.UserToken },
                            { "@Source", _objProjectTimesheetApproveRejection.Source },
                            { "@ParamType", "ApproveRejectTimesheet" }
                        };

                        ds = objDAC.ExecuteStoredProcedureWithMultipleParameters("usp_VUSYS_EmployeeProjectTimesheetApproveReject", parameters, CONST.DBRetType.DATASET) as DataSet;
                        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            Message = ds.Tables[0].Rows[0]["Message"].ToString();
                            Status = ds.Tables[0].Rows[0]["Status"].ToString();

                            if (Status == "Success")
                            {
                                string EmailResponseMessage = string.Empty;
                                var dtEmailConfigurationSettings = CommonHelper.GetEmailConfigurationSettings(db);
                                if (dtEmailConfigurationSettings.Rows.Count > 0)
                                {
                                    string EmailBodyTemplate = string.Empty;
                                    string MailSubject = string.Empty;
                                    if (IsApproved)
                                    {
                                        string TimesheetApprovedEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                                        string TimesheetApprovedEmailTemplatePath = TimesheetApprovedEmailTemplateFolderPath + "\\TimesheetApprovedEmailTemplate.html";
                                        StreamReader rdrEmailBody = new StreamReader(TimesheetApprovedEmailTemplatePath);
                                        string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                                        EmailBodyTemplate = txthtmlEmailBody;
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Header#", "Approval Notification: " + _objProjectTimesheetApproveRejection.TimesheetMonth + " " + _objProjectTimesheetApproveRejection.TimesheetYear);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonthYear#", _objProjectTimesheetApproveRejection.TimesheetMonth + "-" + _objProjectTimesheetApproveRejection.TimesheetYear);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Remarks#", _objProjectTimesheetApproveRejection.Remarks);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());
                                        MailSubject = "Timesheet Approved – " + _objProjectTimesheetApproveRejection.TimesheetMonth + " " + _objProjectTimesheetApproveRejection.TimesheetYear;
                                    }
                                    else
                                    {
                                        string TimesheetRejectedEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                                        string TimesheetRejectedEmailTemplatePath = TimesheetRejectedEmailTemplateFolderPath + "\\TimesheetRejectedEmailTemplate.html";
                                        StreamReader rdrEmailBody = new StreamReader(TimesheetRejectedEmailTemplatePath);
                                        string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                                        EmailBodyTemplate = txthtmlEmailBody;

                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Header#", "Rejection Alert: Timesheet for " + _objProjectTimesheetApproveRejection.TimesheetMonth + " " + _objProjectTimesheetApproveRejection.TimesheetYear);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonthYear#", _objProjectTimesheetApproveRejection.TimesheetMonth + "-" + _objProjectTimesheetApproveRejection.TimesheetYear);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Remarks#", _objProjectTimesheetApproveRejection.Remarks);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());
                                        MailSubject = "Rejection Alert: Timesheet for " + _objProjectTimesheetApproveRejection.TimesheetMonth + " " + _objProjectTimesheetApproveRejection.TimesheetYear;
                                    }
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
                                    objSendEmailRequestParam.ToEmail = UserDetails[0].EmailID;
                                    objSendEmailRequestParam.EmailSubject = MailSubject;
                                    objSendEmailRequestParam.EmailBody = MailBody;
                                    bool IsEmailSent = Helper.SendEmail(objSendEmailRequestParam);
                                    if (!IsEmailSent)
                                    {
                                        EmailResponseMessage = Message + ". " + "However, We Encountered An Issue Sending An Email To The User. Please Contact Your Administrator For Assistance !!!";
                                    }
                                    else
                                    {
                                        EmailResponseMessage = Message + ", " + "and an Email Notification Has Been Sent to the User !!!";
                                    }
                                }
                                Message = EmailResponseMessage;
                            }
                        }
                    }
                    else
                    {
                        Message = "You Cannot Approve/Reject The Timesheet. Since User Is Not Available In the System. Please Contact Administrator.";
                        Status = "Error";
                    }
                }
                else
                {
                    Message = "You Cannot Approve/Reject The Timesheet. Since Approver Is Not Available In the System. Please Contact Administrator.";
                    Status = "Error";
                }   
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.ApproveRejectTimesheet()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Download Timesheet.
        /// </summary>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="ParamType"></param>
        /// <returns></returns>
        public async Task<List<ProjectTimesheetDownloadList>> DownloadTimeSheet(ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, string ParamType)
        {
            List<ProjectTimesheetDownloadList> lstProjectTimesheetDownload = new List<ProjectTimesheetDownloadList>();
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", _objProjectTimesheetApproveRejection.ProjectTimeSheetID },
                    { "@EmployeeCode", _objProjectTimesheetApproveRejection.EmployeeCode },
                    { "@TimeSheetMonth", _objProjectTimesheetApproveRejection.TimesheetMonth },
                    { "@TimeSheetYear", _objProjectTimesheetApproveRejection.TimesheetYear },
                    { "@IsApproved", false },
                    { "@Remarks", string.Empty },
                    { "@ApprovedOrRejectedBy", string.Empty },
                    { "@UserAccessToken",  string.Empty },
                    { "@Source", string.Empty },
                    { "@ParamType", ParamType }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_EmployeeProjectTimesheetApproveReject", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.DownloadTimeSheet()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProjectTimesheetDownload = Helper.ConvertDataTableToList<ProjectTimesheetDownloadList>(dt);
            }
            return lstProjectTimesheetDownload;
        }

        /// <summary>
        /// This Method is Used to Get Project Allocated Consumed Report List.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <returns></returns>
        public async Task<List<ProjectAllocatedConsumedReportList>> GetProjectAllocatedConsumedReportList(int ProjectTimeSheetID)
        {
            List<ProjectAllocatedConsumedReportList> lstProjectAllocatedConsumedReportList = new List<ProjectAllocatedConsumedReportList>();
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ProjectTimeSheetID }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetProjectAllocatedConsumedHoursReport", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.GetProjectAllocatedConsumedReportList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProjectAllocatedConsumedReportList = Helper.ConvertDataTableToList<ProjectAllocatedConsumedReportList>(dt);
            }
            return lstProjectAllocatedConsumedReportList;
        }


        /// <summary>
        /// This Method is Used to Get Leave Report List.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<LeaveReportList>> GetLeaveReportList(string EmployeeCode, string TimesheetMonth, int TimesheetYear, string Type)
        {
            List<LeaveReportList> lstLeaveReport = new List<LeaveReportList>();
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", 0 },
                    { "@EmployeeCode", EmployeeCode },
                    { "@TimeSheetMonth", TimesheetMonth },
                    { "@TimeSheetYear", TimesheetYear },
                    { "@ApproverEmailID", string.Empty },
                    { "@UserAccessToken", string.Empty },
                    { "@ParamType", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_EmployeeProjectTimesheetForSubmission", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Repositories", "ITimeSheetResp.GetLeaveReportList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstLeaveReport = Helper.ConvertDataTableToList<LeaveReportList>(dt);
            }
            return lstLeaveReport;
        }
    }
}
