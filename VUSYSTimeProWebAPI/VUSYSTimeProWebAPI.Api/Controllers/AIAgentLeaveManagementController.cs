using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class AIAgentLeaveManagementController : BaseApiAuthorizationController
    {
        private readonly IAILeaveManagementAgentService _AILeaveManagementAgentService;
        private const string ControllerLogName = "AIAgentLeaveManagementController";

        /// <summary>
        /// AI Agent Leave Management Controller Class Constructor
        /// </summary>
        /// <param name="AILeaveManagementAgentService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AIAgentLeaveManagementController(IAILeaveManagementAgentService AILeaveManagementAgentService)
        {
            _AILeaveManagementAgentService = AILeaveManagementAgentService ?? throw new ArgumentNullException(nameof(AILeaveManagementAgentService));
        }

        #region [AI Agent Leave Application]

        /// <summary>
        /// This Post Action Method Is Used To Process Leave Management Request From AI Agent.
        /// </summary>
        /// <param name="_objAILeaveManagementRequestParam"></param>
        /// To Process Leave Management Request From AI Agent Below Request Parameters is required Such as Message
        /// <returns>
        /// A JSON-formatted string representing the Processing The Leave Request From AI Agent.
        /// </returns>
        [HttpPost, ActionName("ProcessAILeaveManagementRequest")]
        public async Task<ActionResult<string>> ProcessAILeaveManagementRequest(AILeaveManagementRequestParam _objAILeaveManagementRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _AILeaveManagementAgentService.ProcessAILeaveManagementRequest(_objAILeaveManagementRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ProcessAILeaveManagementRequest()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Confirm Leave Management Action From AI Agent.
        /// </summary>
        /// <param name="_objAILeaveManagementConfirmRequestParam"></param>
        /// To Confirm Leave Management Action From AI Agent Below Request Parameters is required Such as ConfirmationToken
        /// <returns>
        /// A JSON-formatted string representing the Confirm Leave Action From AI Agent.
        /// </returns>
        [HttpPost, ActionName("ConfirmAILeaveManagementAction")]
        public async Task<ActionResult<string>> ConfirmAILeaveManagementAction(AILeaveManagementConfirmRequestParam _objAILeaveManagementConfirmRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _AILeaveManagementAgentService.ConfirmAILeaveManagementAction(_objAILeaveManagementConfirmRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ConfirmAILeaveManagementAction()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [AI Agent Leave Application]
    }
}

