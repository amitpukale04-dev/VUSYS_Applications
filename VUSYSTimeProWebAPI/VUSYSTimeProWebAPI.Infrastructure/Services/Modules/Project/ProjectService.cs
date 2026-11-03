using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Project;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;
using ApplicationSendEmailRequestParam = VUSYSTimeProWebAPI.Application.Models.SendEmailRequestParam;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Project
{
    public sealed class ProjectService : IProjectService
    {
        private const string ProjectServiceLogClassName = "ProjectService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly IDropdownService _DropdownSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Project Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="DropdownSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ProjectService(SQLDBHelper SQLDBHelper, IDropdownService DropdownSvc, IEmailService EmailSvc, IWebHostEnvironment webHostEnvironment)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        #region [Project Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Project Details.
        /// </summary>
        /// <param name="_objProjectM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateProjectDetails(ProjectM _objProjectM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "PROJECT_DETAILS_RESPONSE";
            try
            {
                _objProjectM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objProjectM, StoredProcedure.PROJECT_MASTER, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureProjectLogNamespace, $"{ProjectServiceLogClassName}.SaveUpdateProjectDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Project Detail Or List.
        /// </summary>
        /// <param name="_objProjectDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetProjectDetailOrList(ProjectDetailsRequestParam _objProjectDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objProjectDetailsRequestParam.Id switch
                {
                    0 => "PROJECT_LIST_RESPONSE",
                    _ => "PROJECT_DETAILS_RESPONSE"
                };
                ParamType = _objProjectDetailsRequestParam.Id switch
                {
                    0 => "GetAllProjectList",
                    _ => "GetProjectDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objProjectDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.PROJECT_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureProjectLogNamespace, $"{ProjectServiceLogClassName}.GetProjectDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Project Master]

        #region [Employee Project Mapping]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Project Mapping Details.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateEmployeeProjectMappingDetails(EmployeeProjectMappingM _objEmployeeProjectMappingM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "EMPLOYEE_PROJECT_MAPPING_DETAILS_RESPONSE";
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _DropdownSvc.SaveAndUpdateEmployeeProjectMappingDetails(_objEmployeeProjectMappingM, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (_objEmployeeProjectMappingM.IsNotifyToEmployee)
                {
                    if (Helper.CheckDataSetIsNullOrEmpty(ds))
                    {
                        string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                        string DBMessage = Helper.ToProperCase(ds.Tables[0].Rows[0]["Message"].ToString());
                        int EmployeeProjectMappingID = Convert.ToInt32(ds.Tables[0].Rows[0]["ID"].ToString());
                        if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                        {
                            EmployeeProjectMappingDetailsRequestParam _objEmployeeProjectMappingDetailsRequestParam = Helper.ToCopyClassProperties<EmployeeProjectMappingM, EmployeeProjectMappingDetailsRequestParam>(_objEmployeeProjectMappingM);
                            var EmployeeProjectMappingData = await _DropdownSvc.GetEmployeeProjectMappingDataAsync(_objEmployeeProjectMappingDetailsRequestParam, cancellationToken);
                            if (Helper.CheckDataSetIsNullOrEmpty(EmployeeProjectMappingData))
                            {
                                string EmployeeProjectMappingDataDBStatus = Helper.ToProperCase(EmployeeProjectMappingData.Tables[0].Rows[0]["Status"].ToString());
                                if (EmployeeProjectMappingDataDBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                                {
                                    var EmployeeProjectMappingList = Helper.ConvertDataSetToClassList<EmployeeProjectMappingDataList>(EmployeeProjectMappingData);
                                    string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "ProjectAllocationEmailTemplate.html");
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", EmployeeProjectMappingList[0].EmployeeName);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#ClientName#", EmployeeProjectMappingList[0].ClientName);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#ProjectCode#", EmployeeProjectMappingList[0].ProjectCode);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#ProjectName#", EmployeeProjectMappingList[0].ProjectName);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#StartDate#", EmployeeProjectMappingList[0].StartDate);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#EndDate#", EmployeeProjectMappingList[0].EndDate);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#ProjectDescription#", EmployeeProjectMappingList[0].ProjectDescription);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());

                                    string MailSubject = string.Empty;
                                    if (_objEmployeeProjectMappingM.Id == 0)
                                    {
                                        MailSubject = "Project Allocation Notification - " + EmployeeProjectMappingList[0].ProjectName;
                                    }
                                    else
                                    {
                                        MailSubject = "Update on Project Allocation - " + EmployeeProjectMappingList[0].ProjectName;
                                    }
                                    var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, EmployeeProjectMappingList[0].EmployeeEmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                                    var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                                    string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                                    JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                                    string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                                    string EmailSentResponseReturnResult = string.Empty;
                                    string EmailSentFailedResponseReturnResult = DBMessage + " " + "But Failed To Send The Notification. Please Contact Your Administrator !!!";

                                    if (resultjObjectEmail == "ok")
                                    {
                                        var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();

                                        if (emailResponse?.Message == Helper.EmailSuccess)
                                        {
                                            EmailSentResponseReturnResult = $"{DBMessage} Notification Has Been Sent To Your Registered Email ID !!!";

                                            List<List<string>> rows = new List<List<string>>
                                            {
                                                new List<string> { EmployeeProjectMappingID.ToString(), DBStatus, EmailSentResponseReturnResult }
                                            };
                                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                            return finalDataSet;
                                        }
                                    }
                                    else
                                    {
                                        List<List<string>> ErrorRows = new List<List<string>>
                                        {
                                            new List<string> { EmployeeProjectMappingID.ToString(), DBStatus, EmailSentFailedResponseReturnResult }
                                        };
                                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    finalDataSet = ds;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureProjectLogNamespace, "EmployeeService.SaveUpdateEmployeeProjectMappingDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Project Mapping Detail Or List.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmployeeProjectMappingDetailOrList(EmployeeProjectMappingDetailsRequestParam _objEmployeeProjectMappingDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _DropdownSvc.GetEmployeeProjectMappingDataAsync(_objEmployeeProjectMappingDetailsRequestParam, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureProjectLogNamespace, "EmployeeService.GetEmployeeProjectMappingDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Employee Project Mapping]

        #region [Project Activity]

        /// <summary>
        /// This Method Is Used To Save Or Update Project Activity Details.
        /// </summary>
        /// <param name="_objProjectActivityM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateProjectActivityDetails(ProjectActivityM _objProjectActivityM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "PROJECT_ACTIVITY_DETAILS_RESPONSE";
            try
            {
                _objProjectActivityM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objProjectActivityM, StoredProcedure.PROJECT_ACTIVITY, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureProjectLogNamespace, "MasterService.SaveUpdateProjectActivityDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Project Activity Details Or List.
        /// </summary>
        /// <param name="_objProjectActivityDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetProjectActivityDetailOrList(ProjectActivityDetailsRequestParam _objProjectActivityDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objProjectActivityDetailsRequestParam.Id switch
                {
                    0 => "PROJECT_ACTIVITY_LIST_RESPONSE",
                    _ => "PROJECT_ACTIVITY_DETAILS_RESPONSE"
                };
                ParamType = _objProjectActivityDetailsRequestParam.Id switch
                {
                    0 => "GetAllProjectActivityList",
                    _ => "GetProjectActivityDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objProjectActivityDetailsRequestParam.Id },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.PROJECT_ACTIVITY_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureProjectLogNamespace, $"{ProjectServiceLogClassName}.GetProjectActivityDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Project Activity]
    }
}
