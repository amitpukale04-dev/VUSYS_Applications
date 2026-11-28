using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.TimesheetManagement;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.TimesheetManagement;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class AIAgentTimesheetManagementController : BaseApiAuthorizationController
    {
        private readonly IAITimesheetManagementAgentService _AITimesheetManagementAgentService;
        private const string ControllerLogName = "AIAgentTimesheetManagementController";

        /// <summary>
        /// AI Agent Timesheet Management Controller Class Constructor
        /// </summary>
        /// <param name="AITimesheetManagementAgentService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AIAgentTimesheetManagementController(IAITimesheetManagementAgentService AITimesheetManagementAgentService)
        {
            _AITimesheetManagementAgentService = AITimesheetManagementAgentService ?? throw new ArgumentNullException(nameof(AITimesheetManagementAgentService));
        }

        #region [AI Agent Timesheet Application]

        /// <summary>
        /// This Post Action Method Is Used To Process Timesheet Management Request From AI Agent.
        /// </summary>
        /// <param name="_objAITimesheetManagementRequestParam"></param>
        /// To Process Timesheet Management Request From AI Agent Below Request Parameters is required Such as Message
        /// <returns>
        /// A JSON-formatted string representing the Processing The Timesheet Request From AI Agent.
        /// </returns>
        [HttpPost, ActionName("ProcessAITimesheetManagementRequest")]
        public async Task<ActionResult<string>> ProcessAITimesheetManagementRequest(AITimesheetManagementRequestParam _objAITimesheetManagementRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _AITimesheetManagementAgentService.ProcessAITimesheetManagementRequest(_objAITimesheetManagementRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ProcessAITimesheetManagementRequest()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Confirm Timesheet Management Action From AI Agent.
        /// </summary>
        /// <param name="_objAITimesheetManagementConfirmRequestParam"></param>
        /// To Confirm Timesheet Management Action From AI Agent Below Request Parameters is required Such as ConfirmationToken
        /// <returns>
        /// A JSON-formatted string representing the Confirm Timesheet Action From AI Agent.
        /// </returns>
        [HttpPost, ActionName("ConfirmAITimesheetManagementAction")]
        public async Task<ActionResult<string>> ConfirmAITimesheetManagementAction(AITimesheetManagementConfirmRequestParam _objAITimesheetManagementConfirmRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _AITimesheetManagementAgentService.ConfirmAITimesheetManagementAction(_objAITimesheetManagementConfirmRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ConfirmAITimesheetManagementAction()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [AI Agent Timesheet Application]
    }
}

