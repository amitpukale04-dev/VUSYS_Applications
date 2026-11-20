using System.Data;
using System.Globalization;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Configuration;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Users;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

public sealed class AILeaveManagementAgentService(IGeminiAIService ai, IAIAPIConfigurationService AIAPIConfigurationService, ILeaveManagementService leave, IAILeaveSecurityContext security, IAILeaveTypeResolver types, IAILeaveConfirmationStore store, IAILeaveClarificationStore clarificationStore, AILeaveDynamicReportService reportService, IAILeaveReportTokenStore reportTokenStore, IUserHelper UserHelper, IWebHostEnvironment webHostEnvironment) : IAILeaveManagementAgentService
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
        if (request is null) return Result(ErrorStatus, "Enter The Message.");
        // A report token bypasses Gemini extraction but never bypasses authenticated authorization.
        if (!string.IsNullOrWhiteSpace(request.ReportToken))
        {
            AILeaveActor reportActor = await security.GetActorAsync(ct);
            if (!int.TryParse(UserHelper.GetUserRoleID(), out int reportRoleID) || reportRoleID != 1)
                return Result(ErrorStatus, "Access denied. AI Leave Reporting is available only to Admin users.");

            if (!reportTokenStore.TryGet(request.ReportToken, out AILeavePendingReport? pendingReport) || pendingReport is null)
                return Result(ErrorStatus, "Invalid or expired report token. Generate the report again.");

            if (pendingReport.UserId != reportActor.UserId ||
                !string.Equals(pendingReport.ActorEmployeeCode, reportActor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                return Result(ErrorStatus, "Access denied. Report token belongs to a different user.");

            string tokenAction = request.Action?.Trim() ?? "View";
            if (tokenAction.Equals("Excel", StringComparison.OrdinalIgnoreCase) || tokenAction.Equals("PDF", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return await reportService.ExportReportAsync(pendingReport.Definition, pendingReport.EmployeeCode,
                        pendingReport.AllowAllEmployees, tokenAction, webHostEnvironment, ct);
                }
                catch (ArgumentException ex)
                {
                    return Result(ErrorStatus, ex.Message);
                }
            }
            if (!tokenAction.Equals("View", StringComparison.OrdinalIgnoreCase))
                return Result(ErrorStatus, "Invalid report action. Supported actions: View, Excel and PDF.");

            try
            {
                DataSet tokenReport = await reportService.GenerateReportAsync(pendingReport.Definition,
                    pendingReport.EmployeeCode, pendingReport.AllowAllEmployees, ct);
                DataSet tokenResult = Result(SuccessStatus,
                    tokenReport.Tables.Count > 0 && tokenReport.Tables[0].Rows.Count > 0
                        ? "Leave report generated successfully." : "No matching records found for the requested report.");
                tokenResult.Tables[0].Columns.Add("ReportToken", typeof(string));
                tokenResult.Tables[0].Rows[0]["ReportToken"] = request.ReportToken;
                if (tokenReport.Tables.Count > 0) tokenResult.Tables.Add(tokenReport.Tables[0].Copy());
                return tokenResult;
            }
            catch (ArgumentException ex)
            {
                return Result(ErrorStatus, ex.Message);
            }
        }
        if (string.IsNullOrWhiteSpace(request.Message)) return Result(ErrorStatus, "Enter The Message.");
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

            if (pendingClarification.Action.NumericReferenceType == "Ambiguous")
            {
                string? referenceType = ParseNumericReferenceChoice(request.Message);
                if (referenceType is null)
                {
                    string retryToken = clarificationStore.Add(pendingClarification with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) });
                    return Result(SuccessStatus, "Does the number refer to a Leave Request ID or a Leave Year? Reply 'Request ID' or 'Leave Year'.",
                        action: pendingClarification.Action, clarificationToken: retryToken);
                }
                action = pendingClarification.Action;
                int number = action.LeaveRequestID ?? action.LeaveYear ?? 0;
                if (number <= 0) return Result(ErrorStatus, "The numeric reference could not be resolved.", action: action);
                if (referenceType == "LeaveYear")
                {
                    action.LeaveYear = number;
                    action.LeaveRequestID = null;
                    action.DateSearchMode = "Overlap";
                }
                else
                {
                    action.LeaveRequestID = number;
                    action.LeaveYear = null;
                }
                action.NumericReferenceType = referenceType;
            }
            else
            {
                string? halfDayType = ParseHalfDayType(request.Message);
                if (halfDayType is null)
                {
                    string retryToken = clarificationStore.Add(pendingClarification with
                    {
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
                    });
                    return Result(SuccessStatus, "Please choose First Half (Morning) or Second Half (Afternoon).", action: pendingClarification.Action, clarificationToken: retryToken);
                }
                action = pendingClarification.Action;
                action.HalfDayType = halfDayType;
            }
        }
        else
        {
            IReadOnlyCollection<AILeaveTypeOptionM> availableTypesForExtraction = await types.GetAvailableLeaveTypesAsync(ct);
            action = await ai.ExtractLeaveActionAsync(request.Message, DateOnly.FromDateTime(DateTime.Now), availableTypesForExtraction, AIAPIConfigurationService, ct);
        }
        IReadOnlyCollection<AILeaveTypeOptionM> availableLeaveTypes = await types.GetAvailableLeaveTypesAsync(ct);
        string intent = action.Intent?.Trim() ?? "Unknown";
        // Resolve and authorize employee scope BEFORE any read-only data access.
        string? targetCode = null;
        string? targetName = null;
        bool allEmployees = false;
        // AI reporting is restricted to authenticated Admin users only.
        if (intent == "GenerateLeaveReport" && Convert.ToInt32(UserHelper.GetUserRoleID()) != 1)
            return Result(ErrorStatus, "Access denied. AI Leave Reporting is available only to Admin users.", action: action);
        bool readOnlyIntent = intent is "GetLeaveBalance" or "GetLeaveStatus" or "GetLeaveHistory" or "GetPendingLeaves" or "GetLeaveDetails" or "GenerateLeaveReport";
        if (readOnlyIntent)
        {
            string scope = action.EmployeeScope?.Trim() ?? "Self";
            string suppliedCode = action.EmployeeCode?.Trim() ?? string.Empty;
            string suppliedName = action.EmployeeName?.Trim() ?? string.Empty;
            if (scope is not ("Self" or "SpecificEmployee" or "AllEmployees"))
                return Result(ErrorStatus, "Invalid employee scope.", action: action);
            // Never allow an explicit target to be silently downgraded to Self.
            if (scope == "Self" && (suppliedCode.Length > 0 || suppliedName.Length > 0))
                scope = "SpecificEmployee";
            bool isAdmin = intent == "GenerateLeaveReport" ? true : await security.CanApproveAsync(actor, ct);
            if (scope == "AllEmployees")
            {
                if (!isAdmin) return Result(ErrorStatus, "Access denied. You are not authorized to view all employees' leave information.", action: action);
                allEmployees = true;
            }
            else if (scope == "SpecificEmployee")
            {
                if (suppliedCode.Length == 0 && suppliedName.Length == 0)
                    return Result(ErrorStatus, "Please specify an employee name or code.", action: action);
                // Employee-code comparisons are performed before looking up another user's identity.
                if (!isAdmin && suppliedCode.Length > 0 && !string.Equals(suppliedCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
                    return Result(ErrorStatus, "Access denied. You are not authorized to view another employee's leave information.", action: action);
                var matches = await security.FindEmployeesForAIAsync(suppliedCode, suppliedName, ct);
                if (!isAdmin) matches = matches.Where(x => string.Equals(x.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 0) return Result(ErrorStatus, isAdmin ? "Employee not found." : "Access denied or employee not found.", action: action);
                if (matches.Count > 1) return Result(ErrorStatus, "Multiple employees match that name. Please provide the exact employee code.", action: action);
                targetCode = matches[0].EmployeeCode;
                targetName = matches[0].EmployeeName;
            }
            else targetCode = actor.EmployeeCode;
        }
        // A bare four-digit number after "leave request" can be either an ID or a year.
        // Preserve authenticated employee scope and all original filters through a one-time clarification token.
        // Explicit "ID", "number", "year", or "in 2026" phrasing must not be treated as ambiguous.
        if (readOnlyIntent && intent != "GetLeaveBalance" &&
            string.IsNullOrWhiteSpace(request.ClarificationToken) &&
            (action.NumericReferenceType == "Ambiguous" || IsAmbiguousLeaveReference(request.Message)))
        {
            int? number = ExtractAmbiguousLeaveReference(request.Message) ?? action.LeaveRequestID;
            if (number is > 0)
            {
                action.LeaveRequestID = number;
                action.LeaveYear = null;
                action.NumericReferenceType = "Ambiguous";
                string clarificationToken = clarificationStore.Add(new AILeavePendingClarification(
                    actor.UserId, actor.EmployeeCode, action, DateTimeOffset.UtcNow.AddMinutes(5)));
                return Result(SuccessStatus,
                    $"Does {number} refer to a Leave Request ID or a Leave Year? Reply 'Request ID' or 'Leave Year'.",
                    action: action, clarificationToken: clarificationToken);
            }
        }

        // Read-only operations never issue confirmation tokens or execute leave actions.
        if (intent == "GenerateLeaveReport")
        {
            if (Convert.ToInt32(UserHelper.GetUserRoleID()) != 1)
                return Result(ErrorStatus, "Access denied. AI Leave Reporting is available only to Admin users.", action: action);

            if (action.Report is null)
                return Result(ErrorStatus, "AI did not provide a report definition.", action: action);

            try
            {
                string ReportAction = request.Action?.Trim() ?? "View";

                if (ReportAction.Equals("Excel", StringComparison.OrdinalIgnoreCase) || ReportAction.Equals("PDF", StringComparison.OrdinalIgnoreCase))
                {
                    string ExportType = ReportAction.Equals("Excel", StringComparison.OrdinalIgnoreCase) ? "Excel" : "PDF";
                    return await reportService.ExportReportAsync(action.Report, targetCode ?? actor.EmployeeCode, allEmployees, ExportType, webHostEnvironment, ct);
                }

                if (!ReportAction.Equals("View", StringComparison.OrdinalIgnoreCase))
                    return Result(ErrorStatus, "Invalid report action. Supported actions: View, Excel and PDF.", action: action);

                DataSet report = await reportService.GenerateReportAsync(action.Report, targetCode ?? actor.EmployeeCode, allEmployees, ct);

                bool hasRecords = report.Tables.Count > 0 && report.Tables[0].Rows.Count > 0;

                DataSet result = Result(
                    SuccessStatus,
                    hasRecords ? "Leave report generated successfully." : "No matching records found for the requested report.",
                    action: action);

                if (report.Tables.Count > 0)
                    result.Tables.Add(report.Tables[0].Copy());

                // Save only the validated report definition and authorized scope; do not store report data.
                string reportToken = reportTokenStore.Add(new AILeavePendingReport(actor.UserId,
                    actor.EmployeeCode, targetCode ?? actor.EmployeeCode, allEmployees,
                    action.Report, DateTimeOffset.UtcNow.AddMinutes(15)));
                result.Tables[0].Columns.Add("ReportToken", typeof(string));
                result.Tables[0].Rows[0]["ReportToken"] = reportToken;
                return result;
            }
            catch (ArgumentException ex)
            {
                return Result(ErrorStatus, ex.Message, action: action);
            }
        }
        if (intent == "GetLeaveBalance")
        {
            int year = action.LeaveYear ?? DateTime.Now.Year;
            if (year < 2000 || year > 9999)
                return Result(ErrorStatus, "Provide a valid leave year.", action: action);
            DataSet balances = allEmployees
                ? await security.GetAllEmployeeLeaveBalancesAsync(year, ct)
                : await security.GetMyLeaveBalancesAsync(targetCode!, year, ct);
            DataTable? source = balances.Tables.Cast<DataTable>().FirstOrDefault(t => t.Columns.Contains("AvailableBalance") && t.Columns.Contains("LeaveType"));
            if (source is null || source.Rows.Count == 0)
                return Result(ErrorStatus, $"No leave balance found for {year}.", action: action);
            DataSet result = Result(SuccessStatus, $"Leave balance for {year}.", action: action);
            DataTable balanceTable = source.Clone();
            balanceTable.TableName = "AI_LEAVE_BALANCE_RESPONSE";
            static string NormalizeLeaveName(string? name)
            {
                string normalized = new string((name ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                return normalized.Replace("privileage", "privilege").Replace("priviledge", "privilege");
            }
            string requestedLeaveType = NormalizeLeaveName(action.LeaveType);
            foreach (DataRow row in source.Rows)
            {
                string actualLeaveType = NormalizeLeaveName(Convert.ToString(row["LeaveType"], CultureInfo.InvariantCulture));
                if (requestedLeaveType.Length == 0 || actualLeaveType == requestedLeaveType ||
                    actualLeaveType.Contains(requestedLeaveType, StringComparison.Ordinal) ||
                    requestedLeaveType.Contains(actualLeaveType, StringComparison.Ordinal))
                    balanceTable.ImportRow(row);
            }
            if (balanceTable.Rows.Count == 0)
                return Result(ErrorStatus, $"No balance found for leave type '{action.LeaveType}' in {year}.", action: action);
            result.Tables.Add(balanceTable);
            return result;
        }
        if (intent is "GetLeaveStatus" or "GetLeaveHistory" or "GetPendingLeaves" or "GetLeaveDetails")
        {
            // Restrict these new queries to the authenticated employee, even when an ID is supplied.
            // Do not trust an employee name or code returned by Gemini.
            bool byId = action.LeaveRequestID is > 0;
            string? fromDate = action.FromDate;
            string? toDate = action.ToDate;
            // No date means search the employee's recent applications across years.
            // When only a year is provided, search that entire year.
            if (!byId && string.IsNullOrWhiteSpace(fromDate) && string.IsNullOrWhiteSpace(toDate) && action.LeaveYear.HasValue)
            {
                int year = action.LeaveYear.Value;
                if (year < 2000 || year > 9999)
                    return Result(ErrorStatus, "Provide a valid leave year.", action: action);
                fromDate = new DateOnly(year, 1, 1).ToString(DateTimeFormatConstants.YYYYMMDD_DATEFORMAT, CultureInfo.InvariantCulture);
                toDate = new DateOnly(year, 12, 31).ToString(DateTimeFormatConstants.YYYYMMDD_DATEFORMAT, CultureInfo.InvariantCulture);
            }
            if (!byId && (string.IsNullOrWhiteSpace(fromDate) != string.IsNullOrWhiteSpace(toDate)))
                return Result(ErrorStatus, "Please provide both start and end dates.", action: action);
            if (!byId && !string.IsNullOrWhiteSpace(fromDate) && !ValidDates(fromDate, toDate))
                return Result(ErrorStatus, "Provide valid leave dates.", action: action);
            var criteria = new AILeaveRequestSearchCriteriaM
            {
                LeaveRequestID = action.LeaveRequestID,
                EmployeeCode = allEmployees ? null : targetCode,
                AllowAllEmployees = allEmployees,
                FromDate = byId ? null : fromDate,
                ToDate = byId ? null : toDate,
                ExactDateRange = byId || (action.DateSearchMode == "Exact" && (intent is "GetLeaveStatus" or "GetLeaveDetails") && !action.LeaveYear.HasValue),
                Intent = intent
            };
            IReadOnlyList<AILeaveRequestDetailsM> records = await security.SearchLeaveRequestsAsync(criteria, ct);
            var ownRecords = records.Where(x => allEmployees || string.Equals(x.EmployeeCode, targetCode, StringComparison.OrdinalIgnoreCase));
            if (intent == "GetPendingLeaves") ownRecords = ownRecords.Where(x => x.LeaveStatus == (int)LeaveStatusEnum.Pending);
            var details = ownRecords.ToList();
            if (details.Count == 0)
                return Result(ErrorStatus, "No matching leave requests found.", action: action);
            string message = intent == "GetLeaveStatus" && details.Count > 1
                ? $"Found {details.Count} leave applications. Select the request whose status you want to check."
                : $"Found {details.Count} leave request(s).";
            return ResultWithDetails(SuccessStatus, message, details, action: action);
        }
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
                string clarificationToken = clarificationStore.Add(new AILeavePendingClarification(actor.UserId, actor.EmployeeCode, action, DateTimeOffset.UtcNow.AddMinutes(5)));
                return Result(SuccessStatus, $"You requested half-day leave on {from:dd MMMM yyyy}. Would you like First Half (Morning) or Second Half (Afternoon)?", action: action, clarificationToken: clarificationToken);
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
                return ResultWithDetails(ErrorStatus, "Review the leave details and provide remarks before confirming this action.", new[] { target }, action: action);

            string token = store.Add(new AILeavePendingAction(actor.UserId, actor.EmployeeCode, action, null,
                target.EmployeeCode, DateTimeOffset.UtcNow.AddMinutes(5)));
            return ResultWithDetails(SuccessStatus, $"Review the leave details before confirming {intent} for request {target.LeaveRequestID}.", new[] { target }, token, action);
        }
        return Result(ErrorStatus, "Supported actions: apply, approve, reject, cancel, leave status, leave balance, leave history and pending leaves.", action: action);
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
    /// <param name="status"></param>
    /// <param name="message"></param>
    /// <param name="details"></param>
    /// <param name="token"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    private static DataSet ResultWithDetails(string status, string message, IEnumerable<AILeaveRequestDetailsM> details, string token = "", AILeaveManagementActionM? action = null)
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
    private static string? ParseNumericReferenceChoice(string message)
    {
        string value = message.Trim().ToLowerInvariant();
        bool isId = value is "id" or "request id" or "leave request id" or "request number" or "leave request number" or "1";
        bool isYear = value is "year" or "leave year" or "calendar year" or "2";
        if (isId == isYear) return null;
        return isId ? "LeaveRequestID" : "LeaveYear";
    }

    /// <summary>
    /// This Method Is Used To Extract Ambiguous Leave Reference.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static int? ExtractAmbiguousLeaveReference(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(message,
            @"\bleave\s+requests?\s+(20\d{2})\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out int value) ? value : null;
    }

    /// <summary>
    /// This Method Is Used To Check Ambiguous Leave Reference.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static bool IsAmbiguousLeaveReference(string message)
    {
        if (ExtractAmbiguousLeaveReference(message) is null) return false;
        return !System.Text.RegularExpressions.Regex.IsMatch(message,
            @"\b(request\s*(?:id|number|no\.?|#)|id\s*[:#]|(?:in|during|year|for\s+the\s+year)\s+20\d{2})\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// This Method Is Used To Parse Half Day Type.
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
