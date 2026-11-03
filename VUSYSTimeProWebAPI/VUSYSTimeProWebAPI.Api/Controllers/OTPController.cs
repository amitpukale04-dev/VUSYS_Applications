using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.OTPService;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class OTPController : BaseApiController
    {
        private readonly IOTPService _OTPService;
        private const string ControllerLogName = "OTPController";

        /// <summary>
        /// OTP Controller Class Constructor
        /// </summary>
        /// <param name="OTPService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OTPController(IOTPService OTPService)
        {
            _OTPService = OTPService ?? throw new ArgumentNullException(nameof(OTPService));
        }

        #region [OTP]

        /// <summary>
        /// This Post Action Method Is Used To Generate Or Resend OTP.
        /// </summary>
        /// <param name="_objOTP">
        /// To Generate OTP Below Request Parameters is required Such as EmailID.
        /// <b>Mandatory Fields are:</b> EmailID
        /// </param>
        [HttpPost, ActionName("GenerateOrResendOTP")]
        public async Task<ActionResult<string>> GenerateOrResendOTP(OTP _objOTP)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _OTPService.GenerateOrResendOTP(_objOTP, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GenerateOrResendOTP()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [OTP]

    }
}
