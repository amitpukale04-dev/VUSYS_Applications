using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.FrontEndUserTabs;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class FrontEndUserTabsController : BaseApiAuthorizationController
    {
        private readonly IUserTabsService _UserTabsService;
        private const string ControllerLogName = "FrontEndUserTabsController";

        /// <summary>
        /// Front End User Tabs Controller Class Constructor
        /// </summary>
        /// <param name="UserTabsService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FrontEndUserTabsController(IUserTabsService UserTabsService)
        {
            _UserTabsService = UserTabsService ?? throw new ArgumentNullException(nameof(UserTabsService));
        }

        #region [User Tabs]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update User Tabs.
        /// </summary>
        /// <param name="_objSaveUserTabsRequestParam"></param>
        /// To Save and Update User Tabs Details Below Request Parameters is required Such as UserId, Path, Label, SvgIcon, Tone and Closable
        /// <returns>
        /// A JSON-formatted string representing the User Tab Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateUserTabs")]
        public async Task<ActionResult<string>> SaveAndUpdateUserTabs(SaveUserTabsRequestParam _objSaveUserTabsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserTabsService.SaveUserTabs(_objSaveUserTabsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateUserTabs()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get User Tab List.
        /// </summary>
        /// <param name="_objUserTabsRequestParam"></param>
        /// To Get User Tab List Below Request Parameters is required Such as UserId.
        /// <returns>
        /// A JSON-formatted string representing the User Tab List.
        /// </returns>
        [HttpPost, ActionName("GetUserTabList")]
        public async Task<ActionResult<string>> GetUserTabList(UserTabsRequestParam _objUserTabsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserTabsService.GetUserTabs(_objUserTabsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetUserTabList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [User Tabs]
    }
}
