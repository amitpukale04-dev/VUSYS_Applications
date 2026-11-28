using System.Collections.Concurrent;
using System.Security.Cryptography;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.TimesheetManagement;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.TimesheetManagement;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.TimesheetManagement;

public sealed class InMemoryAITimesheetConfirmationStore : IAITimesheetConfirmationStore
{
    private readonly ConcurrentDictionary<string, AITimesheetPendingAction> _items = new();

    public string Add(AITimesheetPendingAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Token expiry must be in the future.");
        foreach (var item in _items)
            if (item.Value.ExpiresAt <= DateTimeOffset.UtcNow)
                _items.TryRemove(item.Key, out _);
        string token;
        do { token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); }
        while (!_items.TryAdd(token, action));
        return token;
    }

    public bool TryConsume(string token, AITimesheetActor actor, out AITimesheetPendingAction? action)
    {
        action = null;
        if (string.IsNullOrWhiteSpace(token) || !_items.TryGetValue(token, out var item)) return false;
        if (item.UserId != actor.UserId || !string.Equals(item.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase)) return false;
        if (!_items.TryRemove(token, out var consumed) || consumed.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        action = consumed;
        return true;
    }
}

public sealed class InMemoryAITimesheetClarificationStore : IAITimesheetClarificationStore
{
    private readonly ConcurrentDictionary<string, AITimesheetPendingClarification> _items = new();

    public string Add(AITimesheetPendingClarification action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Token expiry must be in the future.");
        foreach (var item in _items)
            if (item.Value.ExpiresAt <= DateTimeOffset.UtcNow)
                _items.TryRemove(item.Key, out _);
        string token;
        do { token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); }
        while (!_items.TryAdd(token, action));
        return token;
    }

    public bool TryConsume(string token, AITimesheetActor actor, out AITimesheetPendingClarification? action)
    {
        action = null;
        if (string.IsNullOrWhiteSpace(token) || !_items.TryGetValue(token, out var item)) return false;
        if (item.UserId != actor.UserId || !string.Equals(item.EmployeeCode, actor.EmployeeCode, StringComparison.OrdinalIgnoreCase)) return false;
        if (!_items.TryRemove(token, out var consumed) || consumed.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        action = consumed;
        return true;
    }
}
