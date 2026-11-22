using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Models.FileUploads;
using VUSYSTimeProWebAPI.Api.Services.Files;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Employee;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class EmployeeController : BaseApiAuthorizationController
    {
        private readonly IEmployeeService _EmployeeService;
        private readonly IFormFileService _FormFileService;
        private readonly IDropdownService _IDropdownService;
        private const string ControllerLogName = "EmployeeController";

        /// <summary>
        /// Employee Controller Class Constructor
        /// </summary>
        /// <param name="EmployeeService"></param>
        /// <param name="FormFileService"></param>
        /// <param name="DropdownService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EmployeeController(IEmployeeService EmployeeService, IFormFileService FormFileService, IDropdownService DropdownService)
        {
            _EmployeeService = EmployeeService ?? throw new ArgumentNullException(nameof(EmployeeService));
            _FormFileService = FormFileService ?? throw new ArgumentNullException(nameof(FormFileService));
            _IDropdownService = DropdownService ?? throw new ArgumentNullException(nameof(DropdownService));
        }

        #region [Employee Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Employee Details.
        /// </summary>
        /// <param name="_objEmployeeRequestParam"></param>
        /// To Save and Update Employee Details Below Request Parameters is required Such as EmployeeID, EmployeeCode, FullName, GenderID, DateofBirth, 
        /// MobileNo, EmailID, CountryID, StateID, CityName, PinCode, Skills, EmployeeTypeID, HireDate, ParentCompanyName and ProfileUserImage.
        /// <returns>
        /// A JSON-formatted string representing the Employee Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateEmployeeDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateEmployeeDetails(EmployeeRequestParam _objEmployeeRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (_objEmployeeRequestParam.ProfileUserImage == null)
                {
                    ModelState.Remove("ProfileUserImage");
                }
                if (!ModelState.IsValid)
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    return string.IsNullOrEmpty(ReturnResult) ? NotFound() : Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
                }
                var Files = Request.Form.Files;
                string ImageFileName = string.Empty;
                byte[] _myData = null;
                if (Files.Count > 0)
                {
                    ImageFileName = _objEmployeeRequestParam.ProfileUserImage.FileName;
                    _myData = await _FormFileService.ConvertToBytesAsync(_objEmployeeRequestParam.ProfileUserImage, RequestCancellationToken);
                }
                else
                {
                    if (_objEmployeeRequestParam.EmployeeID == 0)
                    {
                        string strGenderName = await _IDropdownService.GetGenderNameAsync(_objEmployeeRequestParam.GenderID);
                        if (strGenderName == "MALE")
                        {
                            ImageFileName = "Default_Male.png";
                        }
                        else if (strGenderName == "FEMALE")
                        {
                            ImageFileName = "Default_Female.png";
                        }
                        _myData = await _FormFileService.GetDefaultUserImageBytesAsync(ImageFileName, RequestCancellationToken);
                    }
                }
                EmployeeM _objEmployeeM = Helper.ToCopyClassProperties<EmployeeRequestParam, EmployeeM>(_objEmployeeRequestParam);
                _objEmployeeM.EmployeeImageFileName = ImageFileName;
                _objEmployeeM.EmployeeProfileUserImage = _myData;
                ReturnResult = ObjectToJson.ConvertDataSetToJson(await _EmployeeService.SaveUpdateEmployeeDetails(_objEmployeeM, RequestCancellationToken));
                if (ReturnResult == null || ReturnResult == string.Empty)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateEmployeeDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Details.
        /// </summary>
        /// <param name="_objEmployeeDetailsRequestParam"></param>
        /// To Get Employee Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Employee Details.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeDetails")]
        public async Task<ActionResult<string>> GetEmployeeDetails(EmployeeDetailsRequestParam _objEmployeeDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _EmployeeService.GetEmployeeDetailOrList(_objEmployeeDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee List.
        /// </summary>
        /// <param name="_objEmployeeDetailsRequestParam"></param>
        /// To Get Employee List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Employee List.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeList")]
        public async Task<ActionResult<string>> GetEmployeeList(EmployeeDetailsRequestParam _objEmployeeDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objEmployeeDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _EmployeeService.GetEmployeeDetailOrList(_objEmployeeDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Employee Master]

        #region [Employee Type Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update EmployeeType Details.
        /// </summary>
        /// <param name="_objEmployeeTypeRequestParameterCommon"></param>
        /// To Save and Update Employee Type Details Below Request Parameters is required Such as Id, EmployeeType, CreatedBy, UpdatedBy.
        /// <returns>
        /// A JSON-formatted string representing the Employee Type Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateEmployeeTypeDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateEmployeeTypeDetails(EmployeeTypeRequestParameterCommon _objEmployeeTypeRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if(ModelState.IsValid)
                {
                    EmployeeTypeM _objEmployeeTypeM = Helper.ToCopyClassProperties<EmployeeTypeRequestParameterCommon, EmployeeTypeM>(_objEmployeeTypeRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _EmployeeService.SaveUpdateEmployeeTypeDetails(_objEmployeeTypeM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateEmployeeTypeDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Type Details.
        /// </summary>
        /// <param name="_objEmployeeTypeDetailsRequestParam"></param>
        /// To Get Employee Type Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Employee Type Details.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeTypeDetails")]
        public async Task<ActionResult<string>> GetEmployeeTypeDetails(EmployeeTypeDetailsRequestParam _objEmployeeTypeDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _EmployeeService.GetEmployeeTypeDetailOrList(_objEmployeeTypeDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeTypeDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Type List.
        /// </summary>
        /// <param name="_objEmployeeTypeDetailsRequestParam"></param>
        /// To Get Employee Type List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Employee Type List.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeTypeList")]
        public async Task<ActionResult<string>> GetEmployeeTypeList(EmployeeTypeDetailsRequestParam _objEmployeeTypeDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objEmployeeTypeDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _EmployeeService.GetEmployeeTypeDetailOrList(_objEmployeeTypeDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeTypeList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Employee Type Master]

    }
}
