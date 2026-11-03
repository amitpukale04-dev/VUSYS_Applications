using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class LeaveManagementController : BaseApiAuthorizationController
    {
        private readonly ILeaveManagementService _LeaveManagementService;
        private readonly IDropdownService _IDropdownService;
        private const string ControllerLogName = "LeaveManagementController";

        /// <summary>
        /// Leave Management Controller Class Constructor
        /// </summary>
        /// <param name="LeaveManagementService"></param>
        /// <param name="DropdownService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LeaveManagementController(ILeaveManagementService LeaveManagementService, IDropdownService DropdownService)
        {
            _LeaveManagementService = LeaveManagementService ?? throw new ArgumentNullException(nameof(LeaveManagementService));
            _IDropdownService = DropdownService ?? throw new ArgumentNullException(nameof(DropdownService));
        }

        #region [Employee Leave Balance]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Employee Leave Balance Details.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceRequestParameterCommon"></param>
        /// To Save and Update Employee Leave Balance Details Below Request Parameters is required Such as Id, Employee Code, Leave Type Id, Leave Year, Opening Balance, Allocated Leave, Used Leave, Pending Leave, Available Balance,
        /// IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Employee Leave Balance Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateEmployeeLeaveBalanceDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateEmployeeLeaveBalanceDetails(EmployeeLeaveBalanceRequestParameterCommon _objEmployeeLeaveBalanceRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    EmployeeLeaveBalanceM _objEmployeeLeaveBalanceM = Helper.ToCopyClassProperties<EmployeeLeaveBalanceRequestParameterCommon, EmployeeLeaveBalanceM>(_objEmployeeLeaveBalanceRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.SaveUpdateEmployeeLeaveBalanceDetails(_objEmployeeLeaveBalanceM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateEmployeeLeaveBalanceDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Save Employee Leave Balance Initialize.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceInitializeRequestParam"></param>
        /// To Save Employee Leave Balance Initialize Below Request Parameters is required Such as Leave Year and CreatedBy
        /// <returns>
        /// A JSON-formatted string representing the Employee Leave Balance Initialized Response.
        /// </returns>
        [HttpPost, ActionName("SaveEmployeeLeaveBalanceInitialize")]
        public async Task<ActionResult<string>> SaveEmployeeLeaveBalanceInitialize(EmployeeLeaveBalanceInitializeRequestParam _objEmployeeLeaveBalanceInitializeRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.SaveEmployeeLeaveBalanceInitialize(_objEmployeeLeaveBalanceInitializeRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveEmployeeLeaveBalanceInitialize()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Leave Balance Details.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceDetailsRequestParam"></param>
        /// To Get Employee Leave Balance Details Below Request Parameters is required Such as Id, Employee Code and Leave Year.
        /// <returns>
        /// A JSON-formatted string representing the Employee Leave Balance Details.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeLeaveBalanceDetails")]
        public async Task<ActionResult<string>> GetEmployeeLeaveBalanceDetails(EmployeeLeaveBalanceDetailsRequestParam _objEmployeeLeaveBalanceDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.GetEmployeeLeaveBalanceDetailOrList(_objEmployeeLeaveBalanceDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeLeaveBalanceDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Leave Balance Based on Leave Type, Employee Code and Leave Year.
        /// </summary>
        /// <param name="_objCurrentLeaveBalanceDetailsRequestParam"></param>
        /// To Get Leave Balance Based on Leave Type, Employee Code and Leave Year Below Request Parameters is required Such as LeaveTypeID, Employee Code and Leave Year.
        /// <returns>
        /// A JSON-formatted string representing the Available Leave Balance.
        /// </returns>
        [HttpPost, ActionName("GetCurrentYearLeaveBalanceBasedOnLeaveType")]
        public async Task<ActionResult<string>> GetCurrentYearLeaveBalanceBasedOnLeaveType(CurrentLeaveBalanceDetailsRequestParam _objCurrentLeaveBalanceDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _IDropdownService.GetLeaveBalanceBasedOnLeaveTypeAndEmployeeCode(_objCurrentLeaveBalanceDetailsRequestParam.LeaveTypeID, _objCurrentLeaveBalanceDetailsRequestParam.EmployeeCode, _objCurrentLeaveBalanceDetailsRequestParam.LeaveYear, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetCurrentYearLeaveBalanceBasedOnLeaveType()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Leave Balance List.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceDetailsRequestParam"></param>
        /// To Get Employee Leave Balance List Below Request Parameters is required Such as Id and Value must be 0 and Leave Year.
        /// <returns>
        /// A JSON-formatted string representing The Employee Leave Balance List.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeLeaveBalanceList")]
        public async Task<ActionResult<string>> GetEmployeeLeaveBalanceList(EmployeeLeaveBalanceDetailsRequestParam _objEmployeeLeaveBalanceDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objEmployeeLeaveBalanceDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.GetEmployeeLeaveBalanceDetailOrList(_objEmployeeLeaveBalanceDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeLeaveBalanceList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Employee Leave Balance]

        #region [Leave Application]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Leave Request Details.
        /// </summary>
        /// <param name="_objLeaveApplicationRequestParameterCommon"></param>
        /// To Save and Update Leave Request Details Below Request Parameters is required Such as Id, EmployeeCode, LeaveTypeID,
        /// FromDate, ToDate, IsHalfDay, Reason, IsDeleted, CreatedBy and UpdatedBy.
        /// <returns>
        /// A JSON-formatted string representing the Leave Request Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateLeaveRequestDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateLeaveRequestDetails(LeaveApplicationRequestParameterCommon _objLeaveApplicationRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    LeaveApplicationM _objLeaveApplicationM = Helper.ToCopyClassProperties<LeaveApplicationRequestParameterCommon, LeaveApplicationM>(_objLeaveApplicationRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.SaveUpdateLeaveRequestDetails(_objLeaveApplicationM, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateLeaveRequestDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Leave Request Details.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// To Get Leave Request Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Leave Request Details.
        /// </returns>
        [HttpPost, ActionName("GetLeaveRequestDetails")]
        public async Task<ActionResult<string>> GetLeaveRequestDetails(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.GetLeaveRequestDetailOrList(_objLeaveApplicationDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetLeaveRequestDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Leave Request List.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// To Get Leave Request List Below Request Parameters is required Such as Id and EmployeeCode.
        /// Id Value Must Be 0.
        /// <returns>
        /// A JSON-formatted string representing the Leave Request List.
        /// </returns>
        [HttpPost, ActionName("GetLeaveRequestList")]
        public async Task<ActionResult<string>> GetLeaveRequestList(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objLeaveApplicationDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.GetLeaveRequestDetailOrList(_objLeaveApplicationDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetLeaveRequestList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Pending Leave Request List.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// To Get Pending Leave Request List Below Request Parameters is required Such as Id and EmployeeCode.
        /// Id Value Must Be 0.
        /// <returns>
        /// A JSON-formatted string representing the Pending Leave Request List.
        /// </returns>
        [HttpPost, ActionName("GetPendingLeaveRequestList")]
        public async Task<ActionResult<string>> GetPendingLeaveRequestList(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objLeaveApplicationDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.GetPendingLeaveRequestList(_objLeaveApplicationDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetPendingLeaveRequestList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Check For Leave Request Approval Status.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// To Check For Leave Request Approval Status List Below Request Parameters is required Such as Id and EmployeeCode.
        /// <returns>
        /// A JSON-formatted string representing the Leave Request Approval Status.
        /// </returns>
        [HttpPost, ActionName("CheckLeaveRequestApprovalStatus")]
        public async Task<ActionResult<string>> CheckLeaveRequestApprovalStatus(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _LeaveManagementService.CheckLeaveRequestApprovalStatus(_objLeaveApplicationDetailsRequestParam, RequestCancellationToken));
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.CheckLeaveRequestApprovalStatus()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Approve, Reject and Cancel Leave Request.
        /// </summary>
        /// <param name="_objLeaveApplicationApproveRejectCancelRequestParam">
        /// To Approve, Reject and Cancel Leave Request Below mentioned Request Parameters Such as Id, EmployeeCode, ActionType, ActionBy and Remarks.<br/><br/>
        /// <b>Approver Flow :</b><br/>
        /// 1. Parameters to Approve Leave Request by Approver.<br/>
        /// - Id                 - Enter The Selected Leave Request ID.<br/>
        /// - EmployeeCode       - Enter The Selected Employee Code.<br/>
        /// - ActionType         - Enter The Action Type as APPROVE.<br/>
        /// - ActionBy           - Enter The UserID of The Approver.<br/>
        /// - Remarks            - Enter The Approve Reason.<br/><br/>
        /// 2. Parameters to Reject Leave Request by Approver.<br/>
        /// - Id                 - Enter The Selected Leave Request ID.<br/>
        /// - EmployeeCode       - Enter The Selected Employee Code.<br/>
        /// - ActionType         - Enter The Action Type as REJECT.<br/>
        /// - ActionBy           - Enter The UserID of The Approver.<br/>
        /// - Remarks            - Enter The Rejection Reason.<br/><br/>
        /// <b>Cancel Flow :</b><br/>
        /// 1. Parameters to Cancel Leave Request by Employee After Leave Request is Approved.<br/>
        /// - Id                 - Enter The Selected Leave Request ID.<br/>
        /// - EmployeeCode       - Enter The Selected Employee Code.<br/>
        /// - ActionType         - Enter The Action Type as CANCEL.<br/>
        /// - ActionBy           - Enter The UserID of The Employee Who Has Requested for the Leave.<br/>
        /// - Remarks            - Enter The Cancellation Reason.<br/><br/>
        /// </param>
        /// <returns>
        /// A JSON-formatted string representing the Approve Or Reject Or Cancel Leave Request Response.
        /// </returns>
        [HttpPost, ActionName("ApproveOrRejectOrCancelLeaveRequest")]
        public async Task<ActionResult<string>> ApproveOrRejectOrCancelLeaveRequest(LeaveApplicationApproveRejectCancelRequestParam _objLeaveApplicationApproveRejectCancelRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = _objLeaveApplicationApproveRejectCancelRequestParam.ActionType switch
                    {
                        "APPROVE" => ObjectToJson.ConvertDataSetToJson(
                            await _LeaveManagementService.ApproveLeaveRequest(_objLeaveApplicationApproveRejectCancelRequestParam, RequestCancellationToken)),

                        "REJECT" => ObjectToJson.ConvertDataSetToJson(
                            await _LeaveManagementService.RejectLeaveRequest(_objLeaveApplicationApproveRejectCancelRequestParam, RequestCancellationToken)),

                        "CANCEL" => ObjectToJson.ConvertDataSetToJson(
                           await _LeaveManagementService.CancelLeaveRequest(_objLeaveApplicationApproveRejectCancelRequestParam, RequestCancellationToken)),

                        _ => ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(new List<string> { "Status", "Message" },
                        new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "Invalid Action Type. Allowed values are APPROVE, REJECT, CANCEL." }
                        },
                        "ValidationErrors"
                        )
                        )
                    };
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
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.ApproveOrRejectOrCancelLeaveRequest()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Leave Application]
    }
}
