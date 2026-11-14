using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Users;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.EntityFramework;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

public sealed class AILeaveSecurityContext(ICurrentUserService current, AppDbContext db, IDropdownService dropdown) : IAILeaveSecurityContext
{
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

    /// <summary>
    /// This Method Is Used To Get Leave Request.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<AILeaveRequestSnapshot?> GetLeaveRequestAsync(int id, CancellationToken ct = default) =>
        await db.LeaveApplicationList.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new AILeaveRequestSnapshot(x.Id, x.EmployeeCode, x.LeaveStatus)).FirstOrDefaultAsync(ct);
    
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
    /// This Is Used To Add Leave Confirmation.
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
    /// This Method Is Used To Consume The Token.
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
