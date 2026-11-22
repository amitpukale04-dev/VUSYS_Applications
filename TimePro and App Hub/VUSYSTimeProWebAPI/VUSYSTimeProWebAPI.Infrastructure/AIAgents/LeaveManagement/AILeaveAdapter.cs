using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Users;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.EntityFramework;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

public sealed class AILeaveSecurityContext(ICurrentUserService current, AppDbContext db, IDropdownService dropdown, SQLDBHelper _objSQLDBHelper) : IAILeaveSecurityContext
{
    /// <summary>
    /// This Method Is Used To Get User Identity.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task<AILeaveActor> GetActorAsync(CancellationToken ct = default)
    {
        if (!current.IsAuthenticated || string.IsNullOrWhiteSpace(current.EmployeeCode))
            throw new UnauthorizedAccessException("Authenticated employee is required.");
        var user = await db.UserList.AsNoTracking().Where(x => x.EmployeeCode == current.EmployeeCode && !x.IsDeleted)
            .Select(x => new { x.Id, x.EmployeeCode, x.IsApprover, x.EmailID }).FirstOrDefaultAsync(ct);
        if (user is null || user.Id <= 0 || !string.Equals(user.EmailID, current.EmailId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Employee identity cannot be verified.");
        return new AILeaveActor(user.Id, user.EmployeeCode, user.IsApprover == true);
    }
    private const string AILeaveResponseTable = "AI_LEAVE_REQUEST_DETAILS_RESPONSE";

    /// <summary>
    /// This Method Is Used To Get Leave Request Using Stored Procedure.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<AILeaveRequestSnapshot?> GetLeaveRequestAsync(int id, CancellationToken ct = default)
    {
        AILeaveRequestDetailsM? details = await GetLeaveRequestDetailsAsync(id, ct);
        return details is null ? null : new AILeaveRequestSnapshot(details.LeaveRequestID, details.EmployeeCode, details.LeaveStatus);
    }

    /// <summary>
    /// This Method Is Used To Retrieve The Authenticated Employee Leave Balance Using The Existing Stored Procedure.
    /// </summary>
    /// <param name="employeeCode"></param>
    /// <param name="leaveYear"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<DataSet> GetMyLeaveBalancesAsync(string employeeCode, int leaveYear, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(employeeCode) || leaveYear < 2000 || leaveYear > 9999)
            throw new ArgumentException("Valid employee code and leave year are required.");
        ct.ThrowIfCancellationRequested();
        var parameters = new Dictionary<string, object>
        {
            { "@Id", 0 },
            { "@EmployeeCode", employeeCode.Trim() },
            { "@LeaveYear", leaveYear },
            { "@Type", "GetEmployeeLeaveBalance" }
        };
        DataSet? ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_LEAVE_BALANCE_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, ct) as DataSet;
        ct.ThrowIfCancellationRequested();
        return ds ?? new DataSet();
    }

    /// <summary>
    /// This Method Is Used For Admin-only use: To Retrieve all employees' Leave Balances For a Year.
    /// </summary>
    /// <param name="leaveYear"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<DataSet> GetAllEmployeeLeaveBalancesAsync(int leaveYear, CancellationToken ct = default)
    {
        if (leaveYear < 2000 || leaveYear > 9999) throw new ArgumentException("Invalid leave year.");
        var parameters = new Dictionary<string, object>
        {
            { "@Id", 0 }, { "@EmployeeCode", string.Empty }, { "@LeaveYear", leaveYear },
            { "@Type", "GetAllEmployeeLeaveBalanceList" }
        };
        return await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.EMPLOYEE_LEAVE_BALANCE_DETAIL_LIST, parameters, CONST.DBRetType.DATASET, ct) as DataSet ?? new DataSet();
    }

    /// <summary>
    /// This Method Is Used To Resolves employees by code or name without exposing other employee data.
    /// </summary>
    /// <param name="employeeCode"></param>
    /// <param name="employeeName"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<(string EmployeeCode, string EmployeeName)>> FindEmployeesForAIAsync(string? employeeCode, string? employeeName, CancellationToken ct = default)
    {
        string code = employeeCode?.Trim() ?? string.Empty;
        string name = employeeName?.Trim() ?? string.Empty;
        if (code.Length == 0 && name.Length == 0) return Array.Empty<(string, string)>();
        var parameters = new Dictionary<string, object>
        {
            { "@Id", 0 }, { "@EmployeeCode", code }, { "@EmployeeName", name },
            { "@FromDate", DBNull.Value }, { "@ToDate", DBNull.Value },
            { "@LeaveStatus", DBNull.Value }, { "@ParamType", "GetEmployeeLookupForAI" },
            { "@Intent", string.Empty }, { "@DateSearchMode", "Exact" }, { "@AllowAllEmployees", 0 }
        };
        DataSet? ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.AI_LEAVE_MANAGEMENT_GET_LEAVE_DETAILS, parameters, CONST.DBRetType.DATASET, ct) as DataSet;
        DataTable? table = ds?.Tables.Cast<DataTable>().FirstOrDefault(t => t.Columns.Contains("EmployeeCode") && t.Columns.Contains("EmployeeName"));
        if (table is null) return Array.Empty<(string, string)>();
        return table.Rows.Cast<DataRow>().Select(r => (Convert.ToString(r["EmployeeCode"]) ?? string.Empty, Convert.ToString(r["EmployeeName"]) ?? string.Empty)).ToList();
    }

    /// <summary>
    /// This Method Is Used To Search Leave Requests By ID, Employee Or Dates.
    /// </summary>
    /// <param name="criteria"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<AILeaveRequestDetailsM>> SearchLeaveRequestsAsync(AILeaveRequestSearchCriteriaM criteria, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ct.ThrowIfCancellationRequested();

        // Exact and overlap searches are supported by the stored procedure.

        bool readOnly = criteria.Intent is "GetLeaveStatus" or "GetLeaveHistory" or "GetPendingLeaves" or "GetLeaveDetails";
        if (!readOnly && criteria.LeaveRequestID is not > 0 &&
            (string.IsNullOrWhiteSpace(criteria.FromDate) || string.IsNullOrWhiteSpace(criteria.ToDate)))
            return Array.Empty<AILeaveRequestDetailsM>();

        // Do not allow an unrestricted search.
        if (criteria.LeaveRequestID is not > 0 &&
            string.IsNullOrWhiteSpace(criteria.EmployeeCode) && string.IsNullOrWhiteSpace(criteria.EmployeeName) && !criteria.AllowAllEmployees)
            return Array.Empty<AILeaveRequestDetailsM>();

        return await ExecuteLeaveRequestLookupAsync("SearchLeaveRequestsForAI", criteria, ct);
    }

    /// <summary>
    /// This Method Is Used To Get Leave Request Details For Review.
    /// </summary>
    /// <param name="leaveRequestID"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<AILeaveRequestDetailsM?> GetLeaveRequestDetailsAsync(int leaveRequestID, CancellationToken ct = default)
    {
        if (leaveRequestID <= 0) return null;
        IReadOnlyList<AILeaveRequestDetailsM> records = await ExecuteLeaveRequestLookupAsync("GetLeaveRequestDetailsForAI", new AILeaveRequestSearchCriteriaM { LeaveRequestID = leaveRequestID }, ct);
        return records.FirstOrDefault();
    }

    /// <summary>
    /// This Method Is Used To Execute AI Leave Request and Map the DataSet data which is received from the Stored Procedure.
    /// </summary>
    /// <param name="paramType"></param>
    /// <param name="criteria"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<IReadOnlyList<AILeaveRequestDetailsM>> ExecuteLeaveRequestLookupAsync(string paramType, AILeaveRequestSearchCriteriaM criteria, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        DateTime? fromDate = string.IsNullOrWhiteSpace(criteria.FromDate) ? null : DataTypeUtils.ToDateTimeExact(criteria.FromDate.Trim(), DateTimeFormatConstants.YYYYMMDD_DATEFORMAT);
        DateTime? toDate = string.IsNullOrWhiteSpace(criteria.ToDate) ? null : DataTypeUtils.ToDateTimeExact(criteria.ToDate.Trim(), DateTimeFormatConstants.YYYYMMDD_DATEFORMAT);
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            throw new ArgumentException("From Date Cannot Be Greater Than To Date.");

        var parameters = new Dictionary<string, object>
        {
            { "@Id", criteria.LeaveRequestID.GetValueOrDefault() },
            { "@EmployeeCode", criteria.EmployeeCode?.Trim() ?? string.Empty },
            { "@EmployeeName", criteria.EmployeeName?.Trim() ?? string.Empty },
            { "@FromDate", fromDate.HasValue ? fromDate.Value : DBNull.Value },
            { "@ToDate", toDate.HasValue ? toDate.Value : DBNull.Value },
            { "@LeaveStatus", DBNull.Value },
            { "@ParamType", paramType },
            { "@Intent", criteria.Intent?.Trim() ?? string.Empty },
            { "@DateSearchMode", criteria.ExactDateRange ? "Exact" : "Overlap" },
            { "@AllowAllEmployees", criteria.AllowAllEmployees ? 1 : 0 },
        };

        DataSet? finalDataSet = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.AI_LEAVE_MANAGEMENT_GET_LEAVE_DETAILS, parameters, CONST.DBRetType.DATASET, ct) as DataSet;
        ct.ThrowIfCancellationRequested();

        // Preserve the existing DataSet validation convention.
        finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, AILeaveResponseTable);
        if (finalDataSet is null || finalDataSet.Tables.Count == 0)
            return Array.Empty<AILeaveRequestDetailsM>();

        DataTable? table = finalDataSet.Tables.Contains(AILeaveResponseTable)
            ? finalDataSet.Tables[AILeaveResponseTable] : finalDataSet.Tables[0];
        if (table is null || table.Rows.Count == 0)
            return Array.Empty<AILeaveRequestDetailsM>();

        // Error responses from the SP have Status/Message, not leave detail columns.
        if (!table.Columns.Contains("LeaveRequestID"))
        {
            string status = DataTypeUtils.GetDataRowValue<string>(table.Rows[0], "Status", string.Empty);
            if (string.Equals(status, Helper.ToProperCase(MessageTypeConstants.Error), StringComparison.OrdinalIgnoreCase))
            {
                string message = DataTypeUtils.GetDataRowValue<string>(table.Rows[0], "Message", string.Empty);
                if (message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
                    return Array.Empty<AILeaveRequestDetailsM>();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                    ? "AI leave request lookup failed." : message);
            }
            throw new InvalidOperationException("Unexpected AI leave request stored procedure response.");
        }

        var records = table.Rows.Cast<DataRow>().Select(row => new AILeaveRequestDetailsM
        {
            LeaveRequestID = DataTypeUtils.GetDataRowValue<int>(row, "LeaveRequestID"),
            EmployeeCode = DataTypeUtils.GetDataRowValue<string>(row, "EmployeeCode", string.Empty),
            EmployeeName = DataTypeUtils.GetDataRowValue<string>(row, "EmployeeName", string.Empty),
            LeaveTypeID = DataTypeUtils.GetDataRowValue<int>(row, "LeaveTypeID"),
            LeaveType = DataTypeUtils.GetDataRowValue<string>(row, "LeaveType", string.Empty),
            FromDate = DataTypeUtils.GetDataRowValue<string>(row, "FromDate", string.Empty),
            ToDate = DataTypeUtils.GetDataRowValue<string>(row, "ToDate", string.Empty),
            IsHalfDay = DataTypeUtils.GetDataRowValue<bool>(row, "IsHalfDay"),
            TotalLeaveDays = DataTypeUtils.GetDataRowValue<decimal>(row, "TotalLeaveDays"),
            Reason = DataTypeUtils.GetDataRowValue<string>(row, "Reason", string.Empty),
            LeaveStatus = DataTypeUtils.GetDataRowValue<int>(row, "LeaveStatus"),
            Remarks = DataTypeUtils.GetDataRowValue<string>(row, "Remarks", string.Empty)
        }).Where(x => x.LeaveRequestID > 0).GroupBy(x => x.LeaveRequestID)
          .Select(x => x.First()).ToList();

        // The SP uses TOP (51); 51 results mean the selection may be incomplete.
        if (records.Count >= 51)
            throw new InvalidOperationException("Too many matching leave requests. Narrow the employee name or date range.");

        if (!string.IsNullOrWhiteSpace(criteria.EmployeeName))
        {
            string name = criteria.EmployeeName.Trim();
            var exact = records.Where(x => string.Equals(x.EmployeeName.Trim(), name, StringComparison.OrdinalIgnoreCase)).ToList();
            records = exact.Count > 0 ? exact : records.Where(x =>
                x.EmployeeName.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return records;
    }

    /// <summary>
    /// This Method Is Used To Check For Approver.
    /// </summary>
    /// <param name="actor"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> CanApproveAsync(AILeaveActor actor, CancellationToken ct = default)
    {
        if (!actor.IsApprover) return false;
        // Existing service assigns the FIRST approver. Require exact match until approver routing is refactored.
        var approvers = await dropdown.GetApproverDetails(ct);
        return approvers.Count > 0 && string.Equals(approvers[0].EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class AILeaveTypeResolver(IDropdownService dropdown) : IAILeaveTypeResolver
{
    /// <summary>
    /// This Method Is Used To Resolve Leave Type ID.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<int?> ResolveLeaveTypeIDAsync(string name, CancellationToken ct = default)
    {
        DataSet ds = await dropdown.GetLeaveTypeListAsync(ct);
        if (ds.Tables.Count == 0) return null;
        var rows = ds.Tables.Cast<DataTable>().SelectMany(t => t.Rows.Cast<DataRow>())
            .Where(r => r.Table.Columns.Contains("DisplayText") && r.Table.Columns.Contains("ValueText") &&
                string.Equals(Convert.ToString(r["DisplayText"])?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        return rows.Count == 1 && int.TryParse(Convert.ToString(rows[0]["ValueText"]), out int id) && id > 0 ? id : null;
    }

    /// <summary>
    /// This Method Is Used To Get Available Leave Types.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<AILeaveTypeOptionM>> GetAvailableLeaveTypesAsync(CancellationToken cancellationToken = default)
    {
        return await dropdown.GetAvailableLeaveTypesForAIAsync(cancellationToken);
    }

    /// <summary>
    /// This Method Is Used To Resolve a leave type using the same master data provided to Gemini.
    /// </summary>
    /// <param name="requestedLeaveType"></param>
    /// <param name="availableLeaveTypes"></param>
    /// <returns></returns>
    public static int? ResolveLeaveTypeID(string? requestedLeaveType, IReadOnlyCollection<AILeaveTypeOptionM> availableLeaveTypes)
    {
        if (string.IsNullOrWhiteSpace(requestedLeaveType))
            return null;

        string requested = requestedLeaveType.Trim();

        AILeaveTypeOptionM? match = availableLeaveTypes.FirstOrDefault(x =>
            string.Equals(x.LeaveType?.Trim(), requested, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.LeaveTypeCode?.Trim(), requested, StringComparison.OrdinalIgnoreCase));

        return match?.ID > 0 ? match.ID : null;
    }

    /// <summary>
    /// This Method Is Used To Get Available Leave Type Message.
    /// </summary>
    /// <param name="availableLeaveTypes"></param>
    /// <returns></returns>
    public static string GetAvailableLeaveTypesMessage(IReadOnlyCollection<AILeaveTypeOptionM> availableLeaveTypes)
    {
        return string.Join(", ", availableLeaveTypes
            .Where(x => x.ID > 0 && !string.IsNullOrWhiteSpace(x.LeaveType))
            .Select(x => string.IsNullOrWhiteSpace(x.LeaveTypeCode)
                ? x.LeaveType
                : $"{x.LeaveType} ({x.LeaveTypeCode})"));
    }
}

// Single-process implementation only; replace with atomic distributed storage for multi-server deployments.
public sealed class InMemoryAILeaveConfirmationStore : IAILeaveConfirmationStore
{
    private readonly ConcurrentDictionary<string, AILeavePendingAction> _items = new();

    /// <summary>
    /// This Method Is Used To Store Leave Confirmation Token.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public string Add(AILeavePendingAction action)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _items[token] = action;
        return token;
    }

    /// <summary>
    /// This Method Is Used To Consume Store Leave Confirmation Token.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public bool TryConsume(string token, out AILeavePendingAction? action)
    {
        action = null;
        if (string.IsNullOrWhiteSpace(token) || !_items.TryRemove(token, out var item)) return false;
        if (item.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        action = item;
        return true;
    }
}

/// <summary>Single-process implementation. Use atomic distributed storage for multiple instances.</summary>
public sealed class InMemoryAILeaveClarificationStore : IAILeaveClarificationStore
{
    private readonly ConcurrentDictionary<string, AILeavePendingClarification> _items = new();

    /// <summary>
    /// This Method Is Used To Store Leave Clarification Token.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public string Add(AILeavePendingClarification action)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _items[token] = action;
        return token;
    }

    /// <summary>
    /// This Method Is Used To Consume Store Leave Clarification Token.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public bool TryConsume(string token, out AILeavePendingClarification? action)
    {
        action = null;
        if (string.IsNullOrWhiteSpace(token) || !_items.TryRemove(token, out var item)) return false;
        if (item.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        action = item;
        return true;
    }
}
