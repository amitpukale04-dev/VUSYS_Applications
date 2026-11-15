using System.Data;
using System.Globalization;
using CommonLibCore10;
using CommonLibCore10.Constants;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

public sealed class AILeaveManagementAgentService(IGeminiAIService ai, ILeaveManagementService leave, IAILeaveSecurityContext security, IAILeaveTypeResolver types, IAILeaveConfirmationStore store, IAILeaveClarificationStore clarificationStore) : IAILeaveManagementAgentService
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
        AILeaveManagementActionM action;
        if (!string.IsNullOrWhiteSpace(request.ClarificationToken))
        {
            // Consume once: replayed clarification tokens cannot be used to apply leave.
            if (!clarificationStore.TryConsume(request.ClarificationToken, out var pendingClarification) || pendingClarification is null)
                return Result(ErrorStatus, "Invalid, expired or already used clarification token.");
            if (pendingClarification.UserId != actor.UserId ||
                !string.Equals(pendingClarification.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                return Result(ErrorStatus, "Clarification belongs to a different user.");

            string? halfDayType = ParseHalfDayType(request.Message);
            if (halfDayType is null)
            {
                string retryToken = clarificationStore.Add(pendingClarification with
                {
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
                });
                return Result(SuccessStatus,
                    "Please choose First Half (Morning) or Second Half (Afternoon).",
                    action: pendingClarification.Action, clarificationToken: retryToken);
            }
            action = pendingClarification.Action;
            action.HalfDayType = halfDayType;
        }
        else
        {
            IReadOnlyCollection<AILeaveTypeOptionM> availableTypesForExtraction = await types.GetAvailableLeaveTypesAsync(ct);
            action = await ai.ExtractLeaveActionAsync(request.Message, DateOnly.FromDateTime(DateTime.Now), availableTypesForExtraction, ct);
        }
        IReadOnlyCollection<AILeaveTypeOptionM> availableLeaveTypes = await types.GetAvailableLeaveTypesAsync(ct);
        string intent = action.Intent?.Trim() ?? "Unknown";
        if (intent == "ApplyLeave")
        {
            if (string.IsNullOrWhiteSpace(action.LeaveType) || string.IsNullOrWhiteSpace(action.FromDate) ||
                string.IsNullOrWhiteSpace(action.ToDate) || string.IsNullOrWhiteSpace(action.Reason))
                return Result(ErrorStatus, "Provide leave type, from date, to date and reason.", action: action);
            if (!DateOnly.TryParseExact(action.FromDate, DateTimeFormatConstants.YYYYMMDD_DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ||
                !DateOnly.TryParseExact(action.ToDate, DateTimeFormatConstants.YYYYMMDD_DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to) || to < from)
                return Result(ErrorStatus, "Invalid leave date range.", action: action);
            if (action.IsHalfDay && (from != to))
                return Result(ErrorStatus, "Half-day leave must be for a single date.", action: action);
            if (action.IsHalfDay && string.IsNullOrWhiteSpace(action.HalfDayType))
            {
                string clarificationToken = clarificationStore.Add(new AILeavePendingClarification(
                    actor.UserId, actor.EmployeeCode, action, DateTimeOffset.UtcNow.AddMinutes(5)));
                return Result(SuccessStatus,
                    $"You requested half-day leave on {from:dd MMMM yyyy}. Would you like First Half (Morning) or Second Half (Afternoon)?",
                    action: action, clarificationToken: clarificationToken);
            }
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
            bool cancel = intent == "CancelLeave";
            if (!cancel && !await security.CanApproveAsync(actor, ct))
                return Result(ErrorStatus, "Leave approval permission denied.", action: action);

            if (action.LeaveRequestID is not > 0 &&
                (string.IsNullOrWhiteSpace(action.FromDate) || string.IsNullOrWhiteSpace(action.ToDate)))
                return Result(ErrorStatus, "Provide a leave request ID or the leave from and to dates.", action: action);

            if (action.LeaveRequestID is not > 0 ||
                !string.IsNullOrWhiteSpace(action.FromDate) && !string.IsNullOrWhiteSpace(action.ToDate))
            {
                if (!ValidDates(action.FromDate, action.ToDate))
                    return Result(ErrorStatus, "Provide a valid from and to date (yyyy-MM-dd).", action: action);
            }
            if (!cancel && action.LeaveRequestID is not > 0 && string.IsNullOrWhiteSpace(action.EmployeeName))
                return Result(ErrorStatus, "Provide the employee name or leave request ID.", action: action);

            string dateSearchMode = action.DateSearchMode?.Trim() ?? "Exact";
            if (dateSearchMode is not ("Exact" or "Overlap"))
                return Result(ErrorStatus, "Invalid leave date search mode.", action: action);

            // Explicit request IDs always identify a single request; the period is not a filter.
            bool searchByID = action.LeaveRequestID is > 0;
            var criteria = new AILeaveRequestSearchCriteriaM
            {
                LeaveRequestID = action.LeaveRequestID,
                EmployeeCode = cancel ? actor.EmployeeCode : null,
                EmployeeName = cancel ? null : action.EmployeeName,
                FromDate = searchByID ? null : action.FromDate,
                ToDate = searchByID ? null : action.ToDate,
                ExactDateRange = searchByID || dateSearchMode == "Exact",
                Intent = intent
            };
            IReadOnlyList<AILeaveRequestDetailsM> matches = await security.SearchLeaveRequestsAsync(criteria, ct);

            // Defense in depth: eligibility is filtered by SQL before TOP (51), and checked again here.
            List<AILeaveRequestDetailsM> eligibleMatches = matches
                .Where(x => cancel
                    ? x.LeaveStatus is (int)LeaveStatusEnum.Pending or (int)LeaveStatusEnum.Approved
                    : x.LeaveStatus == (int)LeaveStatusEnum.Pending)
                .GroupBy(x => x.LeaveRequestID)
                .Select(g => g.First())
                .ToList();

            if (eligibleMatches.Count == 0)
                return Result(ErrorStatus, cancel
                    ? "No pending or approved leave request was found for the specified dates."
                    : "No pending leave request was found for the specified employee and dates.", action: action);

            // Never silently select the newest request when multiple eligible records exist.
            if (eligibleMatches.Count > 1)
                return ResultWithDetails(ErrorStatus,
                    "Multiple eligible leave requests match. Select the correct LeaveRequestID and send the request again.",
                    eligibleMatches, action: action);

            AILeaveRequestDetailsM target = eligibleMatches[0];
            if (cancel)
            {
                if (!string.Equals(target.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                    return Result(ErrorStatus, "You can only cancel your own leave request.", action: action);
                if (target.LeaveStatus != (int)LeaveStatusEnum.Pending && target.LeaveStatus != (int)LeaveStatusEnum.Approved)
                    return Result(ErrorStatus, "This leave cannot be cancelled.", action: action);
            }
            else
            {
                if (target.LeaveStatus != (int)LeaveStatusEnum.Pending)
                    return Result(ErrorStatus, "Only pending leave requests can be approved or rejected.", action: action);
                if (string.Equals(target.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                    return Result(ErrorStatus, "Self-approval is not allowed.", action: action);
            }

            // Bind the resolved ID and dates to the confirmation token.
            action.LeaveRequestID = target.LeaveRequestID;
            action.EmployeeName = target.EmployeeName;
            action.FromDate = target.FromDate;
            action.ToDate = target.ToDate;
            action.TotalLeaveDays = target.TotalLeaveDays;
            action.LeaveType = target.LeaveType;
            action.Reason = target.Reason;

            // The existing leave-action DTO requires Remarks for all three operations.
            // Never invent remarks on the employee's behalf.
            if (string.IsNullOrWhiteSpace(action.Remarks))
                return ResultWithDetails(ErrorStatus,
                    "Review the leave details and provide remarks before confirming this action.",
                    new[] { target }, action: action);


            string token = store.Add(new AILeavePendingAction(actor.UserId, actor.EmployeeCode, action, null,
                target.EmployeeCode, DateTimeOffset.UtcNow.AddMinutes(5)));
            return ResultWithDetails(SuccessStatus,
                $"Review the leave details before confirming {intent} for request {target.LeaveRequestID}.",
                new[] { target }, token, action);
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
                HalfDayType = action.HalfDayType,
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
        // Revalidate the selected request's date range at confirmation time.
        AILeaveRequestDetailsM? currentDetails = await security.GetLeaveRequestDetailsAsync(target.Id, ct);
        if (currentDetails is null ||
            !string.Equals(currentDetails.FromDate, action.FromDate, StringComparison.Ordinal) ||
            !string.Equals(currentDetails.ToDate, action.ToDate, StringComparison.Ordinal))
            return Result(ErrorStatus, "Leave details changed. Review the request again.");
        if (string.IsNullOrWhiteSpace(action.Remarks))
            return Result(ErrorStatus, "Remarks are required.");
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
    /// This Method Is Used To Validate ISO Leave Dates.
    /// </summary>
    private static bool ValidDates(string? fromDate, string? toDate) =>
        DateOnly.TryParseExact(fromDate, DateTimeFormatConstants.YYYYMMDD_DATEFORMAT,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) &&
        DateOnly.TryParseExact(toDate, DateTimeFormatConstants.YYYYMMDD_DATEFORMAT,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var to) && to >= from;

    /// <summary>
    /// This Method Is Used To Return Leave Details For Review Or Disambiguation.
    /// </summary>
    private static DataSet ResultWithDetails(string status, string message,
        IEnumerable<AILeaveRequestDetailsM> details, string token = "", AILeaveManagementActionM? action = null)
    {
        DataSet ds = Result(status, message, token, action);
        DataTable table = new DataTable("AI_LEAVE_REQUEST_DETAILS_RESPONSE");
        foreach (string name in new[] { "LeaveRequestID", "EmployeeCode", "EmployeeName", "LeaveTypeID",
            "LeaveType", "FromDate", "ToDate", "IsHalfDay", "TotalLeaveDays",
            "Reason", "LeaveStatus", "Remarks" })
            table.Columns.Add(name, typeof(string));
        foreach (AILeaveRequestDetailsM item in details)
            table.Rows.Add(item.LeaveRequestID.ToString(), item.EmployeeCode, item.EmployeeName,
                item.LeaveTypeID.ToString(), item.LeaveType, item.FromDate, item.ToDate,
                item.IsHalfDay.ToString(), item.TotalLeaveDays.ToString(CultureInfo.InvariantCulture),
                item.Reason, item.LeaveStatus.ToString(), item.Remarks);
        ds.Tables.Add(table);
        return ds;
    }

    /// <summary>
    /// This Method Is Used To Get Result.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static string? ParseHalfDayType(string message)
    {
        string value = message.Trim().ToLowerInvariant();
        // Only accept explicit, unambiguous choices. Do not infer the user's preference.
        bool first = value is "first" or "first half" or "morning" or "1" or "1st half" or "first half (morning)";
        bool second = value is "second" or "second half" or "afternoon" or "2" or "2nd half" or "second half (afternoon)";
        if (first == second) return null;
        return first ? "First Half" : "Second Half";
    }

    /// <summary>
    /// This Method Is Used To Result.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <param name="action"></param>
    /// <param name="clarificationToken"></param>
    /// <returns></returns>
    private static DataSet Result(string status, string message, string token = "", AILeaveManagementActionM? action = null, string clarificationToken = "")
    {
        var ds = new DataSet(); var t = new DataTable("AI_LEAVE_MANAGEMENT_RESPONSE");
        foreach (string col in new[] { "Status", "Message", "ConfirmationToken", "Intent", "LeaveType", "FromDate", "ToDate", "LeaveRequestID", "EmployeeName", "Reason", "Remarks", "ClarificationToken", "IsHalfDay", "HalfDayType", "TotalLeaveDays", "LeaveStatus" })
            t.Columns.Add(col, typeof(string));
        t.Rows.Add(status, message, token, action?.Intent ?? "", action?.LeaveType ?? "", action?.FromDate ?? "",
            action?.ToDate ?? "", action?.LeaveRequestID?.ToString() ?? "", action?.EmployeeName ?? "", action?.Reason ?? "", action?.Remarks ?? "", clarificationToken, action?.IsHalfDay.ToString() ?? "False", action?.HalfDayType ?? "", action?.TotalLeaveDays.ToString() ?? "", action?.LeaveStatus.ToString() ?? "");
        ds.Tables.Add(t); return ds;
    }
}
