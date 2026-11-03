using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Models.FileUploads;
using VUSYSTimeProWebAPI.Api.Services.Files;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Master;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class MasterController : BaseApiAuthorizationController
    {
        private readonly IMasterService _MasterService;
        private readonly IFormFileService _FormFileService;
        private readonly IDropdownService _IDropdownService;
        private const string ControllerLogName = "MasterController";

        /// <summary>
        /// Master Controller Class Constructor
        /// </summary>
        /// <param name="MasterService"></param>
        /// <param name="FormFileService"></param>
        /// <param name="DropdownService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MasterController(IMasterService MasterService, IFormFileService FormFileService, IDropdownService DropdownService)
        {
            _MasterService = MasterService ?? throw new ArgumentNullException(nameof(MasterService));
            _FormFileService = FormFileService ?? throw new ArgumentNullException(nameof(FormFileService));
            _IDropdownService = DropdownService ?? throw new ArgumentNullException(nameof(DropdownService));
        }

        #region [User Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update User Details.
        /// </summary>
        /// <param name="_objUserRequestParam"></param>
        /// To Save and Update User Details Below Request Parameters is required Such as Id, UserName, FullName, Password, UserEmailID, MobileNumber, RoleID, EmployeeCode
        /// IsApprover, IsEnableTwoFactorAuthentication, ProfileUserImage.
        /// <returns>
        /// A JSON-formatted string representing the User Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateUserDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateUserDetails(UserRequestParam _objUserRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (_objUserRequestParam.ProfileUserImage == null)
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
                    ImageFileName = _objUserRequestParam.ProfileUserImage.FileName;
                    _myData = await _FormFileService.ConvertToBytesAsync(_objUserRequestParam.ProfileUserImage, RequestCancellationToken);
                }
                else
                {
                    if (_objUserRequestParam.Id == 0)
                    {
                        string strGenderName = await _IDropdownService.GetGenderNameAsync(_objUserRequestParam.EmployeeCode);
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
                UsersM _objUsersM = Helper.ToCopyClassProperties<UserRequestParam, UsersM>(_objUserRequestParam);
                _objUsersM.UserImageFileName = ImageFileName;
                _objUsersM.UserImage = _myData;
                ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateUserDetails(_objUsersM, RequestCancellationToken));
                if (ReturnResult == null || ReturnResult == string.Empty)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateUserDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get User Details.
        /// </summary>
        /// <param name="_objUserDetailsRequestParam"></param>
        /// To Get User Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the User Details.
        /// </returns>
        [HttpPost, ActionName("GetUserDetails")]
        public async Task<ActionResult<string>> GetUserDetails(UserDetailsRequestParam _objUserDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetUserDetailOrList(_objUserDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetUserDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get User List.
        /// </summary>
        /// <param name="_objUserDetailsRequestParam"></param>
        /// To Get User List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the User List.
        /// </returns>
        [HttpPost, ActionName("GetUserList")]
        public async Task<ActionResult<string>> GetUserList(UserDetailsRequestParam _objUserDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objUserDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetUserDetailOrList(_objUserDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetUserList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [User Master]

        #region [Role Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Role Details.
        /// </summary>
        /// <param name="_objRoleRequestParameterCommon"></param>
        /// To Save and Update Role Details Below Request Parameters is required Such as Id, RoleName, IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Role Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateRoleDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateRoleDetails(RoleRequestParameterCommon _objRoleRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    RolesM _objRolesM = Helper.ToCopyClassProperties<RoleRequestParameterCommon, RolesM>(_objRoleRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateRoleDetails(_objRolesM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateRoleDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Role Details.
        /// </summary>
        /// <param name="_objRoleDetailsRequestParam"></param>
        /// To Get Role Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Role Details.
        /// </returns>
        [HttpPost, ActionName("GetRoleDetails")]
        public async Task<ActionResult<string>> GetRoleDetails(RoleDetailsRequestParam _objRoleDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetRoleDetailOrList(_objRoleDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetRoleDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Role List.
        /// </summary>
        /// <param name="_objRoleDetailsRequestParam"></param>
        /// To Get Role List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Role List.
        /// </returns>
        [HttpPost, ActionName("GetRoleList")]
        public async Task<ActionResult<string>> GetRoleList(RoleDetailsRequestParam _objRoleDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objRoleDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetRoleDetailOrList(_objRoleDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetRoleList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Role Master]

        #region [Security Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Security Details.
        /// </summary>
        /// <param name="_objSecurityMasterParameterCommon"></param>
        /// To Save and Update Security Details Below Request Parameters is required Such as SecurityId, SecurityName, RoleID, ScreenName, IsDefault.
        /// <returns>
        /// A JSON-formatted string representing the Security Details.
        /// </returns>
        [HttpPost, ActionName("SaveUpdateSecurityDetails")]
        public async Task<ActionResult<string>> SaveUpdateSecurityDetails(SecurityMasterParameterCommon _objSecurityMasterParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateSecurityDetails(_objSecurityMasterParameterCommon, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveUpdateSecurityDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Security Details.
        /// </summary>
        /// <param name="_objSecurityMasterDetailsRequestParam"></param>
        /// To Get Security Details Below Request Parameters is required Such as SecurityId.
        /// <returns>
        /// A JSON-formatted string representing the Security Details.
        /// </returns>
        [HttpPost, ActionName("GetSecurityDetails")]
        public async Task<ActionResult<string>> GetSecurityDetails(SecurityMasterDetailsRequestParam _objSecurityMasterDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetSecurityDetailOrList(_objSecurityMasterDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetSecurityDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Security List.
        /// </summary>
        /// <param name="_objSecurityMasterDetailsRequestParam"></param>
        /// To Get Security List Below Request Parameters is required Such as SecurityId and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Security List.
        /// </returns>
        [HttpPost, ActionName("GetSecurityList")]
        public async Task<ActionResult<string>> GetSecurityList(SecurityMasterDetailsRequestParam _objSecurityMasterDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objSecurityMasterDetailsRequestParam.SecurityId = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetSecurityDetailOrList(_objSecurityMasterDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetSecurityList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Delete Security Details.
        /// </summary>
        /// <param name="_objSecurityMasterDetailsRequestParam"></param>
        /// To Delete Security Details Below Request Parameters is required Such as SecurityId.
        /// <returns>
        /// A JSON-formatted string representing the Security Details Deleted.
        /// </returns>
        [HttpPost, ActionName("DeleteSecurityDetails")]
        public async Task<ActionResult<string>> DeleteSecurityDetails(SecurityMasterDetailsRequestParam _objSecurityMasterDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.DeleteSecurityDetails(_objSecurityMasterDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.DeleteSecurityDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Security Master]

        #region [User Security Access]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update User Security Access Details.
        /// </summary>
        /// <param name="_lstUserSecurityAccess"></param>
        /// To Save and Update User Security Access Details Below Request Parameters is required Such as SecurityAccessId, SecurityID, UserID, CreatedBy and UpdatedBy.
        /// <returns>
        /// A JSON-formatted string representing the User Security Access Details.
        /// </returns>
        [HttpPost, ActionName("SaveUpdateUserSecurityAccessDetails")]
        public async Task<ActionResult<string>> SaveUpdateUserSecurityAccessDetails(List<UserSecurityAccess> _lstUserSecurityAccess)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateUserSecurityAccessDetails(_lstUserSecurityAccess, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveUpdateUserSecurityAccessDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get User Security Access List.
        /// </summary>
        /// <param name="_objUserSecurityAccess"></param>
        /// To Get User Security Access List Below Request Parameters is required Such as UserID and RoleID.
        /// <returns>
        /// A JSON-formatted string representing the User Security Access List.
        /// </returns>
        [HttpPost, ActionName("GetUserSecurityAccessList")]
        public async Task<ActionResult<string>> GetUserSecurityAccessList(UserSecurityAccess _objUserSecurityAccess)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetUserSecurityAccessList(_objUserSecurityAccess, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetUserSecurityAccessList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Delete User Security Access Details.
        /// </summary>
        /// <param name="_objUserSecurityAccess"></param>
        /// To Delete User Security Access Details Below Request Parameters is required Such as SecurityAccessId, SecurityID and UserID.
        /// <returns>
        /// A JSON-formatted string representing the User Security Access Details Deleted.
        /// </returns>
        [HttpPost, ActionName("DeleteUserSecurityDetails")]
        public async Task<ActionResult<string>> DeleteUserSecurityAccessDetails(UserSecurityAccess _objUserSecurityAccess)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.DeleteUserSecurityAccessDetails(_objUserSecurityAccess, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.DeleteUserSecurityAccessDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [User Security Access]

        #region [User Menu Access]

        /// <summary>
        /// This Post Action Method Is Used To Get User Menu Access.
        /// </summary>
        /// <param name="_objUserMenuAccessRequestParam"></param>
        /// To Get User Menu Access Below Request Parameters is required Such as UserId and RoleID.
        /// <returns>
        /// A JSON-formatted string representing the User Menu Access.
        /// </returns>
        [HttpPost, ActionName("GetUserMenuAccess")]
        public async Task<ActionResult<string>> GetUserMenuAccess(UserMenuAccessRequestParam _objUserMenuAccessRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = await _MasterService.GetUserMenuAccess(_objUserMenuAccessRequestParam, RequestCancellationToken);
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetUserMenuAccess()", ex.Message, false);
                throw;
            }
            return Content(ReturnResult, ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [User Menu Access]

        #region [Role Menu Access]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Role Menu Access Details.
        /// </summary>
        /// <param name="_lstRoleMenuAccessFrontEnd"></param>
        /// To Save and Update Role Menu Access Details Below Request Parameters is required Such as RoleMenuAccessFrontEndId, RoleID, SecurityID, CreatedBy and UpdatedBy.
        /// <returns>
        /// A JSON-formatted string representing the Role Menu Access Details.
        /// </returns>
        [HttpPost, ActionName("SaveUpdateRoleMenuAccessDetails")]
        public async Task<ActionResult<string>> SaveUpdateRoleMenuAccessDetails(List<RoleMenuAccessFrontEnd> _lstRoleMenuAccessFrontEnd)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateRoleMenuAccessDetails(_lstRoleMenuAccessFrontEnd, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveUpdateRoleMenuAccessDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Role Menu Access]

        #region [Dropdown Items]

        /// <summary>
        /// This Post Action Method Is Used To Get Dropdown Items based on Source Table.
        /// </summary>
        /// <param name="_objDropdownRequestParam">
        /// The Dropdown Request Parameters such as SourceTable, ArgumentValue and ArgumentValue1.
        /// </param>
        /// <returns>
        /// A JSON-formatted string representing the dropdown items.
        /// </returns>
        [HttpPost, ActionName("GetDropdownItems")]
        public async Task<ActionResult<string>> GetDropdownItems(DropdownRequestParam _objDropdownRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetDropdownItems(_objDropdownRequestParam, RequestCancellationToken));
                    if (string.IsNullOrEmpty(ReturnResult))
                    {
                        return NotFound();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (string.IsNullOrEmpty(ReturnResult))
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetDropdownItems()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Dropdown Items]

        #region [Gender Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Gender Details.
        /// </summary>
        /// <param name="_objGenderRequestParameterCommon"></param>
        /// To Save and Update Gender Details Below Request Parameters is required Such as Id, Gender, IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Gender Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateGenderDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateGenderDetails(GenderRequestParameterCommon _objGenderRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    GenderM _objGenderM = Helper.ToCopyClassProperties<GenderRequestParameterCommon, GenderM>(_objGenderRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateGenderDetails(_objGenderM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateGenderDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Gender Details.
        /// </summary>
        /// <param name="_objGenderDetailsRequestParam"></param>
        /// To Get Gender Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Gender Details.
        /// </returns>
        [HttpPost, ActionName("GetGenderDetails")]
        public async Task<ActionResult<string>> GetGenderDetails(GenderDetailsRequestParam _objGenderDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetGenderDetailOrList(_objGenderDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetGenderDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Gender List.
        /// </summary>
        /// <param name="_objGenderDetailsRequestParam"></param>
        /// To Get Gender List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Gender List.
        /// </returns>
        [HttpPost, ActionName("GetGenderList")]
        public async Task<ActionResult<string>> GetGenderList(GenderDetailsRequestParam _objGenderDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objGenderDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetGenderDetailOrList(_objGenderDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetGenderList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Gender Master]

        #region [Country Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Country Details.
        /// </summary>
        /// <param name="_objCountryRequestParameterCommon"></param>
        /// To Save and Update Country Details Below Request Parameters is required Such as Id, CountryCode, CountryName, IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Country Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateCountryDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateCountryDetails(CountryRequestParameterCommon _objCountryRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    CountryM _objCountryM = Helper.ToCopyClassProperties<CountryRequestParameterCommon, CountryM>(_objCountryRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateCountryDetails(_objCountryM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateCountryDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Country Details.
        /// </summary>
        /// <param name="_objCountryDetailsRequestParam"></param>
        /// To Get Country Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Country Details.
        /// </returns>
        [HttpPost, ActionName("GetCountryDetails")]
        public async Task<ActionResult<string>> GetCountryDetails(CountryDetailsRequestParam _objCountryDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetCountryDetailOrList(_objCountryDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetCountryDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Country List.
        /// </summary>
        /// <param name="_objCountryDetailsRequestParam"></param>
        /// To Get Country List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Country List.
        /// </returns>
        [HttpPost, ActionName("GetCountryList")]
        public async Task<ActionResult<string>> GetCountryList(CountryDetailsRequestParam _objCountryDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objCountryDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetCountryDetailOrList(_objCountryDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetCountryList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Country Master]

        #region [State Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update State Details.
        /// </summary>
        /// <param name="_objStateRequestParameterCommon"></param>
        /// To Save and Update State Details Below Request Parameters is required Such as Id, StateCode, StateName, CountryID, IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the State Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateStateDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateStateDetails(StateRequestParameterCommon _objStateRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    StateM _objStateM = Helper.ToCopyClassProperties<StateRequestParameterCommon, StateM>(_objStateRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateStateDetails(_objStateM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateStateDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get State Details.
        /// </summary>
        /// <param name="_objStateDetailsRequestParam"></param>
        /// To Get State Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the State Details.
        /// </returns>
        [HttpPost, ActionName("GetStateDetails")]
        public async Task<ActionResult<string>> GetStateDetails(StateDetailsRequestParam _objStateDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetStateDetailOrList(_objStateDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetStateDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get State List.
        /// </summary>
        /// <param name="_objStateDetailsRequestParam"></param>
        /// To Get State List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the State List.
        /// </returns>
        [HttpPost, ActionName("GetStateList")]
        public async Task<ActionResult<string>> GetStateList(StateDetailsRequestParam _objStateDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objStateDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetStateDetailOrList(_objStateDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetStateList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [State Master]

        #region [Holiday Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Holiday Details.
        /// </summary>
        /// <param name="_objHolidayRequestParameterCommon"></param>
        /// To Save and Update Holiday Details Below Request Parameters is required Such as Id, Holiday Name, Holiday Date, Holiday Year, Is Optional Holiday, Holiday Description, 
        /// IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Holiday Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateHolidayDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateHolidayDetails(HolidayRequestParameterCommon _objHolidayRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    HolidayM _objHolidayM = Helper.ToCopyClassProperties<HolidayRequestParameterCommon, HolidayM>(_objHolidayRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateHolidayDetails(_objHolidayM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateHolidayDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Holiday Details.
        /// </summary>
        /// <param name="_objHolidayDetailsRequestParam"></param>
        /// To Get Holiday Details Below Request Parameters is required Such as Id, Holiday Year.
        /// <returns>
        /// A JSON-formatted string representing the Holiday Details.
        /// </returns>
        [HttpPost, ActionName("GetHolidayDetails")]
        public async Task<ActionResult<string>> GetHolidayDetails(HolidayDetailsRequestParam _objHolidayDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetHolidayDetailOrList(_objHolidayDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetHolidayDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Holiday List.
        /// </summary>
        /// <param name="_objHolidayDetailsRequestParam"></param>
        /// To Get Holiday List Below Request Parameters is required Such as Id and Value must be 0 and Holiday Year.
        /// <returns>
        /// A JSON-formatted string representing The Holiday List.
        /// </returns>
        [HttpPost, ActionName("GetHolidayList")]
        public async Task<ActionResult<string>> GetHolidayList(HolidayDetailsRequestParam _objHolidayDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objHolidayDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetHolidayDetailOrList(_objHolidayDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetHolidayList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Holiday Master]

        #region [Leave Type Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Leave Type Details.
        /// </summary>
        /// <param name="_objLeaveTypeRequestParameterCommon"></param>
        /// To Save and Update Leave Type Details Below Request Parameters is required Such as Id, Leave Type Code, Leave Type, Leave Type Description, Default Leave Count, Is Paid Leave, Allow Half Day, 
        /// IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Leave Type Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateLeaveTypeDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateLeaveTypeDetails(LeaveTypeRequestParameterCommon _objLeaveTypeRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    LeaveTypeM _objLeaveTypeM = Helper.ToCopyClassProperties<LeaveTypeRequestParameterCommon, LeaveTypeM>(_objLeaveTypeRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.SaveUpdateLeaveTypeDetails(_objLeaveTypeM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateLeaveTypeDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Leave Type Details.
        /// </summary>
        /// <param name="_objLeaveTypeDetailsRequestParam"></param>
        /// To Get Leave Type Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Leave Type Details.
        /// </returns>
        [HttpPost, ActionName("GetLeaveTypeDetails")]
        public async Task<ActionResult<string>> GetLeaveTypeDetails(LeaveTypeDetailsRequestParam _objLeaveTypeDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetLeaveTypeDetailOrList(_objLeaveTypeDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetLeaveTypeDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Leave Type List.
        /// </summary>
        /// <param name="_objLeaveTypeDetailsRequestParam"></param>
        /// To Get Leave Type List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing The Leave Type List.
        /// </returns>
        [HttpPost, ActionName("GetLeaveTypeList")]
        public async Task<ActionResult<string>> GetLeaveTypeList(LeaveTypeDetailsRequestParam _objLeaveTypeDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objLeaveTypeDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _MasterService.GetLeaveTypeDetailOrList(_objLeaveTypeDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetLeaveTypeList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Leave Type Master]
    }
}
