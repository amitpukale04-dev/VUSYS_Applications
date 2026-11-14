using System.Data;
using System.Globalization;
using CommonLibCore10;
using CommonLibCore10.Constants;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

public sealed class AILeaveManagementAgentService(IGeminiAIService ai, ILeaveManagementService leave, IAILeaveSecurityContext security, IAILeaveTypeResolver types, IAILeaveConfirmationStore store) : IAILeaveManagementAgentService
{
    private static readonly string ErrorStatus = Helper.ToProperCase(MessageTypeConstants.Error);
    private static readonly string SuccessStatus = Helper.ToProperCase(MessageTypeConstants.Success);

    /// <summary>
    /// This Method Is Used To Process AI Leave Management Request.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<DataSet> ProcessAILeaveManagementRequest(AILeaveManagementRequestParam request, CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message)) return Result(ErrorStatus, "Enter The Message.");
        AILeaveActor actor = await security.GetActorAsync(ct);
        IReadOnlyCollection<AILeaveTypeOptionM> availableLeaveTypes = await types.GetAvailableLeaveTypesAsync(ct);
        AILeaveManagementActionM action = await ai.ExtractLeaveActionAsync(request.Message, DateOnly.FromDateTime(DateTime.Now), availableLeaveTypes, ct);
        string intent = action.Intent?.Trim() ?? "Unknown";
        if (intent == "ApplyLeave")
        {
            if (string.IsNullOrWhiteSpace(action.LeaveType) || string.IsNullOrWhiteSpace(action.FromDate) ||
                string.IsNullOrWhiteSpace(action.ToDate) || string.IsNullOrWhiteSpace(action.Reason))
                return Result(ErrorStatus, "Provide leave type, from date, to date and reason.", action: action);
            if (!DateOnly.TryParseExact(action.FromDate, DateTimeFormatConstants.YYYYMMDD_DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ||
                !DateOnly.TryParseExact(action.ToDate, DateTimeFormatConstants.YYYYMMDD_DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to) || to < from)
                return Result(ErrorStatus, "Invalid leave date range.", action: action);
            if (action.IsHalfDay && (from != to || string.IsNullOrWhiteSpace(action.HalfDayType)))
                return Result(ErrorStatus, "Specify one date and the half-day type.", action: action);
            int? leaveType = AILeaveTypeResolver.ResolveLeaveTypeID(action.LeaveType, availableLeaveTypes);
            if (leaveType is null)
            {
                string availableTypes = AILeaveTypeResolver.GetAvailableLeaveTypesMessage(availableLeaveTypes);
                return Result(ErrorStatus, $"The requested leave type '{action.LeaveType}' is not available. Available leave types: {availableTypes}.", action: action);
            }
            DataSet preflight = await leave.GetTotalNoofLeaveDays(new GetTotalNoofLeaveDaysRequestParam
            {
                EmployeeCode = actor.EmployeeCode,
                LeaveTypeID = leaveType.Value,
                FromDate = action.FromDate,
                ToDate = action.ToDate,
                IsHalfDay = action.IsHalfDay
            }, ct);
            if (!Success(preflight)) return Result(ErrorStatus, FirstMessage(preflight, "Leave validation failed."), action: action);
            string token = store.Add(new AILeavePendingAction(actor.UserId, actor.EmployeeCode, action, leaveType, null, DateTimeOffset.UtcNow.AddMinutes(5)));
            return Result(SuccessStatus, "Review and confirm the leave application.", token, action);
        }
        if (intent is "ApproveLeave" or "RejectLeave" or "CancelLeave")
        {
            if (action.LeaveRequestID is null or <= 0) return Result(ErrorStatus, "Provide the leave request ID.", action: action);
            if (string.IsNullOrWhiteSpace(action.Remarks)) return Result(ErrorStatus, "Provide remarks for this action.", action: action);
            var target = await security.GetLeaveRequestAsync(action.LeaveRequestID.Value, ct);
            if (target is null) return Result(ErrorStatus, "Leave request not found.", action: action);
            if (intent == "CancelLeave")
            {
                if (!string.Equals(target.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                    return Result(ErrorStatus, "You can only cancel your own leave request.", action: action);
                if (target.LeaveStatus is not (1 or 2)) return Result(ErrorStatus, "This leave cannot be cancelled.", action: action);
            }
            else
            {
                if (!await security.CanApproveAsync(actor, ct)) return Result(ErrorStatus, "Leave approval permission denied.", action: action);
                if (target.LeaveStatus != 1) return Result(ErrorStatus, "Only pending leave requests can be approved or rejected.", action: action);
                if (string.Equals(target.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                    return Result(ErrorStatus, "Self-approval is not allowed.", action: action);
            }
            string token = store.Add(new AILeavePendingAction(actor.UserId, actor.EmployeeCode, action, null, target.EmployeeCode, DateTimeOffset.UtcNow.AddMinutes(5)));
            return Result(SuccessStatus, $"Review and confirm {intent} for leave request {target.Id} ({target.EmployeeCode}).", token, action);
        }
        return Result(ErrorStatus, "Supported actions: apply, approve, reject and cancel leave.", action: action);
    }

    /// <summary>
    /// This Method Is Used To Confirm AI Leave Management Action.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<DataSet> ConfirmAILeaveManagementAction(AILeaveManagementConfirmRequestParam request, CancellationToken ct = default)
    {
        AILeaveActor actor = await security.GetActorAsync(ct);
        if (request is null || !store.TryConsume(request.ConfirmationToken, out var pending) || pending is null)
            return Result(ErrorStatus, "Invalid, expired or already used confirmation token.");
        if (actor.UserId != pending.UserId || !string.Equals(actor.EmployeeCode, pending.EmployeeCode, StringComparison.OrdinalIgnoreCase))
            return Result(ErrorStatus, "Confirmation belongs to a different user.");
        var action = pending.Action;
        if (action.Intent == "ApplyLeave")
        {
            if (pending.LeaveTypeID is null) return Result(ErrorStatus, "Leave type unavailable.");
            var check = await leave.GetTotalNoofLeaveDays(new GetTotalNoofLeaveDaysRequestParam
            {
                EmployeeCode = actor.EmployeeCode,
                LeaveTypeID = pending.LeaveTypeID.Value,
                FromDate = action.FromDate!,
                ToDate = action.ToDate!,
                IsHalfDay = action.IsHalfDay
            }, ct);
            if (!Success(check)) return Result(ErrorStatus, FirstMessage(check, "Leave validation failed."));
            return await leave.SaveUpdateLeaveRequestDetails(new LeaveApplicationM
            {
                Id = 0,
                EmployeeCode = actor.EmployeeCode,
                LeaveTypeID = pending.LeaveTypeID.Value,
                FromDate = action.FromDate!,
                ToDate = action.ToDate!,
                IsHalfDay = action.IsHalfDay,
                HalfDayType = action.HalfDayType ?? "",
                Reason = action.Reason!,
                CreatedBy = actor.UserId,
                ActionBy = actor.UserId,
                Status = "ADD"
            }, ct);
        }
        if (action.LeaveRequestID is null) return Result(ErrorStatus, "Leave request ID missing.");
        var target = await security.GetLeaveRequestAsync(action.LeaveRequestID.Value, ct);
        if (target is null || !string.Equals(target.EmployeeCode, pending.TargetEmployeeCode, StringComparison.OrdinalIgnoreCase))
            return Result(ErrorStatus, "Leave request changed or no longer exists.");
        bool cancel = action.Intent == "CancelLeave";
        if (cancel)
        {
            if (!string.Equals(target.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase) || target.LeaveStatus is not (1 or 2))
                return Result(ErrorStatus, "Cancellation is no longer allowed.");
        }
        else if (action.Intent is "ApproveLeave" or "RejectLeave")
        {
            if (!await security.CanApproveAsync(actor, ct) || target.LeaveStatus != 1 ||
                string.Equals(target.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                return Result(ErrorStatus, "Approval/rejection is no longer permitted.");
        }
        else return Result(ErrorStatus, "Unsupported action.");
        var param = new LeaveApplicationApproveRejectCancelRequestParam
        {
            Id = target.Id,
            EmployeeCode = target.EmployeeCode,
            ActionBy = actor.UserId,
            Remarks = action.Remarks!,
            ActionType = cancel ? "CANCEL" : action.Intent == "ApproveLeave" ? "APPROVE" : "REJECT"
        };
        if (cancel) return await leave.CancelLeaveRequest(param, ct);
        if (action.Intent == "ApproveLeave") return await leave.ApproveLeaveRequest(param, ct);
        return await leave.RejectLeaveRequest(param, ct);
    }

    /// <summary>
    /// This Method Is Used To Check For Success Or Error.
    /// </summary>
    /// <param name="ds"></param>
    /// <returns></returns>
    private static bool Success(DataSet? ds) => ds is not null && ds.Tables.Cast<DataTable>().Any(t => t.Columns.Contains("Status") &&
        t.Rows.Cast<DataRow>().Any(r => string.Equals(Convert.ToString(r["Status"]), SuccessStatus, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// This Method Is Used To Get First Message.
    /// </summary>
    /// <param name="ds"></param>
    /// <param name="fallback"></param>
    /// <returns></returns>
    private static string FirstMessage(DataSet? ds, string fallback) => ds?.Tables.Cast<DataTable>()
        .Where(t => t.Columns.Contains("Message") && t.Rows.Count > 0)
        .Select(t => Convert.ToString(t.Rows[0]["Message"]) ?? fallback).FirstOrDefault() ?? fallback;

    /// <summary>
    /// This Method Is Used To Get Result.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    private static DataSet Result(string status, string message, string token = "", AILeaveManagementActionM? action = null)
    {
        var ds = new DataSet(); var t = new DataTable("AI_LEAVE_MANAGEMENT_RESPONSE");
        foreach (string col in new[] { "Status", "Message", "ConfirmationToken", "Intent", "LeaveType", "FromDate", "ToDate", "LeaveRequestID", "Reason", "Remarks" })
            t.Columns.Add(col, typeof(string));
        t.Rows.Add(status, message, token, action?.Intent ?? "", action?.LeaveType ?? "", action?.FromDate ?? "",
            action?.ToDate ?? "", action?.LeaveRequestID?.ToString() ?? "", action?.Reason ?? "", action?.Remarks ?? "");
        ds.Tables.Add(t); return ds;
    }
}
