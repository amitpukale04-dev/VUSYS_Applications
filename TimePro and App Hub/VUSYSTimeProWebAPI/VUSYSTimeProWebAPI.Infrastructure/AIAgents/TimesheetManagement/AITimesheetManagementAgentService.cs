using System.Data;
using System.Globalization;
using System.Text.Json;
using CommonLibCore10;
using CommonLibCore10.Constants;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.TimesheetManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Configuration;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Timesheet;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.TimesheetManagement;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.TimesheetManagement;

public sealed class AITimesheetManagementAgentService(
    IGeminiAITimesheetService gemini,
    IAIAPIConfigurationService configuration,
    IAITimesheetSecurityContext security,
    IAITimesheetConfirmationStore confirmations,
    IAITimesheetClarificationStore clarifications,
    ITimesheetService timesheetService,
    IDropdownService dropdownService,
    SQLDBHelper sql) : IAITimesheetManagementAgentService
{
    private const string TableName = "AI_TIMESHEET_MANAGEMENT_RESPONSE";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    /// <summary>This Method Is Used To Process Timesheet Management Request From AI Agent.</summary>
    public async Task<DataSet> ProcessAITimesheetManagementRequest(AITimesheetManagementRequestParam request, CancellationToken cancellationToken = default)
    {
        var actor = await security.GetActorAsync(cancellationToken);
        string message = request.Message;
        if (!string.IsNullOrWhiteSpace(request.ClarificationToken))
        {
            if (!clarifications.TryConsume(request.ClarificationToken, actor, out var pending) || pending is null)
                return Response("Error", "Clarification expired or invalid. Please repeat your request.");
            message = JsonSerializer.Serialize(new { PreviousAction = pending.Action, LatestReply = message });
        }
        // TimePro uses Indian calendar dates; resolve relative periods against IST.
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(330)).DateTime);
        var action = await gemini.ExtractTimesheetActionAsync(message, today, configuration, cancellationToken);
        // Correct only an unambiguous self-status enquiry misclassified as ViewTimesheet.
        // Do not reinterpret clarification replies, mutations or named-employee requests.
        if (string.IsNullOrWhiteSpace(request.ClarificationToken) && action.Intent == "ViewTimesheet" &&
            string.IsNullOrWhiteSpace(action.EmployeeCode) && string.IsNullOrWhiteSpace(action.EmployeeName) &&
            action.EmployeeScope != "SpecificEmployee" && IsExplicitOwnTimesheetStatusRequest(request.Message))
            action = action with { Intent = "CheckTimesheetStatus", EmployeeScope = "Self", Remarks = null };
        if (action.Intent == "CheckTimesheetStatus")
        {
            if (action.EmployeeScope == "SpecificEmployee" || !string.IsNullOrWhiteSpace(action.EmployeeName) ||
                (!string.IsNullOrWhiteSpace(action.EmployeeCode) && !string.Equals(action.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase)))
            {
                if (!actor.IsApprover)
                    return Response("Error", "You can check only your own timesheet status.", action);
                return await ProcessApproverActionAsync(actor, action, cancellationToken);
            }
            return await CheckTimesheetStatusAsync(actor, action, actor.EmployeeCode, cancellationToken);
        }
        if (action.Intent is "ViewTimesheet" or "DownloadTimesheet" or "ApproveTimesheet" or "RejectTimesheet")
            return await ProcessApproverActionAsync(actor, action, cancellationToken);
        if (action.Intent is not ("SubmitTimesheet" or "ResubmitTimesheet"))
            return Response("Error", "Unsupported timesheet action.", action);
        if (action.EmployeeScope == "SpecificEmployee" || !string.IsNullOrWhiteSpace(action.EmployeeName) ||
            (!string.IsNullOrWhiteSpace(action.EmployeeCode) && !string.Equals(action.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase)))
            return Response("Error", "You can submit only your own timesheet.", action);
        if (action.TimesheetMonth is null || action.TimesheetYear is null)
        {
            string token = clarifications.Add(new(actor.UserId, actor.EmployeeCode, action, DateTimeOffset.UtcNow.Add(TokenLifetime)));
            string question = action.TimesheetMonth is null && action.TimesheetYear is null ? "For which month and year?" : action.TimesheetMonth is null ? "For which month?" : "For which year?";
            return Response("ClarificationRequired", question, action, clarification: token);
        }
        var (row, validationMessage) = await GetEligibleTimesheetAsync(actor, action, cancellationToken);
        if (validationMessage != null || row is null)
            return SubmissionValidationResponse(actor, action, row, validationMessage ?? "Timesheet details are unavailable.");
        int id = Convert.ToInt32(row["ProjectTimeSheetID"], CultureInfo.InvariantCulture);
        string confirmation = confirmations.Add(new(actor.UserId, actor.EmployeeCode, action, actor.EmployeeCode, id, DateTimeOffset.UtcNow.Add(TokenLifetime)));
        return Response("ConfirmationRequired", $"Confirm {action.Intent} for {Month(action)} {action.TimesheetYear}?", action, confirmation, employee: actor.EmployeeCode, id: id, hours: Convert.ToString(row["TotalHoursWorked"], CultureInfo.InvariantCulture) ?? "", timesheetStatus: Convert.ToString(row["TimesheetApprovalStatus"]) ?? "");
    }

    /// <summary>This Method Is Used To Confirm Timesheet Management Action From AI Agent.</summary>
    public async Task<DataSet> ConfirmAITimesheetManagementAction(AITimesheetManagementConfirmRequestParam request, CancellationToken cancellationToken = default)
    {
        var actor = await security.GetActorAsync(cancellationToken);
        if (!confirmations.TryConsume(request.ConfirmationToken, actor, out var pending) || pending is null)
            return Response("Error", "Confirmation expired, invalid or already used. Please repeat your request.");
        var action = pending.Action;
        if (action.Intent is "ApproveTimesheet" or "RejectTimesheet")
        {
            if (!actor.IsApprover || string.IsNullOrWhiteSpace(action.Remarks) || action.Remarks.Length > 2000)
                return Response("Error", "Unauthorized action or invalid remarks.", action);
            var approvalRow = await GetSubmittedTimesheetAsync(pending.TargetEmployeeCode, action, cancellationToken);
            if (approvalRow is null || Convert.ToInt32(approvalRow["ProjectTimeSheetID"]) != pending.ProjectTimeSheetID || !IsPendingApproval(approvalRow))
                return Response("Error", "Timesheet status changed. Please repeat your request.", action);
            return await timesheetService.ApproveOrRejectTimesheetAsApprover(new TimesheetApproveRejectRequestParam
            {
                EmployeeCode = pending.TargetEmployeeCode,
                ProjectTimeSheetID = pending.ProjectTimeSheetID,
                TimesheetMonth = Month(action),
                TimesheetYear = action.TimesheetYear!.Value,
                ApproveStatus = action.Intent == "ApproveTimesheet" ? "Approve" : "Reject",
                Remarks = action.Remarks.Trim(),
                UserToken = string.Empty,
                Source = string.Empty
            }, actor.UserId, cancellationToken);
        }
        if (action.Intent is not ("SubmitTimesheet" or "ResubmitTimesheet") || !string.Equals(pending.TargetEmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
            return Response("Error", "Unauthorized timesheet action.", action);
        var (row, validationMessage) = await GetEligibleTimesheetAsync(actor, action, cancellationToken);
        if (validationMessage != null || row is null)
            return SubmissionValidationResponse(actor, action, row, validationMessage ?? "Timesheet details are unavailable.");
        if (Convert.ToInt32(row["ProjectTimeSheetID"], CultureInfo.InvariantCulture) != pending.ProjectTimeSheetID)
            return Response("Error", "Timesheet status changed. Please repeat your request.", action);
        // Preserve existing submission and email notification logic.
        return await timesheetService.SubmitTimesheetDetailsForApproval(new TimesheetSubmitForApprovalRequestParam
        {
            EmployeeCode = actor.EmployeeCode,
            ProjectTimeSheetID = pending.ProjectTimeSheetID,
            TimesheetMonth = Month(action),
            TimesheetYear = action.TimesheetYear!.Value,
            ApproveStatus = action.Intent == "ResubmitTimesheet" ? "Resend For Approval" : "Send For Approval"
        }, cancellationToken);
    }

    /// <summary>This Method Is Used To View Or Download Submitted Employee Timesheet.</summary>
    private async Task<DataSet> ProcessApproverActionAsync(AITimesheetActor actor, AITimesheetManagementActionM action, CancellationToken cancellationToken)
    {
        if (!actor.IsApprover)
        {
            string permissionMessage = action.Intent switch
            {
                "ApproveTimesheet" => "Timesheet approval permission denied.",
                "RejectTimesheet" => "Timesheet reject permission denied.",
                _ => "Only authorized approvers can view, download, approve or reject submitted timesheets."
            };

            return Response("Error", permissionMessage, action);
        }

        if (action.TimesheetMonth is null || action.TimesheetYear is null)
            return Clarify(actor, action, "Please specify the missing month or year of the timesheet.");
        if (action.TimesheetMonth is < 1 or > 12 || action.TimesheetYear is < 2000 or > 9999)
            return Response("Error", "Invalid timesheet period.", action);

        string employeeCode = action.EmployeeCode?.Trim() ?? string.Empty;
        string employeeName = action.EmployeeName?.Trim() ?? string.Empty;
        if (action.EmployeeScope == "Self")
        {
            if (employeeName.Length > 0 || (employeeCode.Length > 0 && !string.Equals(employeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase)))
                return Clarify(actor, action with { EmployeeCode = null, EmployeeName = null, EmployeeScope = "Unspecified" }, "Please specify one employee name or code.");
            employeeCode = actor.EmployeeCode;
        }
        if (employeeCode.Length == 0 && employeeName.Length == 0)
            return Clarify(actor, action, "Whose timesheet would you like to retrieve? Enter the employee name or code.");

        DataSet employees = await dropdownService.GetEmployeeListAsync(cancellationToken);
        if (employees.Tables.Count == 0 || !employees.Tables[0].Columns.Contains("ValueText") || !employees.Tables[0].Columns.Contains("DisplayText"))
            return Response("Error", "Employee list is unavailable.", action);
        var candidates = employees.Tables[0].Rows.Cast<DataRow>()
            .Select(r => new { Code = Convert.ToString(r["ValueText"])?.Trim() ?? "", Name = Convert.ToString(r["DisplayText"])?.Trim() ?? "" })
            .Where(r => r.Code.Length > 0 && (employeeCode.Length == 0 || string.Equals(r.Code, employeeCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (employeeName.Length > 0)
        {
            var exact = candidates.Where(r => string.Equals(r.Name, employeeName, StringComparison.OrdinalIgnoreCase)).ToList();
            candidates = exact.Count > 0 ? exact : candidates.Where(r => r.Name.Contains(employeeName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (candidates.Count != 1)
        {
            string question = candidates.Count == 0 ? "Employee not found. Please enter the correct employee code or full name." :
                "Multiple employees match. Please enter the employee code: " + string.Join(", ", candidates.Take(10).Select(r => $"{r.Name} ({r.Code})"));
            return Clarify(actor, action with { EmployeeCode = null, EmployeeName = null, EmployeeScope = "SpecificEmployee" }, question);
        }
        employeeCode = candidates[0].Code;
        if (action.Intent == "CheckTimesheetStatus")
            return await CheckTimesheetStatusAsync(actor, action, employeeCode, cancellationToken);
        var row = await GetSubmittedTimesheetAsync(employeeCode, action, cancellationToken);
        if (row is null)
            return Response("Error", "No unique submitted timesheet was found for this employee and period.", action);
        if (action.Intent is "ApproveTimesheet" or "RejectTimesheet")
        {
            action = action with { EmployeeCode = employeeCode, EmployeeName = candidates[0].Name, EmployeeScope = "SpecificEmployee" };
            if (!IsPendingApproval(row))
                return Response("Error", "Only timesheets pending approval can be approved or rejected.", action,
                    employee: employeeCode, id: Convert.ToInt32(row["ProjectTimeSheetID"], CultureInfo.InvariantCulture),
                    hours: Convert.ToString(row["TotalHoursWorked"], CultureInfo.InvariantCulture) ?? "",
                    timesheetStatus: Convert.ToString(row["TimesheetApprovalStatus"]) ?? "");
            int id = Convert.ToInt32(row["ProjectTimeSheetID"], CultureInfo.InvariantCulture);
            DataSet export = await timesheetService.GetOrExportEmployeeTimesheetList(new ProjectTimesheetApproveRejection
            {
                ProjectID = 0,
                ProjectTimeSheetID = id,
                EmployeeCode = employeeCode,
                TimesheetMonth = Month(action),
                TimesheetYear = action.TimesheetYear.Value,
                Action = "Export"
            }, cancellationToken);
            DataTable? exportTable = export?.Tables["EMPLOYEE_TIMESHEET_EXPORTED_RESPONSE"];
            if (exportTable is null || exportTable.Rows.Count != 1 ||
                !exportTable.Columns.Contains("Status") || !exportTable.Columns.Contains("FileName") ||
                !exportTable.Columns.Contains("ContentType") || !exportTable.Columns.Contains("FileBase64") ||
                !string.Equals(Convert.ToString(exportTable.Rows[0]["Status"]), "Success", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(Convert.ToString(exportTable.Rows[0]["FileName"])) ||
                string.IsNullOrWhiteSpace(Convert.ToString(exportTable.Rows[0]["ContentType"])) ||
                string.IsNullOrWhiteSpace(Convert.ToString(exportTable.Rows[0]["FileBase64"])))
                return Response("Error", "Unable to generate the timesheet Excel file. Please repeat your request.", action,
                    employee: employeeCode, id: id, hours: Convert.ToString(row["TotalHoursWorked"], CultureInfo.InvariantCulture) ?? "",
                    timesheetStatus: Convert.ToString(row["TimesheetApprovalStatus"]) ?? "");

            if (string.IsNullOrWhiteSpace(action.Remarks) || action.Remarks.Length > 2000)
            {
                DataSet result = Clarify(actor, action with { Remarks = null }, $"Please provide {(action.Intent == "ApproveTimesheet" ? "approval" : "rejection")} remarks for {candidates[0].Name}'s timesheet for {Month(action)} {action.TimesheetYear} (maximum 2000 characters).", row);
                result.Tables.Add(exportTable.Copy());
                return result;
            }
            action = action with { Remarks = action.Remarks.Trim() };
            string token = confirmations.Add(new(actor.UserId, actor.EmployeeCode, action, employeeCode, id, DateTimeOffset.UtcNow.Add(TokenLifetime)));
            DataSet confirmationResult = Response("ConfirmationRequired", $"Confirm {action.Intent} for {candidates[0].Name} ({employeeCode}), {Month(action)} {action.TimesheetYear}? Remarks: {action.Remarks}", action, token, employee: employeeCode, id: id, hours: Convert.ToString(row["TotalHoursWorked"], CultureInfo.InvariantCulture) ?? "", timesheetStatus: Convert.ToString(row["TimesheetApprovalStatus"]) ?? "");
            confirmationResult.Tables.Add(exportTable.Copy());
            return confirmationResult;
        }

        // Existing monthly view and Excel export use the same service and SQL branch.
        return await timesheetService.GetOrExportEmployeeTimesheetList(new ProjectTimesheetApproveRejection
        {
            ProjectID = 0,
            ProjectTimeSheetID = Convert.ToInt32(row["ProjectTimeSheetID"]),
            EmployeeCode = employeeCode,
            TimesheetMonth = Month(action),
            TimesheetYear = action.TimesheetYear.Value,
            Action = action.Intent == "ViewTimesheet" ? "View" : "Export"
        }, cancellationToken);
    }

    /// <summary>This Method Is Used To Identify Explicit Own Timesheet Status Enquiries.</summary>
    private static bool IsExplicitOwnTimesheetStatusRequest(string message)
    {
        // Match the entire request; never use a broad substring match to grant access.
        return System.Text.RegularExpressions.Regex.IsMatch(message.Trim(),
            @"\A(?:what\s+is\s+(?:the|my)\s+timesheet\s+status\s+(?:of|for)|(?:check|show|display)\s+my\s+timesheet\s+status\s+for)\s+(?:january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|jun|jul|aug|sep|sept|oct|nov|dec)\s+[0-9]{4}[?.]?\z",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
    }

    private static bool IsPendingApproval(DataRow row) =>
        string.Equals(Convert.ToString(row["TimesheetApprovalStatus"]), "Pending For Approval", StringComparison.OrdinalIgnoreCase);

    private async Task<DataRow?> GetSubmittedTimesheetAsync(string employeeCode, AITimesheetManagementActionM action, CancellationToken cancellationToken)
    {
        if (action.TimesheetMonth is null or < 1 or > 12 || action.TimesheetYear is null or < 2000 or > 9999 || string.IsNullOrWhiteSpace(employeeCode)) return null;
        var parameters = new Dictionary<string, object>
        {
            { "@ProjectTimeSheetID", 0 }, { "@EmployeeCode", employeeCode },
            { "@TimeSheetMonth", Month(action) }, { "@TimeSheetYear", action.TimesheetYear.Value },
            { "@ApproverEmailID", string.Empty }, { "@UserAccessToken", string.Empty },
            { "@ParamType", "GetAllEmployeeProjectTimesheetForApproval" }
        };
        var ds = await sql.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_PROJECT_TIMESHEET_SUBMISSION, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
        if (ds is null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count != 1)
            return null;
        var row = ds.Tables[0].Rows[0];
        if (!string.Equals(Convert.ToString(row["Status"]), "Success", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Convert.ToString(row["EmployeeCode"]), employeeCode, StringComparison.OrdinalIgnoreCase) ||
            !Convert.ToBoolean(row["IsSentForApproval"]) || Convert.ToInt32(row["ProjectTimeSheetID"]) <= 0)
            return null;

        return row;
    }

    private DataSet Clarify(AITimesheetActor actor, AITimesheetManagementActionM action, string message, DataRow? row = null)
    {
        string token = clarifications.Add(new(actor.UserId, actor.EmployeeCode, action, DateTimeOffset.UtcNow.Add(TokenLifetime)));
        if (row is null)
            return Response("ClarificationRequired", message, action, clarification: token);
        return Response("ClarificationRequired", message, action, clarification: token,
            employee: Convert.ToString(row["EmployeeCode"]) ?? "",
            id: Convert.ToInt32(row["ProjectTimeSheetID"], CultureInfo.InvariantCulture),
            hours: Convert.ToString(row["TotalHoursWorked"], CultureInfo.InvariantCulture) ?? "",
            timesheetStatus: Convert.ToString(row["TimesheetApprovalStatus"]) ?? "");
    }

    private static string Month(AITimesheetManagementActionM action) => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(action.TimesheetMonth!.Value);

    private async Task<(DataRow? Row, string? ValidationMessage)> GetEligibleTimesheetAsync(AITimesheetActor actor, AITimesheetManagementActionM action, CancellationToken cancellationToken)
    {
        var (row, error) = await GetTimesheetStatusRowAsync(actor.EmployeeCode, action, cancellationToken);
        if (row is null || error != null) return (row, error);
        string period = $"{Month(action)} - {action.TimesheetYear}";
        string? status = Convert.ToString(row["TimesheetApprovalStatus"]);
        bool sent = Convert.ToBoolean(row["IsSentForApproval"]);
        bool resend = Convert.ToBoolean(row["IsAvailableToResendForApproval"]);
        if (action.Intent == "SubmitTimesheet" ? status == "Not Submitted" && !sent : status == "Rejected" && sent && resend)
            return (row, null);
        string validationMessage = status switch
        {
            "Pending For Approval" => $"Your Timesheet Is Already Submitted For Approval For The Month Of {period} !!!",
            "Approved" => $"Your Timesheet Is Already Approved For The Month Of {period} !!!",
            "Rejected" when action.Intent == "SubmitTimesheet" => $"Your Timesheet Is Rejected For The Month Of {period}. Please Resubmit The Timesheet For Approval !!!",
            "Not Submitted" when action.Intent == "ResubmitTimesheet" => $"Your Timesheet Is Not Yet Submitted For The Month Of {period}. Please Submit The Timesheet For Approval !!!",
            _ => $"Your Timesheet Is Not Eligible For This Action For The Month Of {period} !!!"
        };
        return (row, validationMessage);
    }

    /// <summary>This Method Is Used To Get Employee Timesheet Status Details.</summary>
    private async Task<DataSet> CheckTimesheetStatusAsync(AITimesheetActor actor, AITimesheetManagementActionM action, string employeeCode, CancellationToken cancellationToken)
    {
        if (!actor.IsApprover && !string.Equals(employeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase))
            return Response("Error", "You can check only your own timesheet status.", action);
        if (action.TimesheetMonth is null || action.TimesheetYear is null)
            return Clarify(actor, action, "Please specify the missing month or year of the timesheet.");
        var (row, error) = await GetTimesheetStatusRowAsync(employeeCode, action, cancellationToken);
        if (row is null || error != null)
            return Response("Error", error ?? "Timesheet details are unavailable.", action, employee: employeeCode);
        string status = Convert.ToString(row["TimesheetApprovalStatus"]) ?? "";
        string Read(string column) => row.Table.Columns.Contains(column) ? Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? "" : "";
        bool decided = status is "Approved" or "Rejected";
        var result = Response("Success", $"Timesheet Status For The Month Of {Month(action)} - {action.TimesheetYear}: {status}",
            action with { EmployeeName = Read("FullName"), Remarks = decided ? Read("ApprovedOrRejectedReason") : null },
            employee: employeeCode, id: Convert.ToInt32(row["ProjectTimeSheetID"]), hours: Read("TotalHoursWorked"), timesheetStatus: status);
        var table = result.Tables[0];
        foreach (string column in new[] { "ApprovedOrRejectedReason", "ApprovedOrRejectedByUser", "ApprovedOrRejectedDateTime" })
        {
            table.Columns.Add(column, typeof(string));
            table.Rows[0][column] = decided ? Read(column) : string.Empty;
        }
        return result;
    }

    private async Task<(DataRow? Row, string? ValidationMessage)> GetTimesheetStatusRowAsync(string employeeCode, AITimesheetManagementActionM action, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employeeCode) || employeeCode == "0") return (null, "Invalid employee code.");
        if (action.TimesheetMonth is null or < 1 or > 12 || action.TimesheetYear is null or < 2000 or > 9999) return (null, "Invalid timesheet period.");
        var parameters = new Dictionary<string, object>
        {
            { "@ProjectTimeSheetID", 0 }, { "@EmployeeCode", employeeCode },
            { "@TimeSheetMonth", Month(action) }, { "@TimeSheetYear", action.TimesheetYear.Value },
            { "@ApproverEmailID", string.Empty }, { "@UserAccessToken", string.Empty },
            { "@ParamType", "GetEmployeeProjectTimesheetForSubmission" }
        };
        var ds = await sql.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_PROJECT_TIMESHEET_SUBMISSION, parameters, CONST.DBRetType.DATASET, cancellationToken) as DataSet;
        string period = $"{Month(action)} - {action.TimesheetYear}";
        if (ds is null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            return (null, $"No Timesheet Details Found For The Month Of {period} !!!");
        if (ds.Tables[0].Rows.Count != 1)
            return (null, $"Unable To Identify A Unique Timesheet For The Month Of {period} !!!");
        var row = ds.Tables[0].Rows[0];
        if (!string.Equals(Convert.ToString(row["Status"]), "Success", StringComparison.OrdinalIgnoreCase))
            return (null, row.Table.Columns.Contains("Message") && !string.IsNullOrWhiteSpace(Convert.ToString(row["Message"]))
                ? Convert.ToString(row["Message"]) : $"No Timesheet Details Found For The Month Of {period} !!!");
        if (!string.Equals(Convert.ToString(row["EmployeeCode"]), employeeCode, StringComparison.OrdinalIgnoreCase) || Convert.ToInt32(row["ProjectTimeSheetID"]) <= 0)
            return (null, "Unable To Verify The Timesheet Details !!!");
        return (row, null);
    }

    private static DataSet SubmissionValidationResponse(AITimesheetActor actor, AITimesheetManagementActionM action, DataRow? row, string message)
    {
        return Response("Error", message, action, employee: actor.EmployeeCode,
            id: row is null ? 0 : Convert.ToInt32(row["ProjectTimeSheetID"], CultureInfo.InvariantCulture),
            hours: row is null ? "" : Convert.ToString(row["TotalHoursWorked"], CultureInfo.InvariantCulture) ?? "",
            timesheetStatus: row is null ? "" : Convert.ToString(row["TimesheetApprovalStatus"]) ?? "");
    }

    private static DataSet Response(string status, string message, AITimesheetManagementActionM? action = null, string confirmation = "", string clarification = "", string employee = "", int id = 0, string hours = "", string timesheetStatus = "")
    {
        return Helper.GenerateCommonMessage(new List<string> { "Status", "Message", "Intent", "ConfirmationToken", "ClarificationToken", "EmployeeCode", "ProjectTimeSheetID", "TimesheetMonth", "TimesheetYear", "TotalHoursWorked", "TimesheetApprovalStatus", "EmployeeName", "Remarks" },
            new List<List<string>> { new() { status, message, action?.Intent ?? "Unknown", confirmation, clarification, employee, id.ToString(CultureInfo.InvariantCulture), action?.TimesheetMonth?.ToString(CultureInfo.InvariantCulture) ?? "", action?.TimesheetYear?.ToString(CultureInfo.InvariantCulture) ?? "", hours, timesheetStatus, action?.EmployeeName ?? "", action?.Remarks ?? "" } }, TableName);
    }
}
