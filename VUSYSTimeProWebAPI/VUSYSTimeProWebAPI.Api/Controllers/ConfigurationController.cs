using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Configuration;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class ConfigurationController : BaseApiAuthorizationController
    {
        private readonly IConfigurationService _ConfigurationService;
        private const string ControllerLogName = "ConfigurationController";

        /// <summary>
        /// Configuration Controller Class Constructor
        /// </summary>
        /// <param name="ConfigurationService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConfigurationController(IConfigurationService ConfigurationService)
        {
            _ConfigurationService = ConfigurationService ?? throw new ArgumentNullException(nameof(ConfigurationService));
        }

        #region [Email Configuration]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Email Configuration Details.
        /// </summary>
        /// <param name="_objConfigureEmailRequestParameterCommon"></param>
        /// To Save and Update Email Configuration Details Below Request Parameters is required Such as Id, FromEmail, Password, SmtpHost, PortNo, IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Email Configuration Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateEmailConfigurationDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateEmailConfigurationDetails(ConfigureEmailRequestParameterCommon _objConfigureEmailRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ConfigureEmailM _objConfigureEmailM = Helper.ToCopyClassProperties<ConfigureEmailRequestParameterCommon, ConfigureEmailM>(_objConfigureEmailRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ConfigurationService.SaveUpdateEmailConfigurationDetails(_objConfigureEmailM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateEmailConfigurationDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Email Configuration Details.
        /// </summary>
        /// <param name="_objConfigureEmailDetailsRequestParam"></param>
        /// To Get Email Configuration Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Email Configuration Details.
        /// </returns>
        [HttpPost, ActionName("GetEmailConfigurationDetails")]
        public async Task<ActionResult<string>> GetEmailConfigurationDetails(ConfigureEmailDetailsRequestParam _objConfigureEmailDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ConfigurationService.GetEmailConfigurationDetailOrList(_objConfigureEmailDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmailConfigurationDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Email Configuration List.
        /// </summary>
        /// <param name="_objConfigureEmailDetailsRequestParam"></param>
        /// To Get Email Configuration List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Email Configuration List.
        /// </returns>
        [HttpPost, ActionName("GetEmailConfigurationList")]
        public async Task<ActionResult<string>> GetEmailConfigurationList(ConfigureEmailDetailsRequestParam _objConfigureEmailDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objConfigureEmailDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ConfigurationService.GetEmailConfigurationDetailOrList(_objConfigureEmailDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmailConfigurationList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Email Configuration]
    }
}
