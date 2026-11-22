using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Reports;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Timesheet;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;
using static CommonLibCore10.CONST;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Reports
{
    public sealed class ReportService : IReportService
    {
        private const string ReportServiceLogClassName = "ReportService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly ITimesheetService _TimesheetSvc;
        private readonly IContentTypeService _ContentTypeSvc;
        private readonly IDropdownService _DropdownSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Report Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="TimesheetSvc"></param>
        /// <param name="ContentTypeSvc"></param>
        /// <param name="DropdownSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ReportService(SQLDBHelper SQLDBHelper, ITimesheetService TimesheetSvc, IContentTypeService ContentTypeSvc, IDropdownService DropdownSvc, 
            IEmailService EmailSvc, IWebHostEnvironment webHostEnvironment)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _TimesheetSvc = TimesheetSvc ?? throw new ArgumentNullException(nameof(TimesheetSvc));
            _ContentTypeSvc = ContentTypeSvc ?? throw new ArgumentNullException(nameof(ContentTypeSvc));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        #region [Timesheet Report]

        /// <summary>
        /// This Method Is Used To Get Timesheet Report List.
        /// </summary>
        /// <param name="_objTimesheetReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetTimesheetReportList(TimesheetReportRequestParam _objTimesheetReportRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                TimesheetSubmissionRequestParam _objTimesheetSubmissionRequestParam = Helper.ToCopyClassProperties<TimesheetReportRequestParam, TimesheetSubmissionRequestParam>(_objTimesheetReportRequestParam);
                _objTimesheetSubmissionRequestParam.ParamType = "GetEmployeeProjectTimesheetForReport";
                finalDataSet = await _TimesheetSvc.GetTimesheetSubmissionList(_objTimesheetSubmissionRequestParam, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureReportsServiceLogNamespace, $"{ReportServiceLogClassName}.GetTimesheetReportList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Timesheet Report]

        #region [Project Allocated Consumed Report]

        /// <summary>
        /// This Method Is Used To Get Project Allocated Consumed Report List.
        /// </summary>
        /// <param name="_objProjectAllocatedConsumedReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetProjectAllocatedConsumedReportList(ProjectAllocatedConsumedReportRequestParam _objProjectAllocatedConsumedReportRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "Status", "FileName", "ContentType", "FileBase64" };
            string TableName = string.Empty;
            try
            {
                TableName = _objProjectAllocatedConsumedReportRequestParam.Action switch
                {
                    "Search" => "PROJECT_ALLOCATED_CONSUMED_REPORT_RESPONSE",
                    "Export" => "PROJECT_ALLOCATED_CONSUMED_EXPORTED_RESPONSE",
                    _ => "PROJECT_ALLOCATED_CONSUMED_REPORT_RESPONSE"
                };
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objProjectAllocatedConsumedReportRequestParam.ProjectID }
                };
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.PROJECT_ALLOCATED_CONSUMED, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    if(_objProjectAllocatedConsumedReportRequestParam.Action == "Search")
                    {
                        finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
                    }
                    else
                    {
                        const int NoOfColumns = 16;
                        string SheetName = "ProjectAllocatedOrConsumed";
                        string Title = "Project Allocated/Consumed Activity Split Report";
                        string FileName = $"ProjectAllocatedOrConsumedReport" + FileHelper.GetFileExtension(FileTypes.Xlsx);
                        string ContentType = _ContentTypeSvc.GetFileContentType(FileName);

                        var ProjectAllocatedConsumedReportDataTable = Helper.ConvertDataSetToDataTable(ds);
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "SLNo", "SL No");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "ProjectCode", "Project Code");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "ProjectName", "Project Name");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "ClientName", "Client");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "AllocatedHours", "Allocated Hours");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "ConsumedHours", "Consumed Hours");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "RequirementGatheringAndAnalysis", "Requirement Gathering & Analysis");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "SystemDesign", "System Design");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "FrontendDevelopment", "Frontend Development");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "BackendDevelopment", "Backend Development");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "CodeReview", "Code Review");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "SystemTesting", "System Testing");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "ProjectManagement", "Project Management");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "PostDeploymentMonitoring", "Post-Deployment Monitoring");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "FixPostReleaseBugs", "Fix Post-Release Bugs");
                        Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "KnowledgeTransfer", "Knowledge Transfer");
                        Helper.RemoveColumnFromDataTable(ProjectAllocatedConsumedReportDataTable, "ProjectID");

                        var ColumnsConvertToNumber = new string[] { "Allocated Hours", "Consumed Hours", "Requirement Gathering & Analysis", "System Design", "Frontend Development", "Backend Development", "Code Review", "System Testing", 
                            "Project Management", "Deployment", "Post-Deployment Monitoring", "Fix Post-Release Bugs", "Maintenance", "Knowledge Transfer", "Learning" };
                        string base64Excel = ExcelHelper.ExportToExcelBase64(ProjectAllocatedConsumedReportDataTable, SheetName, Title, "A1:S1", 1, NoOfColumns, true, NoOfColumns, false, 0, "#FFFFFF", FileName, true, ColumnsConvertToNumber, "#4F81BD", "#DCE6F1", true);
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
                    if (_objProjectAllocatedConsumedReportRequestParam.Action == "Search")
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureReportsServiceLogNamespace, $"{ReportServiceLogClassName}.GetProjectAllocatedConsumedReportList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Project Allocated Consumed Report]

        #region [Employee Leave Report]

        /// <summary>
        /// This Method Is Used To Get Employee Leave Report List.
        /// </summary>
        /// <param name="_objEmployeeLeaveReportRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmployeeLeaveReportList(EmployeeLeaveReportRequestParam _objEmployeeLeaveReportRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "Status", "FileName", "ContentType", "FileBase64" };
            string TableName = string.Empty;
            try
            {
                TableName = _objEmployeeLeaveReportRequestParam.Action switch
                {
                    "Search" => "EMPLOYEE_LEAVE_REPORT_RESPONSE",
                    "Export" => "EMPLOYEE_LEAVE_EXPORTED_RESPONSE",
                    _ => "EMPLOYEE_LEAVE_REPORT_RESPONSE"
                };
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@ProjectTimeSheetID", 0 },
                    { "@EmployeeCode", _objEmployeeLeaveReportRequestParam.EmployeeCode },
                    { "@TimeSheetMonth", _objEmployeeLeaveReportRequestParam.LeaveMonth },
                    { "@TimeSheetYear", _objEmployeeLeaveReportRequestParam.LeaveYear },
                    { "@ApproverEmailID", string.Empty },
                    { "@UserAccessToken", string.Empty },
                    { "@ParamType", "GetLeaveReport" }
                };
                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_PROJECT_TIMESHEET_SUBMISSION, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                {
                    string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        if (_objEmployeeLeaveReportRequestParam.Action == "Search")
                        {
                            finalDataSet = Helper.CheckAndValidateDatasetTable(ds, TableName);
                        }
                        else
                        {
                            const int NoOfColumns = 6;
                            string SheetName = $"Leave Report {_objEmployeeLeaveReportRequestParam.LeaveMonth + "_" + _objEmployeeLeaveReportRequestParam.LeaveYear}";
                            string Title = "Leave Report For: " + _objEmployeeLeaveReportRequestParam.LeaveMonth + " " + _objEmployeeLeaveReportRequestParam.LeaveYear;
                            string FileName = $"{SheetName}{FileHelper.GetFileExtension(FileTypes.Xlsx)}";

                            string ContentType = _ContentTypeSvc.GetFileContentType(FileName);

                            var ProjectAllocatedConsumedReportDataTable = Helper.ConvertDataSetToDataTable(ds);
                            Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "SLNo", "SL No");
                            Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "FullName", "Employee Name");
                            Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "EmailID", "Email ID");
                            Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "OnLeave", "On Leave");
                            Helper.RenameDataTableColumnName(ProjectAllocatedConsumedReportDataTable, "CompOff", "Compoff");
                            Helper.RemoveColumnFromDataTable(ProjectAllocatedConsumedReportDataTable, "Status");

                            var ColumnsConvertToNumber = new string[] { "On Leave", "Holiday", "Compoff" };
                            string base64Excel = ExcelHelper.ExportToExcelBase64(ProjectAllocatedConsumedReportDataTable, SheetName, Title, "A1:F1", 1, NoOfColumns, true, NoOfColumns, false, 0, "#FFFFFF", FileName, true, ColumnsConvertToNumber, "#4F81BD", "#DCE6F1", true);
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
                        if (_objEmployeeLeaveReportRequestParam.Action == "Search")
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
                    if (_objEmployeeLeaveReportRequestParam.Action == "Search")
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureReportsServiceLogNamespace, $"{ReportServiceLogClassName}.GetEmployeeLeaveReportList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Employee Leave Report]
    }
}
