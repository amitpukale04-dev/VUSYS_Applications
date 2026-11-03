using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Users;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IUserLoginService _UserLoginService;
        private const string ControllerLogName = "AuthController";

        /// <summary>
        /// Auth Controller Class Constructor
        /// </summary>
        /// <param name="UserLoginService"></param>
        public AuthController(IUserLoginService UserLoginService)
        {
            _UserLoginService = UserLoginService ?? throw new ArgumentNullException(nameof(UserLoginService));
        }

        /// <summary>
        /// This Post Action Method Is Used To Login.
        /// </summary>
        /// <param name="objUserLogin"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Login")]
        public async Task<ActionResult<string>> Login(UserLogin objUserLogin)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.GetUserLoginDetails(objUserLogin, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.Login()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Login From The Scheduler.
        /// </summary>
        /// <param name="objSchedulerUserLogin"></param>
        /// <returns></returns>
        [HttpPost, ActionName("SchedulerLogin")]
        public async Task<ActionResult<string>> SchedulerLogin(SchedulerUserLogin objSchedulerUserLogin)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.GetSchedulerUserLoginDetails(objSchedulerUserLogin, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SchedulerLogin()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Refresh Token.
        /// </summary>
        /// <param name="objTokenRefreshRequest"></param>
        /// <returns></returns>
        [HttpPost, ActionName("RefreshToken")]
        public async Task<ActionResult<string>> RefreshToken(TokenRefreshRequest objTokenRefreshRequest)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.GetRefreshToken(objTokenRefreshRequest));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.RefreshToken()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Change Password.
        /// </summary>
        /// <param name="_objChangePassword"></param>
        /// <returns></returns>
		[Authorize]
        [HttpPost, ActionName("ChangePassword")]
        public async Task<ActionResult<string>> ChangePassword(ChangePassword _objChangePassword)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.ChangePassword(_objChangePassword, "ChangePassword", RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ChangePassword()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Send Request For Reset Password.
        /// </summary>
        /// <param name="_objForgotPassword"></param>
        /// <returns></returns>
        [HttpPost, ActionName("ForgotPasswordRequest")]
        public async Task<ActionResult<string>> ForgotPasswordRequest(ForgotPassword _objForgotPassword)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.ForgotPassword(_objForgotPassword, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ForgotPasswordRequest()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Check For Password Link Available.
        /// </summary>
        /// <param name="_objCheckPasswordLinkAvailableRequestParam"></param>
        /// <returns></returns>
        [HttpPost, ActionName("CheckForPasswordLinkAvailable")]
        public async Task<ActionResult<string>> CheckForPasswordLinkAvailable(CheckPasswordLinkAvailableRequestParam _objCheckPasswordLinkAvailableRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.CheckForPasswordLinkAvailable(_objCheckPasswordLinkAvailableRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.CheckForPasswordLinkAvailable()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Reset Password.
        /// </summary>
        /// <param name="_objResetPassword"></param>
        /// <returns></returns>
        [HttpPost, ActionName("ResetPassword")]
        public async Task<ActionResult<string>> ResetPassword(ResetPassword _objResetPassword)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ChangePassword _objChangePassword = Helper.ToCopyClassProperties<ResetPassword, ChangePassword>(_objResetPassword);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.ChangePassword(_objChangePassword, "ResetPassword", RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ResetPassword()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Logout.
        /// </summary>
        /// <param name="objUserLoginCommon"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Logout")]
        public async Task<ActionResult<string>> Logout(UserLoginCommon objUserLoginCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _UserLoginService.Logout(objUserLoginCommon.EmailID, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.Logout()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Encrypt Text.
        /// </summary>
        /// <param name="_objEncryptTextRequest"></param>
        /// <returns></returns>
        [HttpPost, ActionName("EncryptText")]
        public async Task<ActionResult<string>> EncryptText(EncryptTextRequest _objEncryptTextRequest)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    if (_objEncryptTextRequest.EncryptDecryptKey == Helper.EncryptDecryptKey)
                    {
                        string EncryptedText = AESEncrytDecry.EncryptAES256(_objEncryptTextRequest.Text, _objEncryptTextRequest.EncryptDecryptKey);
                        string PlainText = _objEncryptTextRequest.Text;
                        var EncryptTextResponse = new EncryptTextResponse { PlainText = PlainText, EncryptedText = EncryptedText };
                        ReturnResult = ObjectToJson.ConvertDataSetToJson(Helper.ConvertClassObjectToDataSet(EncryptTextResponse, "ENCRYPTED_TEXT_RESPONSE"));
                        if (ReturnResult == null || ReturnResult == string.Empty)
                        {
                            return NotFound();
                        }
                    }
                    else
                    {
                        List<string> columns = new List<string> { "ID", "Message" };
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { "0", "Encrypt/Decrypt Key Is In-Valid !!!" }
                        };
                        ReturnResult = ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(columns, rows, "ENCRYPTED_TEXT_RESPONSE"));
                        if (ReturnResult == null || ReturnResult == string.Empty)
                        {
                            return NotFound();
                        }
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.EncryptText()", ex.Message, false);
                throw;
            }
            return Content(Util.OK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Decrypt Text.
        /// </summary>
        /// <param name="_objDecryptTextRequest"></param>
        /// <returns></returns>
        [HttpPost, ActionName("DecryptText")]
        public async Task<ActionResult<string>> DecryptText(DecryptTextRequest _objDecryptTextRequest)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    if (_objDecryptTextRequest.EncryptDecryptKey == Helper.EncryptDecryptKey)
                    {
                        string DecryptedText = AESEncrytDecry.DecryptAES256(_objDecryptTextRequest.EncryptedText, _objDecryptTextRequest.EncryptDecryptKey);
                        string EncryptedText = _objDecryptTextRequest.EncryptedText;
                        var DecryptTextResponse = new DecryptTextResponse { DecryptedText = DecryptedText, EncryptedText = EncryptedText };
                        ReturnResult = ObjectToJson.ConvertDataSetToJson(Helper.ConvertClassObjectToDataSet(DecryptTextResponse, "DECRYPTED_TEXT_RESPONSE"));
                        if (ReturnResult == null || ReturnResult == string.Empty)
                        {
                            return NotFound();
                        }
                    }
                    else
                    {
                        List<string> columns = new List<string> { "ID", "Message" };
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { "0", "Encrypt/Decrypt Key Is In-Valid !!!" }
                        };
                        ReturnResult = ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(columns, rows, "DECRYPTED_TEXT_RESPONSE"));
                        if (ReturnResult == null || ReturnResult == string.Empty)
                        {
                            return NotFound();
                        }
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.DecryptText()", ex.Message, false);
                throw;
            }
            return Content(Util.OK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }
    }
}
