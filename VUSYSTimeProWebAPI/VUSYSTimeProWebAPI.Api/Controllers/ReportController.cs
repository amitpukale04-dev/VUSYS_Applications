using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Reports;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class ReportController : BaseApiAuthorizationController
    {
        private readonly IReportService _ReportService;
        private const string ControllerLogName = "ReportController";

        /// <summary>
        /// Report Controller Class Constructor
        /// </summary>
        /// <param name="ReportService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ReportController(IReportService ReportService)
        {
            _ReportService = ReportService ?? throw new ArgumentNullException(nameof(ReportService));
        }

        #region [Timesheet Report]

        /// <summary>
        /// This Post Action Method Is Used To Get Timesheet Report List.
        /// </summary>
        /// <param name="_objTimesheetReportRequestParam"></param>
        /// To Get Timesheet Report List Below Request Parameters is required Such as EmployeeCode, TimesheetMonth and TimesheetYear.
        /// <returns>
        /// A JSON-formatted string representing the Timesheet Report List.
        /// </returns>
        [HttpPost, ActionName("GetTimesheetReportList")]
        public async Task<ActionResult<string>> GetTimesheetReportList(TimesheetReportRequestParam _objTimesheetReportRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ReportService.GetTimesheetReportList(_objTimesheetReportRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetTimesheetReportList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Timesheet Report]

        #region [Project Allocated Consumed Report]

        /// <summary>
        /// This Post Action Method Is Used To Get Project Allocated Consumed Report List.
        /// </summary>
        /// <param name="_objProjectAllocatedConsumedReportRequestParam"></param>
        /// To Get Project Allocated Consumed Report List Below Request Parameters is required Such as Project ID and Action(Search/Export).
        /// <returns>
        /// A JSON-formatted string representing the Project Allocated Consumed Report List.
        /// </returns>
        [HttpPost, ActionName("GetProjectAllocatedConsumedReportList")]
        public async Task<ActionResult<string>> GetProjectAllocatedConsumedReportList(ProjectAllocatedConsumedReportRequestParam _objProjectAllocatedConsumedReportRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ReportService.GetProjectAllocatedConsumedReportList(_objProjectAllocatedConsumedReportRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetProjectAllocatedConsumedReportList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Project Allocated Consumed Report]

        #region [Employee Leave Report]

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Leave Report List.
        /// </summary>
        /// <param name="_objEmployeeLeaveReportRequestParam"></param>
        /// To Get Employee Leave Report List Below Request Parameters is required Such as EmployeeCode, LeaveMonth, LeaveYear and Action(Search/Export).
        /// <returns>
        /// A JSON-formatted string representing the Employee Leave Report List.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeLeaveReportList")]
        public async Task<ActionResult<string>> GetEmployeeLeaveReportList(EmployeeLeaveReportRequestParam _objEmployeeLeaveReportRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ReportService.GetEmployeeLeaveReportList(_objEmployeeLeaveReportRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeLeaveReportList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Employee Leave Report]
    }
}
