using System.Data;
using VUSYSTimeProWebAPI.Application.Abstractions.Configuration;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

namespace VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.LeaveManagement;

public interface IGitHubModelsAIService
{
    /// <summary>
    /// This Method Is Used To Extract Leave Action.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="today"></param>
    /// <param name="AIAPIConfigurationService"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AILeaveManagementActionM> ExtractLeaveActionAsync(string message, DateOnly today, IAIAPIConfigurationService AIAPIConfigurationService, CancellationToken cancellationToken = default);
}

public interface IGeminiAIService
{
    /// <summary>
    /// This Method Is Used To Extract Leave Management Action Using Google Gemini AI.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="today"></param>
    /// <param name="availableLeaveTypes"></param>
    /// <param name="AIAPIConfigurationService"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AILeaveManagementActionM> ExtractLeaveActionAsync(string message, DateOnly today, IReadOnlyCollection<AILeaveTypeOptionM> availableLeaveTypes, IAIAPIConfigurationService AIAPIConfigurationService, CancellationToken cancellationToken = default);
}

public interface IAILeaveSecurityContext
{
    /// <summary>
    /// This Method Is Used To Resolves employees by code or name without exposing other employee data.
    /// </summary>
    /// <param name="employeeCode"></param>
    /// <param name="employeeName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<(string EmployeeCode, string EmployeeName)>> FindEmployeesForAIAsync(string? employeeCode, string? employeeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// This Method Is Used To Retrieves leave balances for all employees; caller must authorize first.
    /// </summary>
    /// <param name="leaveYear"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<DataSet> GetAllEmployeeLeaveBalancesAsync(int leaveYear, CancellationToken cancellationToken = default);

    /// <summary>
    /// This Method Is Used To Return the authenticated employee's authoritative leave balances.
    /// </summary>
    /// <param name="employeeCode"></param>
    /// <param name="leaveYear"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<DataSet> GetMyLeaveBalancesAsync(string employeeCode, int leaveYear, CancellationToken cancellationToken = default);
    
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

    /// <summary>
    /// This Method Is Used To Search Leave Requests By ID, Employee Or Dates.
    /// </summary>
    /// <param name="criteria"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<AILeaveRequestDetailsM>> SearchLeaveRequestsAsync(AILeaveRequestSearchCriteriaM criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// This Method Is Used To Get Leave Request Details For Review.
    /// </summary>
    /// <param name="leaveRequestID"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AILeaveRequestDetailsM?> GetLeaveRequestDetailsAsync(int leaveRequestID, CancellationToken cancellationToken = default);
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
    /// This Method Is Used To Store Leave Confirmation Token.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    string Add(AILeavePendingAction action);

    /// <summary>
    /// This Method Is Used To Consume Store Leave Confirmation Token.
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

public interface IAILeaveClarificationStore
{
    /// <summary>
    /// This Method Is Used To Store Leave Clarification Token.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    string Add(AILeavePendingClarification action);

    /// <summary>
    /// This Method Is Used To Consume Store Leave Clarification Token.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    bool TryConsume(string token, out AILeavePendingClarification? action);
}
