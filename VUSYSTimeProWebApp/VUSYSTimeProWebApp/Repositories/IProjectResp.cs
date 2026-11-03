using System.Data;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class IProjectResp:IProject
    {
        private readonly DAC objDAC;

        public IProjectResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get Project List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<ProjectM>> GetProjectList(int ID, string Type)
        {
            List<ProjectM> lstProject = new List<ProjectM>();
            DataTable dt = new DataTable();
            try
            {
                dt = await GetProject(ID, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IProjectResp.GetProjectList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProject = Helper.ConvertDataTableToList<ProjectM>(dt);
            }
            return lstProject;
        }

        /// <summary>
        /// This Method is Used to Get Project Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ProjectM> GetProjectDetails(int? id)
        {
            ProjectM _objProjectM = new ProjectM();
            List<ProjectM> lstProjectM = new List<ProjectM>();
            DataTable dt = new DataTable();
            try
            {
                string Type = "GetProjectDetails";
                dt = await GetProject((int)id, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IProjectResp.GetProjectDetails()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProjectM = Helper.ConvertDataTableToList<ProjectM>(dt);
                _objProjectM = lstProjectM[0];
            }
            return _objProjectM;
        }

        /// <summary>
        /// This Method is Used to Get Project List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<DataTable> GetProject(int ID, string Type)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ID },
                    { "@Type", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetProjectList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IProjectResp.GetProject()", ex.Message, false);
                throw;
            }
            return dt;
        }

        /// <summary>
        /// This Method is Used to Save Update Project Details.
        /// </summary>
        /// <param name="_objProjectM"></param>
        /// <returns></returns>
        public async Task<string> SaveUpdateProjectDetails(ProjectM _objProjectM)
        {
            string Message = string.Empty;
            DataSet ds = new DataSet();
            try
            {
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objProjectM, "usp_VUSYS_SaveUpdateProjectDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IProjectResp.SaveUpdateProjectDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Notify Project Allocation To Employee.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="ID"></param>
        /// <param name="DBResponseMessage"></param>
        /// <param name="EmployeeProjectMappingDataList"></param>
        /// <returns></returns>
        public string NotifyProjectAllocationToEmployee(AppDbContext db, IWebHostEnvironment webHostEnvironment, int ID, string DBResponseMessage, List<EmployeeProjectMappingDataList> EmployeeProjectMappingDataList)
        {
            string Message = string.Empty;
            try
            {
                if(EmployeeProjectMappingDataList != null && EmployeeProjectMappingDataList.Count > 0)
                {
                    var dtEmailConfigurationSettings = CommonHelper.GetEmailConfigurationSettings(db);
                    if (dtEmailConfigurationSettings.Rows.Count > 0)
                    {
                        string ProjectAllocationEmailTemplateFolderPath = CommonHelper.GetActualPath(webHostEnvironment, "Templates");
                        string ProjectAllocationEmailTemplatePath = ProjectAllocationEmailTemplateFolderPath + "\\ProjectAllocationEmailTemplate.html";
                        StreamReader rdrEmailBody = new StreamReader(ProjectAllocationEmailTemplatePath);
                        string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                        string EmailBodyTemplate = txthtmlEmailBody;
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", EmployeeProjectMappingDataList[0].EmployeeName);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ClientName#", EmployeeProjectMappingDataList[0].ClientName);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ProjectCode#", EmployeeProjectMappingDataList[0].ProjectCode);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ProjectName#", EmployeeProjectMappingDataList[0].ProjectName);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#StartDate#", EmployeeProjectMappingDataList[0].StartDate);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EndDate#", EmployeeProjectMappingDataList[0].EndDate);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ProjectDescription#", EmployeeProjectMappingDataList[0].ProjectDescription);
                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                        string MailSubject = string.Empty;
                        if(ID == 0)
                        {
                            MailSubject = "Project Allocation Notification - " + EmployeeProjectMappingDataList[0].ProjectName;
                        }
                        else
                        {
                            MailSubject = "Update on Project Allocation - " + EmployeeProjectMappingDataList[0].ProjectName;
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
                        objSendEmailRequestParam.ToEmail = EmployeeProjectMappingDataList[0].EmployeeEmailID;
                        objSendEmailRequestParam.EmailSubject = MailSubject;
                        objSendEmailRequestParam.EmailBody = MailBody;
                        bool IsEmailSent = Helper.SendEmail(objSendEmailRequestParam);
                        if (!IsEmailSent)
                        {
                            Message = DBResponseMessage + ". " + "But Failed To Send The Notification. Please Contact Your Administrator !!!";
                        }
                        else
                        {
                            Message = DBResponseMessage + ". " + "Notification Has Been Sent To Your Registered Email ID !!!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IProjectResp.NotifyProjectAllocationToEmployee()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Get Project Activity List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<ProjectActivityM>> GetProjectActivityList(int ID, string Type)
        {
            List<ProjectActivityM> lstProjectActivity = new List<ProjectActivityM>();
            DataTable dt = new DataTable();
            try
            {
                dt = await GetProjectActivity(ID, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Repositories", "IProjectActivityResp.GetProjectActivityList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProjectActivity = Helper.ConvertDataTableToList<ProjectActivityM>(dt);
            }
            return lstProjectActivity;
        }

        /// <summary>
        /// This Method is Used to Get Project Activity Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ProjectActivityM> GetProjectActivityDetails(int? id)
        {
            ProjectActivityM _objProjectActivityM = new ProjectActivityM();
            List<ProjectActivityM> lstProjectActivity = new List<ProjectActivityM>();
            DataTable dt = new DataTable();
            try
            {
                string Type = "GetProjectActivityDetails";
                dt = await GetProjectActivity((int)id, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Repositories", "IProjectActivityResp.GetProjectActivityDetails()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstProjectActivity = Helper.ConvertDataTableToList<ProjectActivityM>(dt);
                _objProjectActivityM = lstProjectActivity[0];
            }
            return _objProjectActivityM;
        }


        /// <summary>
        /// This Method is Used to Get Project Activity List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<DataTable> GetProjectActivity(int ID, string Type)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ID },
                    { "@Type", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetProjectActivityList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Repositories", "IProjectActivityResp.GetProjectActivity()", ex.Message, false);
                throw;
            }
            return dt;
        }

        /// <summary>
        /// This Method is Used to Save Update Project Activity Details.
        /// </summary>
        /// <param name="_objProjectActivityM"></param>
        /// <returns></returns>
        public async Task<string> SaveUpdateProjectActivityDetails(ProjectActivityM _objProjectActivityM)
        {
            string Message = string.Empty;
            DataSet ds = new DataSet();
            try
            {
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objProjectActivityM, "usp_VUSYS_SaveUpdateProjectActivityDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Repositories", "IProjectActivityResp.SaveUpdateProjectActivityDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }
    }
}
