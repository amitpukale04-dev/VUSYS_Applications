using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Timesheet;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class TimesheetController : BaseApiAuthorizationController
    {
        private readonly ITimesheetService _TimesheetService;
        private const string ControllerLogName = "TimesheetController";

        /// <summary>
        /// Timesheet Controller Class Constructor
        /// </summary>
        /// <param name="TimesheetService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public TimesheetController(ITimesheetService TimesheetService)
        {
            _TimesheetService = TimesheetService ?? throw new ArgumentNullException(nameof(TimesheetService));
        }

        #region [Timesheet]

        /// <summary>
        /// This Post Action Method Is Used To Get Timesheet Submission List.
        /// </summary>
        /// <param name="_objTimesheetSubmissionRequestParam"></param>
        /// To Get Timesheet Submission List Below Request Parameters is required Such as EmployeeCode, TimesheetMonth, TimesheetYear and ParamType.
        /// <returns>
        /// A JSON-formatted string representing the Timesheet Submission List.
        /// </returns>
        [HttpPost, ActionName("GetTimesheetSubmissionList")]
        public async Task<ActionResult<string>> GetTimesheetSubmissionList(TimesheetSubmissionRequestParam _objTimesheetSubmissionRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.GetTimesheetSubmissionList(_objTimesheetSubmissionRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetTimesheetSubmissionList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Log Timesheet List.
        /// </summary>
        /// <param name="_objLogTimesheetListRequestParam"></param>
        /// To Get Log Timesheet List Below Request Parameters is required Such as ProjectTimeSheetID, EmployeeCode, FromDate and ToDate.
        /// <returns>
        /// A JSON-formatted string representing the Log Timesheet List.
        /// </returns>
        [HttpPost, ActionName("GetLogTimesheetList")]
        public async Task<ActionResult<string>> GetLogTimesheetList(LogTimesheetListRequestParam _objLogTimesheetListRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.GetLogTimesheetList(_objLogTimesheetListRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetLogTimesheetList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Timesheet Details.
        /// </summary>
        /// <param name="_objGetTimesheetDetailsRequestParam"></param>
        /// To Get Timesheet Details Below Request Parameters is required Such as ProjectTimeSheetID, EmployeeCode, TaskDate, TimesheetMonth and TimesheetYear.
        /// <returns>
        /// A JSON-formatted string representing the Timesheet Details.
        /// </returns>
        [HttpPost, ActionName("GetTimesheetDetails")]
        public async Task<ActionResult<string>> GetTimesheetDetails(GetTimesheetDetailsRequestParam _objGetTimesheetDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.GetTimesheetDetails(_objGetTimesheetDetailsRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetTimesheetDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Save Timesheet Details.
        /// </summary>
        /// <param name="lstProjectTimesheetRequestParameterCommon"></param>
        /// To Save Timesheet Details Below Request Parameters is required Such as ProjectTimeSheetID, ProjectTimeSheetDetailID, EmployeeCode, ProjectID, ProjectActivityID, 
        /// TaskDate, TaskDescription, TaskDescriptionRaw, NoofHoursWorked, IsOnLeave, IsDeleted, CreatedBy and UpdatedBy.
        /// <returns>
        /// A JSON-formatted string representing the Saved/Updated Timesheet Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateTimesheetDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateTimesheetDetails(List<ProjectTimesheetRequestParameterCommon> lstProjectTimesheetRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    List<ProjectTimesheet> lstProjectTimesheet = Helper.ToCopyGenericListClassProperties<ProjectTimesheetRequestParameterCommon, ProjectTimesheet>(lstProjectTimesheetRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.SaveUpdateTimesheetDetails(lstProjectTimesheet, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateTimesheetDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Send Timesheet Submission Reminder Notification To Employee.
        /// </summary>
        /// <param name="_objTimesheetReportRequestParam"></param>
        /// To Send Timesheet Submission Reminder Notification To Employee Below Request Parameters is required Such as EmployeeCode, TimesheetMonth, 
        /// TimesheetYear and ParamType.
        /// <returns>
        /// A JSON-formatted string representing the Timesheet Submission List.
        /// </returns>
        [HttpPost, ActionName("SendTimesheetSubmissionReminderNotificationToEmployee")]
        public async Task<ActionResult<string>> SendTimesheetSubmissionReminderNotificationToEmployee(TimesheetReportRequestParam _objTimesheetReportRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.SendTimesheetSubmissionReminderNotificationToEmployee(_objTimesheetReportRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SendTimesheetSubmissionReminderNotificationToEmployee()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get or Export Employee Timesheet List.
        /// </summary>
        /// <param name="_objProjectTimesheetApproveRejection"></param>
        /// To Get or Export Employee Timesheet List Below Request Parameters is required Such as ProjectID, ProjectTimeSheetID, EmployeeCode, TimesheetMonth, TimesheetYear and Action(View/Export/ExportDateWise).
        /// <returns>
        /// A JSON-formatted string representing the Employee Timesheet List.
        /// </returns>
        [HttpPost, ActionName("GetOrExportEmployeeTimesheetList")]
        public async Task<ActionResult<string>> GetOrExportEmployeeTimesheetList(ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    switch (_objProjectTimesheetApproveRejection.Action)
                    {
                        case "ExportProjectWise":
                            break;
                        default:
                            if (string.IsNullOrEmpty(_objProjectTimesheetApproveRejection.EmployeeCode))
                            {
                                ModelState.AddModelError("EmployeeCode", "Enter The Employee Code.");
                                ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                                if (string.IsNullOrEmpty(ReturnResult))
                                {
                                    return NotFound();
                                }
                                return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
                            }
                            break;
                    }
                    if(_objProjectTimesheetApproveRejection.Action == "ExportProjectWise")
                    {
                        ProjectTimesheetDownload _objProjectTimesheetDownload = Helper.ToCopyClassProperties<ProjectTimesheetApproveRejection, ProjectTimesheetDownload>(_objProjectTimesheetApproveRejection);
                        ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.ExportEmployeeProjectTimesheet(_objProjectTimesheetDownload, RequestCancellationToken));
                    }
                    else
                    {
                        ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.GetOrExportEmployeeTimesheetList(_objProjectTimesheetApproveRejection, RequestCancellationToken));
                    }
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetOrExportEmployeeTimesheetList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Check Timesheet Approval Status.
        /// </summary>
        /// <param name="_objCheckTimesheetApprovalStatusRequestParam"></param>
        /// To Check Timesheet Approval Status Below Request Parameters is required Such as EmployeeCode, TimesheetMonth and TimesheetYear.
        /// <returns>
        /// A JSON-formatted string representing the Timesheet Approval Status.
        /// </returns>
        [HttpPost, ActionName("CheckTimesheetApprovalStatus")]
        public async Task<ActionResult<string>> CheckTimesheetApprovalStatus(CheckTimesheetApprovalStatusRequestParam _objCheckTimesheetApprovalStatusRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.CheckTimesheetApprovalStatus(_objCheckTimesheetApprovalStatusRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.CheckTimesheetApprovalStatus()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Submit Timesheet Details For Approval.
        /// </summary>
        /// <param name="_objTimesheetSubmitForApprovalRequestParam"></param>
        /// To Submit Timesheet Details For Approval Below Request Parameters is required Such as ProjectTimeSheetID, EmployeeCode, TimesheetMonth, 
        /// TimesheetYear and ApproveStatus.
        /// <returns>
        /// A JSON-formatted string representing the Submitted Timesheet Details Approval Response.
        /// </returns>
        [HttpPost, ActionName("SubmitTimesheetDetailsForApproval")]
        public async Task<ActionResult<string>> SubmitTimesheetDetailsForApproval(TimesheetSubmitForApprovalRequestParam _objTimesheetSubmitForApprovalRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.SubmitTimesheetDetailsForApproval(_objTimesheetSubmitForApprovalRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SubmitTimesheetDetailsForApproval()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Approve Or Reject Timesheet.
        /// </summary>
        /// <param name="_objTimesheetApproveRejectRequestParam"></param>
        /// To Approve Or Reject Timesheet Below Request Parameters is required Such as ProjectTimeSheetID, EmployeeCode, TimesheetMonth, 
        /// TimesheetYear, Remarks, UserToken, Source and ApproveStatus.
        /// <returns>
        /// A JSON-formatted string representing the Approved Or Rejected Timesheet Response.
        /// </returns>
        [HttpPost, ActionName("ApproveOrRejectTimesheet")]
        public async Task<ActionResult<string>> ApproveOrRejectTimesheet(TimesheetApproveRejectRequestParam _objTimesheetApproveRejectRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.ApproveOrRejectTimesheet(_objTimesheetApproveRejectRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ApproveOrRejectTimesheet()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Timesheet Not Filled List and Send Email Notification.
        /// </summary>
        /// To Get Employee Timesheet Not Filled List and Send Email Notification Request Parameters is None.
        /// <returns>
        /// A JSON-formatted string representing the Employee Timesheet Not Filled List and Send Email Notification Response.
        /// </returns>
        [HttpPost, ActionName("GetEmployeesTimesheetNotFilledListAndSendEmailNotification")]
        public async Task<ActionResult<string>> GetEmployeesTimesheetNotFilledListAndSendEmailNotification()
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.GetEmployeesTimesheetNotFilledListAndSendEmailNotification(RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeesTimesheetNotFilledListAndSendEmailNotification()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employees Leave, Holiday and Sync To Project Timesheet.
        /// </summary>
        /// To Get Employees Leave, Holiday and Sync To Project Timesheet Request Parameters is None.
        /// <returns>
        /// A JSON-formatted string representing the Get Employees Leave, Holiday and Sync To Project Timesheet Response.
        /// </returns>
        [HttpPost, ActionName("GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet")]
        public async Task<ActionResult<string>> GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet()
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _TimesheetService.GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet(RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Timesheet]
    }
}
