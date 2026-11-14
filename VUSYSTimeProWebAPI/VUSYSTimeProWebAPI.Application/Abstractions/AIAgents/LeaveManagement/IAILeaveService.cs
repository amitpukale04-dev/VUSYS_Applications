using System.Data;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

namespace VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;

public interface IGitHubModelsAIService
{
    /// <summary>
    /// This Method Is Used To Extract Leave Action.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="today"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AILeaveManagementActionM> ExtractLeaveActionAsync(string message, DateOnly today, CancellationToken cancellationToken = default);
}

public interface IGeminiAIService
{
    /// <summary>
    /// This Method Is Used To Extract Leave Management Action Using Google Gemini AI.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="today"></param>
    /// <param name="availableLeaveTypes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AILeaveManagementActionM> ExtractLeaveActionAsync(string message, DateOnly today, IReadOnlyCollection<AILeaveTypeOptionM> availableLeaveTypes, CancellationToken cancellationToken = default);
}

public interface IAILeaveSecurityContext
{
    /// <summary>
    /// This Method Is Used To Get Actor.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AILeaveActor> GetActorAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// This Method Is Used To Get Leave Request.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AILeaveRequestSnapshot?> GetLeaveRequestAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// This Method Is Used To Check For Approve.
    /// </summary>
    /// <param name="actor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> CanApproveAsync(AILeaveActor actor, CancellationToken cancellationToken = default);
}
public interface IAILeaveTypeResolver
{
    /// <summary>
    /// This Method Is Used To Resolve Leave Type ID.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int?> ResolveLeaveTypeIDAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// This Method Is Used To Get Available Leave Types.
    /// </summary>
    Task<IReadOnlyCollection<AILeaveTypeOptionM>> GetAvailableLeaveTypesAsync(CancellationToken cancellationToken = default);
}
public interface IAILeaveConfirmationStore
{
    /// <summary>
    /// This Is Used To Add Leave Confirmation.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    string Add(AILeavePendingAction action);

    /// <summary>
    /// This Method Is Used To Consume The Token.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    bool TryConsume(string token, out AILeavePendingAction? action);
}
public interface IAILeaveManagementAgentService
{
    /// <summary>
    /// This Method Is Used To Process AI Leave Management Request.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<DataSet> ProcessAILeaveManagementRequest(AILeaveManagementRequestParam request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// This Method Is Used To Confirm AI Leave Management Action.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<DataSet> ConfirmAILeaveManagementAction(AILeaveManagementConfirmRequestParam request, CancellationToken cancellationToken = default);
}
