using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

/// <summary>
/// This Service Is Used To Store Validated AI Leave Report Tokens In Memory.
/// </summary>
public sealed class InMemoryAILeaveReportTokenStore : IAILeaveReportTokenStore
{
    private readonly ConcurrentDictionary<string, AILeavePendingReport> _items = new(StringComparer.Ordinal);

    /// <summary>
    /// This Method Is Used To Generate And Store A Secure Report Token.
    /// </summary>
    public string Add(AILeavePendingReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (report.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Report token expiration must be in the future.", nameof(report));

        foreach (var item in _items)
            if (item.Value.ExpiresAt <= DateTimeOffset.UtcNow)
                _items.TryRemove(item.Key, out _);

        // A deep copy prevents later mutation of the Gemini-generated definition.
        AILeavePendingReport snapshot = Clone(report);
        string token;
        do
        {
            token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }
        while (!_items.TryAdd(token, snapshot));
        return token;
    }

    /// <summary>
    /// This Method Is Used To Retrieve An Unexpired Report Definition Without Consuming Its Token.
    /// </summary>
    public bool TryGet(string token, out AILeavePendingReport? report)
    {
        report = null;
        if (string.IsNullOrWhiteSpace(token) || !_items.TryGetValue(token, out AILeavePendingReport? stored))
            return false;
        if (stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _items.TryRemove(token, out _);
            return false;
        }
        report = Clone(stored);
        return true;
    }

    private static AILeavePendingReport Clone(AILeavePendingReport report)
    {
        AILeaveReportDefinitionM definition = JsonSerializer.Deserialize<AILeaveReportDefinitionM>(
            JsonSerializer.Serialize(report.Definition))
            ?? throw new InvalidOperationException("Unable to clone the report definition.");
        return report with { Definition = definition };
    }
}
