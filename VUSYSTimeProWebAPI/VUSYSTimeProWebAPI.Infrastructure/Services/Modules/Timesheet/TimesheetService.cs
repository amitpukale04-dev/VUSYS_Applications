using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Timesheet;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;
using static CommonLibCore10.CONST;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Timesheet
{
    public sealed class TimesheetService : ITimesheetService
    {
        private const string TimesheetServiceLogClassName = "TimesheetService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly IDropdownService _DropdownSvc;
        private readonly IContentTypeService _ContentTypeSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Timesheet Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="DropdownSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="ContentTypeSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public TimesheetService(SQLDBHelper SQLDBHelper, IDropdownService DropdownSvc, IEmailService EmailSvc, IContentTypeService ContentTypeSvc,
            IWebHostEnvironment webHostEnvironment)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _ContentTypeSvc = ContentTypeSvc ?? throw new ArgumentNullException(nameof(ContentTypeSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        #region [Timesheet]

        /// <summary>
        /// This Method Is Used To Get Timesheet Submission List.
        /// </summary>
        /// <param name="_objTimesheetSubmissionRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetTimesheetSubmissionList(TimesheetSubmissionRequestParam _objTimesheetSubmissionRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string SourceScreen = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objTimesheetSubmissionRequestParam.ParamType switch
                {
                    "GetEmployeeProjectTimesheetForReport" => "TIMESHEET_REPORT_RESPONSE",
                    "GetEmployeeProjectTimesheetForSubmission" => "TIMESHEET_SUBMISSION_RESPONSE",
                    "GetAllEmployeeProjectTimesheetForApproval" => "TIMESHEET_APPROVAL_REJECTION_LIST_RESPONSE",
                    _ => "TIMESHEET_REPORT_RESPONSE"
                };
                SourceScreen = _objTimesheetSubmissionRequestParam.ParamType switch
                {
                    "GetEmployeeProjectTimesheetForReport" => "TimesheetReport",
                    "GetEmployeeProjectTimesheetForSubmission" => "TimesheetSubmission",
                    "GetAllEmployeeProjectTimesheetForApproval" => "TimesheetApprovalRejection",
                    _ => "TimesheetSubmission"
                };
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", 0 },
                    { "@EmployeeCode", _objTimesheetSubmissionRequestParam.EmployeeCode },
                    { "@TimeSheetMonth", _objTimesheetSubmissionRequestParam.TimesheetMonth },
                    { "@TimeSheetYear", _objTimesheetSubmissionRequestParam.TimesheetYear },
                    { "@ApproverEmailID", string.Empty },
                    { "@UserAccessToken", string.Empty },
                    { "@ParamType", _objTimesheetSubmissionRequestParam.ParamType }
                };
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_PROJECT_TIMESHEET_SUBMISSION, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        var ProjectTimesheetSubmissionApproveRejectReportList = Helper.ConvertDataSetToClassList<ProjectTimesheetSubmissionApproveRejectReportList>(ds);
                        if(ProjectTimesheetSubmissionApproveRejectReportList != null && ProjectTimesheetSubmissionApproveRejectReportList.Count > 0)
                        {
                            foreach (ProjectTimesheetSubmissionApproveRejectReportList item in ProjectTimesheetSubmissionApproveRejectReportList)
                            {
                                if(SourceScreen == "TimesheetReport")
                                {
                                    item.IsShowSendNotificationButton = (item.TimesheetApprovalStatus != "Approved" && item.IsSentForApproval == false ? true : false);
                                    item.IsShowViewDetailsButton = (item.TimesheetApprovalStatus != "Not Submitted" && item.TimesheetApprovalStatus != "Pending For Approval" ? true : false);
                                }
                                else
                                {
                                    item.IsShowSendForApprovalButton = (item.TimesheetApprovalStatus != "Approved" || item.IsAvailableToResendForApproval == true ? true : false);
                                    item.IsShowViewDetailsButton = (item.TimesheetApprovalStatus != "Not Submitted" && item.TimesheetApprovalStatus != "Pending For Approval" ? true : false);
                                }
                            }
                        }
                        var dsProjectTimesheetSubmissionApproveRejectReport = Helper.ConvertListToDataSet<ProjectTimesheetSubmissionApproveRejectReportList>(ProjectTimesheetSubmissionApproveRejectReportList, TableName);
                        finalDataSet = dsProjectTimesheetSubmissionApproveRejectReport;
                        return finalDataSet;
                    }
                    else
                    {
                        finalDataSet = ds;
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.GetTimesheetSubmissionList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Log Timesheet List.
        /// </summary>
        /// <param name="_objLogTimesheetListRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetLogTimesheetList(LogTimesheetListRequestParam _objLogTimesheetListRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "LOG_TIMESHEET_LIST_RESPONSE";
            string TableName1 = "ALLOW_ADD_TIMESHEET_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", _objLogTimesheetListRequestParam.ProjectTimeSheetID },
                    { "@EmployeeCode", _objLogTimesheetListRequestParam.EmployeeCode },
                    { "@FromTaskDate", _objLogTimesheetListRequestParam.FromDate },
                    { "@ToTaskDate", _objLogTimesheetListRequestParam.ToDate },
                    { "@ParamType", "ByTaskDate" }
                };
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.GET_EMPLOYEE_PROJECT_LOG_TIMESHEET, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        if (ds.Tables.Contains(DataSetDataTableConstants.Subroot))
                        {
                            ds.Tables[DataSetDataTableConstants.Subroot].TableName = TableName;
                        }
                        if (ds.Tables.Contains(DataSetDataTableConstants.Subroot1))
                        {
                            ds.Tables[DataSetDataTableConstants.Subroot1].TableName = TableName1;
                        }
                        finalDataSet = ds;
                        return finalDataSet;
                    }
                    else
                    {
                        finalDataSet = ds;
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.GetLogTimesheetList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Timesheet Details.
        /// </summary>
        /// <param name="_objGetTimesheetDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetTimesheetDetails(GetTimesheetDetailsRequestParam _objGetTimesheetDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "Status", "Message" };
            string TableName = "TIMESHEET_DETAILS_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var IsApproved = await _DropdownSvc.CheckTimesheetApprovedAsync(_objGetTimesheetDetailsRequestParam.EmployeeCode, _objGetTimesheetDetailsRequestParam.TaskDate, cancellationToken);
                if(!IsApproved)
                {
                    var IsSentForApproval = await _DropdownSvc.CheckTimesheetSentForApprovalAsync(_objGetTimesheetDetailsRequestParam.EmployeeCode, _objGetTimesheetDetailsRequestParam.TaskDate, cancellationToken);
                    if(!IsSentForApproval)
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            { "@ProjectTimeSheetID", _objGetTimesheetDetailsRequestParam.ProjectTimeSheetID },
                            { "@EmployeeCode", _objGetTimesheetDetailsRequestParam.EmployeeCode },
                            { "@FromTaskDate", _objGetTimesheetDetailsRequestParam.TaskDate },
                            { "@ToTaskDate", string.Empty },
                            { "@ParamType", "GetProjectTimeSheetDetails" }
                        };
                        finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.GET_EMPLOYEE_PROJECT_LOG_TIMESHEET, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                        cancellationToken.ThrowIfCancellationRequested();
                        finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), $"You Cannot Edit The Timesheet. Since Timesheet is Already Sent for Approval for the Month of {_objGetTimesheetDetailsRequestParam.TimesheetMonth} - {_objGetTimesheetDetailsRequestParam.TimesheetYear} !!!" }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), $"You Cannot Edit The Timesheet. Since Timesheet is Already Approved for the Month of {_objGetTimesheetDetailsRequestParam.TimesheetMonth} - {_objGetTimesheetDetailsRequestParam.TimesheetYear} !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.GetTimesheetDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Save Update Timesheet Details.
        /// </summary>
        /// <param name="lstProjectTimesheet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateTimesheetDetails(List<ProjectTimesheet> lstProjectTimesheet, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "Status", "Message" };
            string TableName = "TIMESHEET_SAVE_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool isAnyTaskApproved = false;
                bool isSentForApproval = false;
                DateTime? tDate = null;
                string TimesheetMonth = string.Empty;
                int TimesheetYear = 0;

                foreach (var item in lstProjectTimesheet)
                {
                    tDate = item.TaskDate;
                    TimesheetMonth = DateTimeHelper.GetMonthName(item.TaskDate.Month);
                    TimesheetYear = item.TaskDate.Year;
                    if (await _DropdownSvc.CheckTimesheetApprovedAsync(lstProjectTimesheet[0].EmployeeCode, tDate, cancellationToken))
                    {
                        isAnyTaskApproved = true;
                        break; // No need to continue checking if one is approved
                    }
                }
                if (isAnyTaskApproved)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), $"You Cannot Add The Timesheet. Since Timesheet is Already Approved for the Month of {TimesheetMonth} - {TimesheetYear} !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }

                foreach (var item in lstProjectTimesheet)
                {
                    tDate = item.TaskDate;
                    TimesheetMonth = DateTimeHelper.GetMonthName(item.TaskDate.Month);
                    TimesheetYear = item.TaskDate.Year;
                    if (await _DropdownSvc.CheckTimesheetSentForApprovalAsync(lstProjectTimesheet[0].EmployeeCode, tDate, cancellationToken))
                    {
                        isSentForApproval = true;
                        break; // No need to continue checking if it is sent for Approval
                    }
                }
                if (isSentForApproval)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), $"You Cannot Add The Timesheet. Since Timesheet is Already Sent for Approval for the Month of {TimesheetMonth} - {TimesheetYear} !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }
                var IsTimesheetClosed = await _DropdownSvc.CheckCurrentMonthTimesheetClosedStatusAsync(lstProjectTimesheet[0].EmployeeCode, cancellationToken);
                if (!IsTimesheetClosed)
                {
                    var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLListParametersAsync(lstProjectTimesheet, lstProjectTimesheet.GetType(), lstProjectTimesheet, StoredProcedure.SAVE_TIMESHEET, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                    cancellationToken.ThrowIfCancellationRequested();
                    finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "You Cannot Add The Timesheet. Since Timesheet is Closed for This Month !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.SaveUpdateTimesheetDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Send Timesheet Submission Reminder Notification To Employee.
        /// </summary>
        /// <param name="_objTimesheetReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SendTimesheetSubmissionReminderNotificationToEmployee(TimesheetReportRequestParam _objTimesheetReportRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "TIMESHEET_SUBMISSION_REMINDER_NOTIFICATION_RESPONSE";
            List<string> columns = new List<string> { "Status", "Message" };
            string EmailSentResponseReturnResult = "An Email Notification Has Been Sent to the Employee !!!";
            string EmailSentFailedResponseReturnResult = "We Encountered An Issue Sending An Email Notification To The Employee !!!";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var UserDetails = await _DropdownSvc.GetUserDetailsByEmployeeCode(_objTimesheetReportRequestParam.EmployeeCode, cancellationToken);
                if (UserDetails != null && UserDetails.Count > 0)
                {
                    string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "TimesheetSubmissionReminderNotificationEmailTemplate.html");
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonth#", _objTimesheetReportRequestParam.TimesheetMonth);
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetYear#", _objTimesheetReportRequestParam.TimesheetYear.ToString());
                    EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                    string MailSubject = "Reminder: Timesheet Submission Required for " + _objTimesheetReportRequestParam.TimesheetMonth + " " + _objTimesheetReportRequestParam.TimesheetYear;
                    var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, UserDetails[0].EmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                    var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                    string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                    JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                    string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                    if (resultjObjectEmail == "ok")
                    {
                        var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                        if (emailResponse?.Message == Helper.EmailSuccess)
                        {

                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                            return finalDataSet;
                        }
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.SendTimesheetSubmissionReminderNotificationToEmployee()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get or Export Employee Timesheet List.
        /// </summary>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetOrExportEmployeeTimesheetList(ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "Status", "FileName", "ContentType", "FileBase64" };
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objProjectTimesheetApproveRejection.Action switch
                {
                    "View" => "EMPLOYEE_TIMESHEET_RESPONSE",
                    "Export" => "EMPLOYEE_TIMESHEET_EXPORTED_RESPONSE",
                    "ExportDateWise" => "EMPLOYEE_TIMESHEET_DATE_WISE_EXPORTED_RESPONSE",
                    _ => "EMPLOYEE_TIMESHEET_RESPONSE"
                };
                ParamType = _objProjectTimesheetApproveRejection.Action switch
                {
                    "View" or "Export" => "DownloadEmployeeTimesheet",
                    "ExportDateWise" => "DownloadEmployeeTimesheetDateWise",
                    _ => "DownloadEmployeeTimesheet"
                };
                cancellationToken.ThrowIfCancellationRequested();
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
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_TIMESHEET_APPROVE_REJECT, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        if (_objProjectTimesheetApproveRejection.Action == "View")
                        {
                            var TimeSheetData = Helper.ConvertDataSetToDataTable(ds);
                            if (Helper.CheckDataTableIsNullOrEmpty(TimeSheetData))
                            {
                                foreach (DataRow row in TimeSheetData.Rows)
                                {
                                    string ClientName = row.Field<string>("ClientName");
                                    string ProjectName = row.Field<string>("ProjectName");
                                    string ActivityName = row.Field<string>("ActivityName");
                                    string TaskDescription = row.Field<string>("TaskDescription");
                                    string formattedTaskDescription = CommonHelper.ConvertNumberedTextToHtmlList(TaskDescription, skipItems: 0);
                                    row["ClientName"] = CommonHelper.AddAmpersandToLast(ClientName);
                                    row["ProjectName"] = CommonHelper.AddAmpersandToLast(ProjectName);
                                    row["ActivityName"] = CommonHelper.AddAmpersandToLast(ActivityName);
                                    row["TaskDescription"] = formattedTaskDescription;
                                }
                            }
                            finalDataSet = Helper.ConvertDataTableToDataSet(TimeSheetData.Copy());
                            finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
                        }
                        else
                        {
                            const int NoOfColumns = 9;
                            string Title = string.Empty;
                            string SheetName = string.Empty;
                            string FileName = string.Empty;
                            string TitleMonthYear = _objProjectTimesheetApproveRejection.TimesheetMonth + " " + Convert.ToString(_objProjectTimesheetApproveRejection.TimesheetYear);
                            string SheetMonthYear = _objProjectTimesheetApproveRejection.TimesheetMonth + "_" + Convert.ToString(_objProjectTimesheetApproveRejection.TimesheetYear);
                            SheetName = _objProjectTimesheetApproveRejection.EmployeeCode + "_TimeSheet_" + SheetMonthYear;
                            FileName = (_objProjectTimesheetApproveRejection.Action == "View" || _objProjectTimesheetApproveRejection.Action == "Export" ? SheetName + FileHelper.GetFileExtension(FileTypes.Xlsx) : SheetName + "_DateWise" + FileHelper.GetFileExtension(FileTypes.Xlsx));
                            Title = "Timesheet - " + TitleMonthYear;

                            string ContentType = _ContentTypeSvc.GetFileContentType(FileName);

                            var TimeSheetDataTable = Helper.ConvertDataSetToDataTable(ds);
                            if(Helper.CheckDataTableIsNullOrEmpty(TimeSheetDataTable))
                            {
                                foreach (DataRow row in TimeSheetDataTable.Rows)
                                {
                                    string ClientName = row.Field<string>("ClientName");
                                    string ProjectName = row.Field<string>("ProjectName");
                                    string ActivityName = row.Field<string>("ActivityName");
                                    row["ClientName"] = CommonHelper.AddAmpersandToLast(ClientName);
                                    row["ProjectName"] = CommonHelper.AddAmpersandToLast(ProjectName);
                                    row["ActivityName"] = CommonHelper.AddAmpersandToLast(ActivityName);
                                }
                            }
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "SLNo", "SL No");
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "EmployeeCode", "Employee Code");
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "DisplayTaskDate", "Task Date");
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "ClientName", "Client");
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "ProjectName", "Project");
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "ActivityName", "Activity");
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "TaskDescription", "Task Description");
                            Helper.RenameDataTableColumnName(TimeSheetDataTable, "TotalHoursWorked", "No. of Hours Worked");
                            Helper.RemoveColumnFromDataTable(TimeSheetDataTable, "Status");

                            var ColumnsConvertToNumber = new string[] { "No. of Hours Worked" };
                            var WordWrapColumnIndexes = new int[] { 1, 2, 3, 4, 5, 6, 7, 9 };
                            string base64Excel = ExcelHelper.ExportToExcelBase64(TimeSheetDataTable, SheetName, Title, "A1:I1", 1, NoOfColumns, true, 2, true, 5, "#FFFFFF", FileName, true, ColumnsConvertToNumber, "#4F81BD", "#DCE6F1", true, true, "H", 8, WordWrapColumnIndexes);
                            if (!string.IsNullOrEmpty(base64Excel))
                            {
                                var result = new FileHelper.Base64File(base64Excel, SheetName, FileHelper.GetFileExtension(FileTypes.Xlsx));
                                //string Folder = CommonHelper.SaveFileToFolder(result.Base64, FileTypes.Xlsx, _webHostEnvironment);
                                List<List<string>> rows = new List<List<string>>
                                {
                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), FileName, ContentType, result.Base64 }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                            }
                            else
                            {
                                List<List<string>> rows = new List<List<string>>
                                {
                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), string.Empty, string.Empty, string.Empty }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                            }
                        }
                    }
                    else
                    {
                        if (_objProjectTimesheetApproveRejection.Action == "View")
                        {
                            finalDataSet = ds;
                            finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
                        }
                        else
                        {
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), string.Empty, string.Empty, string.Empty }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        }
                    }
                }
                else
                {
                    if (_objProjectTimesheetApproveRejection.Action == "View")
                    {
                        ds.Tables[DataSetDataTableConstants.Subroot].Clear();
                        finalDataSet = ds;
                        finalDataSet = Helper.RenameDataSetTableName(finalDataSet, DataSetDataTableConstants.Subroot, TableName);
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), string.Empty, string.Empty, string.Empty }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.GetOrExportEmployeeTimesheetList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Check Timesheet Approval Status.
        /// </summary>
        /// <param name="_objCheckTimesheetApprovalStatusRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> CheckTimesheetApprovalStatus(CheckTimesheetApprovalStatusRequestParam _objCheckTimesheetApprovalStatusRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "TIMESHEET_APPROVAL_STATUS_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", 0 },
                    { "@EmployeeCode", _objCheckTimesheetApprovalStatusRequestParam.EmployeeCode },
                    { "@TimeSheetMonth", _objCheckTimesheetApprovalStatusRequestParam.TimesheetMonth },
                    { "@TimeSheetYear", _objCheckTimesheetApprovalStatusRequestParam.TimesheetYear },
                    { "@IsApproved", 0 },
                    { "@Remarks", string.Empty },
                    { "@ApprovedOrRejectedBy", string.Empty },
                    { "@UserAccessToken", string.Empty },
                    { "@Source", string.Empty },
                    { "@ParamType", "CheckApprovalStatusFromFE" }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_TIMESHEET_APPROVE_REJECT, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.CheckTimesheetApprovalStatus()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Submit Timesheet Details For Approval.
        /// </summary>
        /// <param name="_objTimesheetSubmitForApprovalRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SubmitTimesheetDetailsForApproval(TimesheetSubmitForApprovalRequestParam _objTimesheetSubmitForApprovalRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "TIMESHEET_SUBMITTED_RESPONSE";
            List<string> columns = new List<string> { "Status", "Message" };
            string EmailSentResponseReturnResult = "An Email Notification Has Been Sent to the Employee !!!";
            string EmailSentFailedResponseReturnResult = "We Encountered An Issue Sending An Email Notification To The Employee !!!";
            string ApproverNotAvailable = "You Cannot Submit The Timesheet. Since Approver Is Not Available In the System. Please Contact Administrator !!!";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string ParamType = string.Empty;
                if (_objTimesheetSubmitForApprovalRequestParam.ApproveStatus == "Resend For Approval")
                {
                    ParamType = "ReSubmitTimesheetForApproval";
                }
                else
                {
                    ParamType = "SubmitTimesheetForApproval";
                }
                var ApproverUserDetails = await _DropdownSvc.GetApproverDetails(cancellationToken);
                var UserDetails = await _DropdownSvc.GetUserDetailsByEmployeeCode(_objTimesheetSubmitForApprovalRequestParam.EmployeeCode, cancellationToken);
                if (ApproverUserDetails != null && ApproverUserDetails.Count > 0)
                {
                    Guid newGuid = Guid.NewGuid();
                    var parameters = new Dictionary<string, object>
                    {
                        { "@ProjectTimeSheetID", _objTimesheetSubmitForApprovalRequestParam.ProjectTimeSheetID },
                        { "@EmployeeCode", _objTimesheetSubmitForApprovalRequestParam.EmployeeCode },
                        { "@TimeSheetMonth", _objTimesheetSubmitForApprovalRequestParam.TimesheetMonth },
                        { "@TimeSheetYear", _objTimesheetSubmitForApprovalRequestParam.TimesheetYear },
                        { "@ApproverEmailID", ApproverUserDetails[0].EmailID },
                        { "@UserAccessToken", newGuid.ToString() },
                        { "@ParamType", ParamType }
                    };
                    var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_PROJECT_TIMESHEET_SUBMISSION, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (Helper.CheckDataSetIsNullOrEmpty(ds))
                    {
                        string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                        string DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                        if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                        {
                            string LinkURL = string.Empty;
                            LinkURL = CommonHelper.KeyOption.FrontEndBaseURL + "/timesheet-approval-rejection?PTSID=" + Helper.EncodeStringToBase64(_objTimesheetSubmitForApprovalRequestParam.ProjectTimeSheetID.ToString()) + "&S=" + Helper.EncodeStringToBase64(UserDetails[0].EmailID) + "&ID=" + Helper.EncodeStringToBase64(newGuid.ToString()) + "&M=" + Helper.EncodeStringToBase64(_objTimesheetSubmitForApprovalRequestParam.TimesheetMonth.ToString()) + "&Y=" + Helper.EncodeStringToBase64(_objTimesheetSubmitForApprovalRequestParam.TimesheetYear.ToString());
                            string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "TimesheetPendingEmailTemplate.html");
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverName#", ApproverUserDetails[0].FullName);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonth#", _objTimesheetSubmitForApprovalRequestParam.TimesheetMonth);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetYear#", _objTimesheetSubmitForApprovalRequestParam.TimesheetYear.ToString());
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", LinkURL);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeEmailID#", UserDetails[0].EmailID);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                            string MailSubject = "Timesheet Approval Request for " + UserDetails[0].FullName + "–" + _objTimesheetSubmitForApprovalRequestParam.TimesheetMonth + " " + _objTimesheetSubmitForApprovalRequestParam.TimesheetYear;
                            var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, ApproverUserDetails[0].EmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                            var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                            string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                            JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                            string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                            if (resultjObjectEmail == "ok")
                            {
                                var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                                if (emailResponse?.Message == Helper.EmailSuccess)
                                {
                                    EmailSentResponseReturnResult = $"{DBMessage}, and an Email Notification Has Been Sent to the Approver !!!";

                                    List<List<string>> rows = new List<List<string>>
                                    {
                                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                    return finalDataSet;
                                }
                                else
                                {
                                    EmailSentResponseReturnResult = $"{DBMessage}, However, We Encountered An Issue Sending The Approval Request Email To The Approver. Please Contact Your Administrator For Assistance !!!";

                                    List<List<string>> rows = new List<List<string>>
                                    {
                                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                    return finalDataSet;
                                }
                            }
                            else
                            {
                                List<List<string>> ErrorRows = new List<List<string>>
                                {
                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                            }
                        }
                        else
                        {
                            List<List<string>> ErrorRows = new List<List<string>>
                            {
                                new List<string> { DBStatus, DBMessage }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ApproverNotAvailable }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.SendTimesheetSubmissionReminderNotificationToEmployee()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Approve Or Reject Timesheet.
        /// </summary>
        /// <param name="_objTimesheetApproveRejectRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> ApproveOrRejectTimesheet(TimesheetApproveRejectRequestParam _objTimesheetApproveRejectRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "TIMESHEET_APPROVE_REJECT_RESPONSE";
            List<string> columns = new List<string> { "Status", "Message" };
            string EmailSentResponseReturnResult = "An Email Notification Has Been Sent to the User !!!";
            string EmailSentFailedResponseReturnResult = "We Encountered An Issue Sending An Email Notification To The User. Please Contact Your Administrator For Assistance !!!";
            string ApproverNotAvailable = "You Cannot Approve/Reject The Timesheet. Since Approver Is Not Available In the System. Please Contact Administrator.";
            string UserNotAvailable = "You Cannot Approve/Reject The Timesheet. Since User Is Not Available In the System. Please Contact Administrator.";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ApproverUserDetails = await _DropdownSvc.GetApproverDetails(cancellationToken);
                if (ApproverUserDetails != null && ApproverUserDetails.Count > 0)
                {
                    var UserDetails = await _DropdownSvc.GetUserDetailsByEmployeeCode(_objTimesheetApproveRejectRequestParam.EmployeeCode, cancellationToken);
                    if(UserDetails != null && UserDetails.Count > 0)
                    {
                        bool IsApproved = (_objTimesheetApproveRejectRequestParam.ApproveStatus == "Approve") ? true : false;
                        var parameters = new Dictionary<string, object>
                        {
                            { "@ProjectTimeSheetID", _objTimesheetApproveRejectRequestParam.ProjectTimeSheetID },
                            { "@EmployeeCode", _objTimesheetApproveRejectRequestParam.EmployeeCode },
                            { "@TimeSheetMonth", _objTimesheetApproveRejectRequestParam.TimesheetMonth },
                            { "@TimeSheetYear", _objTimesheetApproveRejectRequestParam.TimesheetYear },
                            { "@IsApproved", IsApproved },
                            { "@Remarks", _objTimesheetApproveRejectRequestParam.Remarks },
                            { "@ApprovedOrRejectedBy", ApproverUserDetails[0].Id },
                            { "@UserAccessToken",  _objTimesheetApproveRejectRequestParam.UserToken },
                            { "@Source", _objTimesheetApproveRejectRequestParam.Source },
                            { "@ParamType", "ApproveRejectTimesheet" }
                        };
                        var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_TIMESHEET_APPROVE_REJECT, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                        cancellationToken.ThrowIfCancellationRequested();
                        if (Helper.CheckDataSetIsNullOrEmpty(ds))
                        {
                            string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                            string DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                            if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                            {
                                string EmailBodyTemplate = string.Empty;
                                string MailSubject = string.Empty;
                                if (IsApproved)
                                {
                                    EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "TimesheetApprovedEmailTemplate.html");
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#Header#", "Approval Notification: " + _objTimesheetApproveRejectRequestParam.TimesheetMonth + " " + _objTimesheetApproveRejectRequestParam.TimesheetYear);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonthYear#", _objTimesheetApproveRejectRequestParam.TimesheetMonth + "-" + _objTimesheetApproveRejectRequestParam.TimesheetYear);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#Remarks#", _objTimesheetApproveRejectRequestParam.Remarks);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                                    MailSubject = "Timesheet Approved – " + _objTimesheetApproveRejectRequestParam.TimesheetMonth + " " + _objTimesheetApproveRejectRequestParam.TimesheetYear;
                                }
                                else
                                {
                                    EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "TimesheetRejectedEmailTemplate.html");
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#Header#", "Rejection Alert: Timesheet for " + _objTimesheetApproveRejectRequestParam.TimesheetMonth + " " + _objTimesheetApproveRejectRequestParam.TimesheetYear);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#TimesheetMonthYear#", _objTimesheetApproveRejectRequestParam.TimesheetMonth + "-" + _objTimesheetApproveRejectRequestParam.TimesheetYear);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#Remarks#", _objTimesheetApproveRejectRequestParam.Remarks);
                                    EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                                    MailSubject = "Rejection Alert: Timesheet for " + _objTimesheetApproveRejectRequestParam.TimesheetMonth + " " + _objTimesheetApproveRejectRequestParam.TimesheetYear;
                                }
                                var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, UserDetails[0].EmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                                var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                                string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                                JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                                string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                                if (resultjObjectEmail == "ok")
                                {
                                    var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                                    if (emailResponse?.Message == Helper.EmailSuccess)
                                    {
                                        EmailSentResponseReturnResult = $"{DBMessage}, and an Email Notification Has Been Sent to the User !!!";

                                        List<List<string>> rows = new List<List<string>>
                                        {
                                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                        };
                                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                        return finalDataSet;
                                    }
                                    else
                                    {
                                        EmailSentResponseReturnResult = $"{DBMessage}, However, We Encountered An Issue Sending The An Email To The User. Please Contact Your Administrator For Assistance !!!";
                                        List<List<string>> rows = new List<List<string>>
                                        {
                                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                        };
                                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                        return finalDataSet;
                                    }
                                }
                                else
                                {
                                    List<List<string>> ErrorRows = new List<List<string>>
                                    {
                                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                }
                            }
                            else
                            {
                                List<List<string>> ErrorRows = new List<List<string>>
                                {
                                    new List<string> { DBStatus, DBMessage }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                            }
                        }
                        else
                        {
                            List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                        };
                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), UserNotAvailable }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ApproverNotAvailable }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.ApproveOrRejectTimesheet()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Timesheet Not Filled List and Send Email Notification.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmployeesTimesheetNotFilledListAndSendEmailNotification(CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "Status", "Message" };
            string TableName = "TIMESHEET_NOT_FILLED_RESPONSE";
            string ErrorMessage = "Failed To Send Email Notification !!!";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithNoParametersAsync(StoredProcedure.GET_TIMESHEET_NOT_FILLED, CONST.DBRetType.DATASET) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    int successCount = 0;
                    int failCount = 0;

                    var dt = Helper.ConvertDataSetToDataTable(ds);
                    if (Helper.CheckDataTableIsNullOrEmpty(dt))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            string EmployeeName = row["FullName"].ToString();
                            string EmployeeEmailID = row["EmailID"].ToString();
                            string NotFilledDates = row["NotFilledDates"].ToString();

                            string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "TimesheetNotFilledNotificationEmailTemplate.html");

                            EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", EmployeeName);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#NotFilledDates#", NotFilledDates);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", CommonHelper.KeyOption.FrontEndBaseURL);
                            EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                            string MailSubject = "Reminder: Timesheet Not Filled for " + NotFilledDates;

                            var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, EmployeeEmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                            var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                            string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                            JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                            string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                            if (resultjObjectEmail == "ok")
                            {
                                var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                                if (emailResponse?.Message == Helper.EmailSuccess)
                                {
                                    successCount++;
                                }
                                else
                                {
                                    failCount++;
                                }
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                        List<List<string>> SuccessRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), $"Email Notification Summary: {successCount} Sent, {failCount} Failed !!!" }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, SuccessRows, TableName);
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ErrorMessage }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ErrorMessage }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.GetEmployeesTimesheetNotFilledListAndSendEmailNotification()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Employees Leave, Holiday and Sync To Project Timesheet.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet(CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "Status", "Message" };
            string ErrorMessage = "Failed To Sync Employee Leave Or Holiday Timesheet !!!";
            string TableName = "EMPLOYEE_LEAVE_HOLIDAY_SYNC_PROJECT_TIMESHEET_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithNoParametersAsync(StoredProcedure.SYNC_EMPLOYEE_LEAVE_HOLIDAY_TO_PROJECT_TIMESHEET, DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    string DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                    string InsertedCount = Convert.ToString(ds.Tables[0].Rows[0]["InsertedCount"].ToString());
                    string DeletedCount = Convert.ToString(ds.Tables[0].Rows[0]["DeletedCount"].ToString());
                    string SkippedMissingTimeSheetCount = Convert.ToString(ds.Tables[0].Rows[0]["SkippedMissingTimeSheetCount"].ToString());
                    DBMessage = DBMessage.Replace(".", ":");
                    string CustomMessage = $"{DBMessage} {InsertedCount} Inserted, {DeletedCount} Deleted, {SkippedMissingTimeSheetCount} SkippedMissingTimeSheet !!!";

                    List<List<string>> SuccessRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), CustomMessage }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, SuccessRows, TableName);
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ErrorMessage }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureTimesheetLogNamespace, $"{TimesheetServiceLogClassName}.GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Timesheet]

    }
}
