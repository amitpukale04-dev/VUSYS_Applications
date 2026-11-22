using System;

namespace VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

/// <summary>
/// This Model Is Used To Store The Authorized AI Leave Report Definition And Scope.
/// </summary>
public sealed record AILeavePendingReport(
    int UserId,
    string ActorEmployeeCode,
    string EmployeeCode,
    bool AllowAllEmployees,
    AILeaveReportDefinitionM Definition,
    DateTimeOffset ExpiresAt);

