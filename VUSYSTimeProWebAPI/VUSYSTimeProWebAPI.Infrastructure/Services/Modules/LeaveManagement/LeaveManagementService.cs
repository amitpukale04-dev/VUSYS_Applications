using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.LeaveManagement
{
    public sealed class LeaveManagementService : ILeaveManagementService
    {
        private const string LeaveManagementServiceLogClassName = "LeaveManagementService";
        private readonly SQLDBHelper _objSQLDBHelper;
        private readonly IDropdownService _DropdownSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Leave Management Service Class Constructor
        /// </summary>
        /// <param name="SQLDBHelper"></param>
        /// <param name="DropdownSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LeaveManagementService(SQLDBHelper SQLDBHelper, IDropdownService DropdownSvc, IEmailService EmailSvc, 
            IWebHostEnvironment webHostEnvironment)
        {
            _objSQLDBHelper = SQLDBHelper ?? throw new ArgumentNullException(nameof(SQLDBHelper));
            _DropdownSvc = DropdownSvc ?? throw new ArgumentNullException(nameof(DropdownSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        #region [Employee Leave Balance]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Leave Balance Details.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateEmployeeLeaveBalanceDetails(EmployeeLeaveBalanceM _objEmployeeLeaveBalanceM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "EMPLOYEE_LEAVE_BALANCE_DETAILS_RESPONSE";
            try
            {
                _objEmployeeLeaveBalanceM.Status = OperationTypeConstants.Add;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objEmployeeLeaveBalanceM, StoredProcedure.EMPLOYEE_LEAVE_BALANCE_SAVE_UPDATE, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.SaveUpdateEmployeeLeaveBalanceDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Leave Balance Details Or List.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetEmployeeLeaveBalanceDetailOrList(EmployeeLeaveBalanceDetailsRequestParam _objEmployeeLeaveBalanceDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                TableName = _objEmployeeLeaveBalanceDetailsRequestParam.Id switch
                {
                    0 => "EMPLOYEE_LEAVE_BALANCE_LIST_RESPONSE",
                    _ => "EMPLOYEE_LEAVE_BALANCE_DETAILS_RESPONSE"
                };
                ParamType = _objEmployeeLeaveBalanceDetailsRequestParam.Id switch
                {
                    0 => "GetAllEmployeeLeaveBalanceList",
                    _ => "GetEmployeeLeaveBalanceDetails"
                };
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objEmployeeLeaveBalanceDetailsRequestParam.Id },
                    { "@EmployeeCode", _objEmployeeLeaveBalanceDetailsRequestParam.EmployeeCode },
                    { "@LeaveYear", _objEmployeeLeaveBalanceDetailsRequestParam.LeaveYear },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_LEAVE_BALANCE_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.GetEmployeeLeaveBalanceDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Save Employee Leave Balance Initialize.
        /// </summary>
        /// <param name="_objEmployeeLeaveBalanceInitializeRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveEmployeeLeaveBalanceInitialize(EmployeeLeaveBalanceInitializeRequestParam _objEmployeeLeaveBalanceInitializeRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            List<string> columns = new List<string> { "ID", "Status", "Message" };
            string TableName = "EMPLOYEE_LEAVE_BALANCE_INITIALIZE_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@LeaveYear", _objEmployeeLeaveBalanceInitializeRequestParam.LeaveYear },
                    { "@CreatedBy", _objEmployeeLeaveBalanceInitializeRequestParam.CreatedBy },
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_LEAVE_BALANCE_INITIALIZE, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.SaveEmployeeLeaveBalanceInitialize()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Employee Leave Balance]

        #region [Leave Application]

        /// <summary>
        /// This Method Is Used To Save Or Update Leave Request Details.
        /// </summary>
        /// <param name="_objLeaveApplicationM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUpdateLeaveRequestDetails(LeaveApplicationM _objLeaveApplicationM, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "LEAVE_REQUEST_RESPONSE";
            List<string> columns = new List<string> { "Status", "Message" };
            string EmailSentResponseReturnResult = "An Email Notification Has Been Sent to the Employee !!!";
            string EmailSentFailedResponseReturnResult = "We Encountered An Issue Sending An Email Notification To The Employee !!!";
            string ApproverNotAvailable = "You Cannot Submit The Leave Request. Since Approver Is Not Available In the System. Please Contact Administrator !!!";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                GetTotalNoofLeaveDaysRequestParam _objGetTotalNoofLeaveDaysRequestParam = Helper.ToCopyClassProperties<LeaveApplicationM, GetTotalNoofLeaveDaysRequestParam>(_objLeaveApplicationM);
                var dsTotalNoofLeaveDays = await GetTotalNoofLeaveDays(_objGetTotalNoofLeaveDaysRequestParam, cancellationToken);
                if (Helper.CheckDataSetIsNullOrEmpty(dsTotalNoofLeaveDays))
                {
                    string DBStatus = Helper.ToProperCase(dsTotalNoofLeaveDays.Tables[0].Rows[0]["Status"].ToString());
                    string DBMessage = dsTotalNoofLeaveDays.Tables[0].Rows[0]["Message"].ToString();
                    decimal CurrentLeaveBalance = Convert.ToDecimal(dsTotalNoofLeaveDays.Tables[0].Rows[0]["CurrentLeaveBalance"].ToString());
                    decimal TotalNoofLeaveDays = Convert.ToDecimal(dsTotalNoofLeaveDays.Tables[0].Rows[0]["TotalNoofLeaveDays"].ToString());
                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                    {
                        _objLeaveApplicationM.TotalLeaveDays = TotalNoofLeaveDays;
                        bool IsNewMode = _objLeaveApplicationM.Id == 0 ? true : false;
                        _objLeaveApplicationM.Status = OperationTypeConstants.Add;
                        if (IsNewMode)
                        {
                            var ApproverUserDetails = await _DropdownSvc.GetApproverDetails(cancellationToken);
                            var UserDetails = await _DropdownSvc.GetUserDetailsByEmployeeCode(_objLeaveApplicationM.EmployeeCode, cancellationToken);
                            if (ApproverUserDetails != null && ApproverUserDetails.Count > 0)
                            {
                                var GenderName = await _DropdownSvc.GetGenderNameAsync(ApproverUserDetails[0].EmployeeCode, cancellationToken);
                                string Title = string.Empty;
                                if (GenderName == "MALE")
                                {
                                    Title = "Sir";
                                }
                                var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objLeaveApplicationM, StoredProcedure.LEAVE_APPLICATION_SAVE_UPDATE, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                                cancellationToken.ThrowIfCancellationRequested();
                                if (Helper.CheckDataSetIsNullOrEmpty(ds))
                                {
                                    DBStatus = string.Empty;
                                    DBMessage = string.Empty;
                                    DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                                    DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                                    string LeaveApplicationID = Convert.ToString(ds.Tables[0].Rows[0]["Id"].ToString());
                                    string LeaveType = ds.Tables[0].Rows[0]["LeaveType"].ToString();
                                    if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                                    {
                                        string LinkURL = string.Empty;
                                        LinkURL = CommonHelper.KeyOption.FrontEndBaseURL + "/leave-approval-rejection?ID=" + Helper.EncodeStringToBase64(LeaveApplicationID) + "&EC=" + Helper.EncodeStringToBase64(_objLeaveApplicationM.EmployeeCode) + "&EN=" + Helper.EncodeStringToBase64(UserDetails[0].FullName);
                                        
                                        string FormattedFromDate = DateTimeHelper.ConvertDateTimeStringToSpecificFormat(_objLeaveApplicationM.FromDate, DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT);
                                        string FormattedToDate = DateTimeHelper.ConvertDateTimeStringToSpecificFormat(_objLeaveApplicationM.ToDate, DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT);

                                        string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "LeaveApplicationPendingEmailTemplate.html");
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverName#", ApproverUserDetails[0].FullName);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Title#", Title);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", UserDetails[0].FullName);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#LeaveType#", LeaveType);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#FromDate#", FormattedFromDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ToDate#", FormattedToDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TotalLeaveDays#", _objLeaveApplicationM.TotalLeaveDays.ToString());
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Reason#", _objLeaveApplicationM.Reason);

                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#URLLink#", LinkURL);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeEmailID#", UserDetails[0].EmailID);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                                        string MailSubject = "Leave Approval Request from " + UserDetails[0].FullName + " – " + FormattedFromDate + " to " + FormattedToDate;
                                        string MailBody = EmailBodyTemplate;
                                        string MailTo = ApproverUserDetails[0].EmailID + (string.IsNullOrWhiteSpace(CommonHelper.KeyOption.SaanviTechToEmailID) ? "" : "," + CommonHelper.KeyOption.SaanviTechToEmailID);
                                        string MailCC = CommonHelper.KeyOption.SaanviTechCCEmailID;
                                        string ToEmailIDs = string.Join(",", new[] { MailTo }.Where(x => !string.IsNullOrWhiteSpace(x)));
                                        string CCEmailIDs = string.Join(",", new[] { MailCC }.Where(x => !string.IsNullOrWhiteSpace(x)));
                                        
                                        var sendReq = CommonHelper.CreateSendEmailRequestParam("Leave Request Notification", ToEmailIDs, CCEmailIDs, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                                        var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                                        string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                                        JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                                        string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                                        if (resultjObjectEmail == "ok")
                                        {
                                            var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                                            if (emailResponse?.Message == Helper.EmailSuccess)
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, and an Email Notification Has Been Sent to the Approver !!!";

                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                            else
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, However, We Encountered An Issue Sending The Approval Request Email To The Approver. Please Contact Your Administrator For Assistance !!!";
                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                        }
                                        else
                                        {
                                            List<List<string>> ErrorRows = new List<List<string>>
                                            {
                                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                            };
                                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                        }
                                    }
                                    else
                                    {
                                        List<List<string>> ErrorRows = new List<List<string>>
                                        {
                                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), DBMessage }
                                        };
                                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                    }
                                }
                                else
                                {
                                    List<List<string>> ErrorRows = new List<List<string>>
                                    {
                                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                }
                            }
                            else
                            {
                                List<List<string>> ErrorRows = new List<List<string>>
                                {
                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ApproverNotAvailable }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                            }
                        }
                        else
                        {
                            finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objLeaveApplicationM, StoredProcedure.LEAVE_APPLICATION_SAVE_UPDATE, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                            cancellationToken.ThrowIfCancellationRequested();
                            finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), DBMessage }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "Unable To Apply The Leave Request !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.SaveUpdateLeaveRequestDetails()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Total No of Leave Days.
        /// </summary>
        /// <param name="_objGetTotalNoofLeaveDaysRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetTotalNoofLeaveDays(GetTotalNoofLeaveDaysRequestParam _objGetTotalNoofLeaveDaysRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "TOTAL_NO_OF_LEAVE_DAYS_RESPONSE";
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@EmployeeCode", _objGetTotalNoofLeaveDaysRequestParam.EmployeeCode },
                    { "@LeaveTypeID", _objGetTotalNoofLeaveDaysRequestParam.LeaveTypeID },
                    { "@FromDate", _objGetTotalNoofLeaveDaysRequestParam.FromDate },
                    { "@ToDate", _objGetTotalNoofLeaveDaysRequestParam.ToDate },
                    { "@IsHalfDay", _objGetTotalNoofLeaveDaysRequestParam.IsHalfDay },
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.GET_TOTAL_NO_OF_LEAVE_DAYS, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.GetTotalNoofLeaveDays()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Leave Request Details Or List.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetLeaveRequestDetailOrList(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = string.Empty;
            string ParamType = string.Empty;
            try
            {
                if (_objLeaveApplicationDetailsRequestParam.Id > 0 && string.IsNullOrWhiteSpace(_objLeaveApplicationDetailsRequestParam.EmployeeCode))
                {
                    TableName = "LEAVE_REQUEST_DETAILS_RESPONSE";
                    ParamType = "GetLeaveRequestDetails";
                }
                else if (_objLeaveApplicationDetailsRequestParam.Id == 0 && !string.IsNullOrWhiteSpace(_objLeaveApplicationDetailsRequestParam.EmployeeCode))
                {
                    TableName = "MY_LEAVE_REQUEST_LIST_RESPONSE";
                    ParamType = "GetLeaveRequestList";
                }
                else if (_objLeaveApplicationDetailsRequestParam.Id > 0 && !string.IsNullOrWhiteSpace(_objLeaveApplicationDetailsRequestParam.EmployeeCode))
                {
                    TableName = "LEAVE_REQUEST_DETAILS_RESPONSE";
                    ParamType = "GetApprovedOrRejectedOrCancelledLeaveRequestDetails";
                }
                else
                {
                    TableName = "LEAVE_REQUEST_LIST_RESPONSE";
                    ParamType = "GetAllLeaveRequestList";
                }
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objLeaveApplicationDetailsRequestParam.Id },
                    { "@EmployeeCode", _objLeaveApplicationDetailsRequestParam.EmployeeCode ?? string.Empty },
                    { "@Type", ParamType }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.LEAVE_APPLICATION_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.GetLeaveRequestDetailOrList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Pending Leave Request List.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetPendingLeaveRequestList(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", 0 },
                    { "@EmployeeCode", _objLeaveApplicationDetailsRequestParam.EmployeeCode ?? string.Empty },
                    { "@Type", "GetAllPendingLeaveRequestList" }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.LEAVE_APPLICATION_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, "PENDING_LEAVE_REQUEST_LIST_RESPONSE");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.GetPendingLeaveRequestList()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Check For Leave Request Approval Status.
        /// </summary>
        /// <param name="_objLeaveApplicationDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> CheckLeaveRequestApprovalStatus(LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", _objLeaveApplicationDetailsRequestParam.Id },
                    { "@EmployeeCode", _objLeaveApplicationDetailsRequestParam.EmployeeCode ?? string.Empty },
                    { "@Type", "CheckLeaveApprovalFromFE" }
                };
                finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.LEAVE_APPLICATION_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                cancellationToken.ThrowIfCancellationRequested();
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, "LEAVE_REQUEST_APPROVAL_STATUS_RESPONSE");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.CheckLeaveRequestApprovalStatus()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Approve Leave Request.
        /// </summary>
        /// <param name="_objLeaveApplicationApproveRejectCancelRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> ApproveLeaveRequest(LeaveApplicationApproveRejectCancelRequestParam _objLeaveApplicationApproveRejectCancelRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "LEAVE_REQUEST_APPROVE_REJECT_CANCEL_RESPONSE";
            List<string> columns = new List<string> { "Status", "Message" };
            string EmailSentResponseReturnResult = "An Email Notification Has Been Sent to the User !!!";
            string EmailSentFailedResponseReturnResult = "We Encountered An Issue Sending An Email Notification To The User. Please Contact Your Administrator For Assistance !!!";
            string ApproverNotAvailable = "You Cannot Approve/Reject The Leave Request. Since Approver Is Not Available In the System. Please Contact Administrator.";
            string UserNotAvailable = "You Cannot Approve/Reject The Leave Request. Since User Is Not Available In the System. Please Contact Administrator.";
            try
            {
                var ApproverUserDetails = await _DropdownSvc.GetApproverDetails(cancellationToken);
                if (ApproverUserDetails != null && ApproverUserDetails.Count > 0)
                {
                    var UserDetails = await _DropdownSvc.GetUserDetailsByEmployeeCode(_objLeaveApplicationApproveRejectCancelRequestParam.EmployeeCode, cancellationToken);
                    if (UserDetails != null && UserDetails.Count > 0)
                    {
                        LeaveApplicationM _objLeaveApplicationM = Helper.ToCopyClassProperties<LeaveApplicationApproveRejectCancelRequestParam, LeaveApplicationM>(_objLeaveApplicationApproveRejectCancelRequestParam);
                        _objLeaveApplicationM.ApproverEmployeeCode = ApproverUserDetails[0].EmployeeCode;
                        _objLeaveApplicationM.ActionType = "APPROVE";
                        cancellationToken.ThrowIfCancellationRequested();
                        var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objLeaveApplicationM, StoredProcedure.LEAVE_APPLICATION_APPROVE_REJECT_CANCEL, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                        cancellationToken.ThrowIfCancellationRequested();
                        if (Helper.CheckDataSetIsNullOrEmpty(ds))
                        {
                            string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                            string DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                            if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                            {
                                string LeaveApplicationID = Convert.ToString(ds.Tables[0].Rows[0]["Id"].ToString());
                                LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam = Helper.ToCopyClassProperties<LeaveApplicationM, LeaveApplicationDetailsRequestParam>(_objLeaveApplicationM);
                                _objLeaveApplicationDetailsRequestParam.Id = Convert.ToInt32(LeaveApplicationID);
                                var dsApprovedLeaveDetails = await GetLeaveRequestDetailOrList(_objLeaveApplicationDetailsRequestParam, CancellationToken.None);
                                if(Helper.CheckDataSetIsNullOrEmpty(dsApprovedLeaveDetails))
                                {
                                    string ApprovedLeaveDetailsDBStatus = Helper.ToProperCase(dsApprovedLeaveDetails.Tables[0].Rows[0]["Status"].ToString());
                                    if (ApprovedLeaveDetailsDBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                                    {
                                        string EmployeeCode = dsApprovedLeaveDetails.Tables[0].Rows[0]["EmployeeCode"].ToString();
                                        string EmployeeName = dsApprovedLeaveDetails.Tables[0].Rows[0]["EmployeeName"].ToString();
                                        string LeaveType = dsApprovedLeaveDetails.Tables[0].Rows[0]["LeaveType"].ToString();
                                        string FromDate = dsApprovedLeaveDetails.Tables[0].Rows[0]["FromDate"].ToString();
                                        string ToDate = dsApprovedLeaveDetails.Tables[0].Rows[0]["ToDate"].ToString();
                                        string TotalLeaveDays = dsApprovedLeaveDetails.Tables[0].Rows[0]["TotalLeaveDays"].ToString();
                                        string Reason = dsApprovedLeaveDetails.Tables[0].Rows[0]["Reason"].ToString();
                                        string ActionBy = dsApprovedLeaveDetails.Tables[0].Rows[0]["ActionBy"].ToString();
                                        string ActionByEmailID = dsApprovedLeaveDetails.Tables[0].Rows[0]["ActionByEmailID"].ToString();
                                        string ActionDate = dsApprovedLeaveDetails.Tables[0].Rows[0]["ActionDate"].ToString();
                                        string ActionRemarks = dsApprovedLeaveDetails.Tables[0].Rows[0]["ActionRemarks"].ToString();

                                        string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "LeaveApplicationApprovedEmailTemplate.html");
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", EmployeeName);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverName#", ActionBy);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#LeaveType#", LeaveType);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#FromDate#", FromDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ToDate#", ToDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TotalLeaveDays#", TotalLeaveDays);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Reason#", Reason);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApprovedDate#", ActionDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApprovedRemarks#", ActionRemarks);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverEmailID#", ActionByEmailID);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                                        string MailSubject = $"Leave Request Approved – {EmployeeName} ({FromDate} to {ToDate})";
                                        string MailBody = EmailBodyTemplate;
                                        string MailCC = string.Join(",", new[] { CommonHelper.KeyOption.SaanviTechToEmailID, CommonHelper.KeyOption.SaanviTechCCEmailID }.Where(email => !string.IsNullOrWhiteSpace(email)));
                                        string CCEmailIDs = string.Join(",", new[] { MailCC }.Where(x => !string.IsNullOrWhiteSpace(x)));

                                        var sendReq = CommonHelper.CreateSendEmailRequestParam("Leave Request Approved Notification", UserDetails[0].EmailID, CCEmailIDs, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                                        var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                                        string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                                        JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                                        string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                                        if (resultjObjectEmail == "ok")
                                        {
                                            var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                                            if (emailResponse?.Message == Helper.EmailSuccess)
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, and an Email Notification Has Been Sent to the User !!!";
                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                            else
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, However, {EmailSentFailedResponseReturnResult}";
                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                        }
                                        else
                                        {
                                            List<List<string>> ErrorRows = new List<List<string>>
                                            {
                                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                            };
                                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                        }
                                    }
                                }
                                else
                                {
                                    List<List<string>> ErrorRows = new List<List<string>>
                                    {
                                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                }
                            }
                            else
                            {
                                List<List<string>> ErrorRows = new List<List<string>>
                                {
                                    new List<string> { DBStatus, DBMessage }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                            }
                        }
                        else
                        {
                            List<List<string>> ErrorRows = new List<List<string>>
                            {
                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), UserNotAvailable }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ApproverNotAvailable }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.ApproveRejectCancelLeaveApplication()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Reject Leave Request.
        /// </summary>
        /// <param name="_objLeaveApplicationApproveRejectCancelRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> RejectLeaveRequest(LeaveApplicationApproveRejectCancelRequestParam _objLeaveApplicationApproveRejectCancelRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "LEAVE_REQUEST_APPROVE_REJECT_CANCEL_RESPONSE";
            List<string> columns = new List<string> { "Status", "Message" };
            string EmailSentResponseReturnResult = "An Email Notification Has Been Sent to the User !!!";
            string EmailSentFailedResponseReturnResult = "We Encountered An Issue Sending An Email Notification To The User. Please Contact Your Administrator For Assistance !!!";
            string ApproverNotAvailable = "You Cannot Approve/Reject The Leave Request. Since Approver Is Not Available In the System. Please Contact Administrator.";
            string UserNotAvailable = "You Cannot Approve/Reject The Leave Request. Since User Is Not Available In the System. Please Contact Administrator.";
            try
            {
                var ApproverUserDetails = await _DropdownSvc.GetApproverDetails(cancellationToken);
                if (ApproverUserDetails != null && ApproverUserDetails.Count > 0)
                {
                    var UserDetails = await _DropdownSvc.GetUserDetailsByEmployeeCode(_objLeaveApplicationApproveRejectCancelRequestParam.EmployeeCode, cancellationToken);
                    if (UserDetails != null && UserDetails.Count > 0)
                    {
                        LeaveApplicationM _objLeaveApplicationM = Helper.ToCopyClassProperties<LeaveApplicationApproveRejectCancelRequestParam, LeaveApplicationM>(_objLeaveApplicationApproveRejectCancelRequestParam);
                        _objLeaveApplicationM.ApproverEmployeeCode = ApproverUserDetails[0].EmployeeCode;
                        _objLeaveApplicationM.ActionType = "REJECT";
                        cancellationToken.ThrowIfCancellationRequested();
                        var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objLeaveApplicationM, StoredProcedure.LEAVE_APPLICATION_APPROVE_REJECT_CANCEL, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                        cancellationToken.ThrowIfCancellationRequested();
                        if (Helper.CheckDataSetIsNullOrEmpty(ds))
                        {
                            string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                            string DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                            if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                            {
                                string LeaveApplicationID = Convert.ToString(ds.Tables[0].Rows[0]["Id"].ToString());
                                LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam = Helper.ToCopyClassProperties<LeaveApplicationM, LeaveApplicationDetailsRequestParam>(_objLeaveApplicationM);
                                _objLeaveApplicationDetailsRequestParam.Id = Convert.ToInt32(LeaveApplicationID);
                                var dsRejectdLeaveDetails = await GetLeaveRequestDetailOrList(_objLeaveApplicationDetailsRequestParam, CancellationToken.None);
                                if (Helper.CheckDataSetIsNullOrEmpty(dsRejectdLeaveDetails))
                                {
                                    string RejectdLeaveDetailsDBStatus = Helper.ToProperCase(dsRejectdLeaveDetails.Tables[0].Rows[0]["Status"].ToString());
                                    if (RejectdLeaveDetailsDBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                                    {
                                        string EmployeeCode = dsRejectdLeaveDetails.Tables[0].Rows[0]["EmployeeCode"].ToString();
                                        string EmployeeName = dsRejectdLeaveDetails.Tables[0].Rows[0]["EmployeeName"].ToString();
                                        string LeaveType = dsRejectdLeaveDetails.Tables[0].Rows[0]["LeaveType"].ToString();
                                        string FromDate = dsRejectdLeaveDetails.Tables[0].Rows[0]["FromDate"].ToString();
                                        string ToDate = dsRejectdLeaveDetails.Tables[0].Rows[0]["ToDate"].ToString();
                                        string TotalLeaveDays = dsRejectdLeaveDetails.Tables[0].Rows[0]["TotalLeaveDays"].ToString();
                                        string Reason = dsRejectdLeaveDetails.Tables[0].Rows[0]["Reason"].ToString();
                                        string ActionBy = dsRejectdLeaveDetails.Tables[0].Rows[0]["ActionBy"].ToString();
                                        string ActionByEmailID = dsRejectdLeaveDetails.Tables[0].Rows[0]["ActionByEmailID"].ToString();
                                        string ActionDate = dsRejectdLeaveDetails.Tables[0].Rows[0]["ActionDate"].ToString();
                                        string ActionRemarks = dsRejectdLeaveDetails.Tables[0].Rows[0]["ActionRemarks"].ToString();

                                        string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "LeaveApplicationRejectedEmailTemplate.html");
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", EmployeeName);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverName#", ActionBy);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#LeaveType#", LeaveType);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#FromDate#", FromDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ToDate#", ToDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TotalLeaveDays#", TotalLeaveDays);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Reason#", Reason);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#RejectedDate#", ActionDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#RejectedRemarks#", ActionRemarks);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverEmailID#", ActionByEmailID);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                                        string MailSubject = $"Leave Request Rejected – {EmployeeName} ({FromDate} to {ToDate})";
                                        string MailBody = EmailBodyTemplate;
                                        string MailCC = string.Join(",", new[] { CommonHelper.KeyOption.SaanviTechToEmailID, CommonHelper.KeyOption.SaanviTechCCEmailID }.Where(email => !string.IsNullOrWhiteSpace(email)));
                                        string CCEmailIDs = string.Join(",", new[] { MailCC }.Where(x => !string.IsNullOrWhiteSpace(x)));
                                        
                                        var sendReq = CommonHelper.CreateSendEmailRequestParam("Leave Request Rejected Notification", UserDetails[0].EmailID, CCEmailIDs, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                                        var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                                        string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                                        JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                                        string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                                        if (resultjObjectEmail == "ok")
                                        {
                                            var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                                            if (emailResponse?.Message == Helper.EmailSuccess)
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, and an Email Notification Has Been Sent to the User !!!";
                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                            else
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, However, {EmailSentFailedResponseReturnResult}";
                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                        }
                                        else
                                        {
                                            List<List<string>> ErrorRows = new List<List<string>>
                                            {
                                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                            };
                                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                        }
                                    }
                                }
                                else
                                {
                                    List<List<string>> ErrorRows = new List<List<string>>
                                    {
                                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                }
                            }
                            else
                            {
                                List<List<string>> ErrorRows = new List<List<string>>
                                {
                                    new List<string> { DBStatus, DBMessage }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                            }
                        }
                        else
                        {
                            List<List<string>> ErrorRows = new List<List<string>>
                            {
                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), UserNotAvailable }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ApproverNotAvailable }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.RejectRejectCancelLeaveApplication()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Cancel Leave Request.
        /// </summary>
        /// <param name="_objLeaveApplicationApproveRejectCancelRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> CancelLeaveRequest(LeaveApplicationApproveRejectCancelRequestParam _objLeaveApplicationApproveRejectCancelRequestParam, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "LEAVE_REQUEST_APPROVE_REJECT_CANCEL_RESPONSE";
            List<string> columns = new List<string> { "Status", "Message" };
            string EmailSentResponseReturnResult = "An Email Notification Has Been Sent to the User !!!";
            string EmailSentFailedResponseReturnResult = "We Encountered An Issue Sending An Email Notification To The User. Please Contact Your Administrator For Assistance !!!";
            string ApproverNotAvailable = "You Cannot Approve/Cancel The Leave Request. Since Approver Is Not Available In the System. Please Contact Administrator.";
            string UserNotAvailable = "You Cannot Approve/Cancel The Leave Request. Since User Is Not Available In the System. Please Contact Administrator.";
            try
            {
                int LeaveRequestStatus = await _DropdownSvc.CheckLeaveRequestStatusAsync(_objLeaveApplicationApproveRejectCancelRequestParam.Id, cancellationToken);
                bool IsLeaveRequestPending = LeaveRequestStatus == (int)LeaveStatusEnum.Pending;
                string HTMLTemplate = string.Empty;
                string MailSubject = string.Empty;
                string EmailFromDisplayName = string.Empty;
                if (IsLeaveRequestPending)
                {
                    HTMLTemplate = "LeaveApplicationCancelledBeforeApprovalEmailTemplate.html";
                }
                else
                {
                    HTMLTemplate = "LeaveApplicationCancelledEmailTemplate.html";
                }
                var ApproverUserDetails = await _DropdownSvc.GetApproverDetails(cancellationToken);
                if (ApproverUserDetails != null && ApproverUserDetails.Count > 0)
                {
                    var UserDetails = await _DropdownSvc.GetUserDetailsByEmployeeCode(_objLeaveApplicationApproveRejectCancelRequestParam.EmployeeCode, cancellationToken);
                    if (UserDetails != null && UserDetails.Count > 0)
                    {
                        var GenderName = await _DropdownSvc.GetGenderNameAsync(ApproverUserDetails[0].EmployeeCode, cancellationToken);
                        string Title = string.Empty;
                        if (GenderName == "MALE")
                        {
                            Title = "Sir";
                        }
                        LeaveApplicationM _objLeaveApplicationM = Helper.ToCopyClassProperties<LeaveApplicationApproveRejectCancelRequestParam, LeaveApplicationM>(_objLeaveApplicationApproveRejectCancelRequestParam);
                        _objLeaveApplicationM.ApproverEmployeeCode = ApproverUserDetails[0].EmployeeCode;
                        _objLeaveApplicationM.ActionType = "CANCEL";
                        cancellationToken.ThrowIfCancellationRequested();
                        var ds = await _objSQLDBHelper.ExecuteStoredProcedureWithXMLParametersAsync(_objLeaveApplicationM, StoredProcedure.LEAVE_APPLICATION_APPROVE_REJECT_CANCEL, Helper.XML_PARAMS, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
                        cancellationToken.ThrowIfCancellationRequested();
                        if (Helper.CheckDataSetIsNullOrEmpty(ds))
                        {
                            string DBStatus = Helper.ToProperCase(ds.Tables[0].Rows[0]["Status"].ToString());
                            string DBMessage = ds.Tables[0].Rows[0]["Message"].ToString();
                            if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                            {
                                string LeaveApplicationID = Convert.ToString(ds.Tables[0].Rows[0]["Id"].ToString());
                                LeaveApplicationDetailsRequestParam _objLeaveApplicationDetailsRequestParam = Helper.ToCopyClassProperties<LeaveApplicationM, LeaveApplicationDetailsRequestParam>(_objLeaveApplicationM);
                                _objLeaveApplicationDetailsRequestParam.Id = Convert.ToInt32(LeaveApplicationID);
                                var dsCancelledLeaveDetails = await GetLeaveRequestDetailOrList(_objLeaveApplicationDetailsRequestParam, CancellationToken.None);
                                if (Helper.CheckDataSetIsNullOrEmpty(dsCancelledLeaveDetails))
                                {
                                    string CanceldLeaveDetailsDBStatus = Helper.ToProperCase(dsCancelledLeaveDetails.Tables[0].Rows[0]["Status"].ToString());
                                    if (CanceldLeaveDetailsDBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                                    {
                                        string EmployeeCode = dsCancelledLeaveDetails.Tables[0].Rows[0]["EmployeeCode"].ToString();
                                        string EmployeeName = dsCancelledLeaveDetails.Tables[0].Rows[0]["EmployeeName"].ToString();
                                        string LeaveType = dsCancelledLeaveDetails.Tables[0].Rows[0]["LeaveType"].ToString();
                                        string FromDate = dsCancelledLeaveDetails.Tables[0].Rows[0]["FromDate"].ToString();
                                        string ToDate = dsCancelledLeaveDetails.Tables[0].Rows[0]["ToDate"].ToString();
                                        string TotalLeaveDays = dsCancelledLeaveDetails.Tables[0].Rows[0]["TotalLeaveDays"].ToString();
                                        string Reason = dsCancelledLeaveDetails.Tables[0].Rows[0]["Reason"].ToString();
                                        string ActionBy = dsCancelledLeaveDetails.Tables[0].Rows[0]["ActionBy"].ToString();
                                        if(string.IsNullOrEmpty(ActionBy))
                                        {
                                            ActionBy = ApproverUserDetails[0].FullName;
                                        }
                                        string ActionByEmailID = dsCancelledLeaveDetails.Tables[0].Rows[0]["ActionByEmailID"].ToString();
                                        if (string.IsNullOrEmpty(ActionByEmailID))
                                        {
                                            ActionByEmailID = ApproverUserDetails[0].EmailID;
                                        }
                                        string ActionDate = dsCancelledLeaveDetails.Tables[0].Rows[0]["ActionDate"].ToString();
                                        if (string.IsNullOrEmpty(ActionDate))
                                        {
                                            ActionDate = DateTimeHelper.ConvertDateTimeStringToSpecificFormat(DateTime.Now.ToString(), DateTimeFormatConstants.DDMMMYYYY_DATEFORMAT);
                                        }
                                        string ActionRemarks = dsCancelledLeaveDetails.Tables[0].Rows[0]["ActionRemarks"].ToString();
                                        string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, HTMLTemplate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeName#", EmployeeName);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ApproverName#", ActionBy);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Title#", Title);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#LeaveType#", LeaveType);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#FromDate#", FromDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#ToDate#", ToDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#TotalLeaveDays#", TotalLeaveDays);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#Reason#", Reason);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CancelledDate#", ActionDate);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CancelledRemarks#", _objLeaveApplicationApproveRejectCancelRequestParam.Remarks);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#EmployeeEmailID#", UserDetails[0].EmailID);
                                        EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                                        MailSubject = (IsLeaveRequestPending ? $"Leave Cancelled by Employee – {EmployeeName} ({FromDate} to {ToDate})" : $"Approved Leave Cancelled by Employee – {EmployeeName} ({FromDate} to {ToDate})");
                                        string MailBody = EmailBodyTemplate;
                                        string MailTo = ApproverUserDetails[0].EmailID + (string.IsNullOrWhiteSpace(CommonHelper.KeyOption.SaanviTechToEmailID) ? "" : "," + CommonHelper.KeyOption.SaanviTechToEmailID);
                                        string MailCC = CommonHelper.KeyOption.SaanviTechCCEmailID;
                                        string ToEmailIDs = string.Join(",", new[] { MailTo }.Where(x => !string.IsNullOrWhiteSpace(x)));
                                        string CCEmailIDs = string.Join(",", new[] { MailCC }.Where(x => !string.IsNullOrWhiteSpace(x)));
                                        string CustomFromDisplayName = IsLeaveRequestPending ? "Leave Cancelled Notification" : "Approved Leave Cancelled Notification";
                                        
                                        var sendReq = CommonHelper.CreateSendEmailRequestParam(CustomFromDisplayName, ToEmailIDs, CCEmailIDs, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                                        var EmailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                                        string ReturnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                                        JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(ReturnResult));
                                        string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                                        if (resultjObjectEmail == "ok")
                                        {
                                            var emailResponse = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>()?.FirstOrDefault();
                                            if (emailResponse?.Message == Helper.EmailSuccess)
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, and an Email Notification Has Been Sent to the Approver !!!";
                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                            else
                                            {
                                                DBMessage = DBMessage.Replace(".", "");
                                                EmailSentResponseReturnResult = $"{DBMessage}, However, {EmailSentFailedResponseReturnResult}";
                                                List<List<string>> rows = new List<List<string>>
                                                {
                                                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                                                };
                                                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                                                return finalDataSet;
                                            }
                                        }
                                        else
                                        {
                                            List<List<string>> ErrorRows = new List<List<string>>
                                            {
                                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                            };
                                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                        }
                                    }
                                }
                                else
                                {
                                    List<List<string>> ErrorRows = new List<List<string>>
                                    {
                                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                                    };
                                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                                }
                            }
                            else
                            {
                                List<List<string>> ErrorRows = new List<List<string>>
                                {
                                    new List<string> { DBStatus, DBMessage }
                                };
                                finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                            }
                        }
                        else
                        {
                            List<List<string>> ErrorRows = new List<List<string>>
                            {
                                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), EmailSentFailedResponseReturnResult }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                        }
                    }
                    else
                    {
                        List<List<string>> ErrorRows = new List<List<string>>
                        {
                            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), UserNotAvailable }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                    }
                }
                else
                {
                    List<List<string>> ErrorRows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), ApproverNotAvailable }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, ErrorRows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureLeaveManagementLogNamespace, $"{LeaveManagementServiceLogClassName}.CancelCancelCancelLeaveApplication()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [Leave Application]
    }
}
