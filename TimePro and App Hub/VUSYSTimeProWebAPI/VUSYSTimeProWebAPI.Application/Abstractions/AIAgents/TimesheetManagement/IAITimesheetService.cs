using System.Data;
using VUSYSTimeProWebAPI.Application.Abstractions.Configuration;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.TimesheetManagement;

namespace VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.TimesheetManagement;

public interface IGeminiAITimesheetService
{
    /// <summary>This Method Is Used To Extract Timesheet Management Action Using Google Gemini AI.</summary>
    Task<AITimesheetManagementActionM> ExtractTimesheetActionAsync(string message, DateOnly today, IAIAPIConfigurationService AIAPIConfigurationService, CancellationToken cancellationToken = default);
}

public interface IAITimesheetSecurityContext
{
    /// <summary>This Method Is Used To Get The Authenticated Actor.</summary>
    Task<AITimesheetActor> GetActorAsync(CancellationToken cancellationToken = default);
}

public interface IAITimesheetConfirmationStore
{
    string Add(AITimesheetPendingAction action);
    bool TryConsume(string token, AITimesheetActor actor, out AITimesheetPendingAction? action);
}

public interface IAITimesheetClarificationStore
{
    string Add(AITimesheetPendingClarification action);
    bool TryConsume(string token, AITimesheetActor actor, out AITimesheetPendingClarification? action);
}

public interface IAITimesheetManagementAgentService
{
    Task<DataSet> ProcessAITimesheetManagementRequest(AITimesheetManagementRequestParam request, CancellationToken cancellationToken = default);
    Task<DataSet> ConfirmAITimesheetManagementAction(AITimesheetManagementConfirmRequestParam request, CancellationToken cancellationToken = default);
}
