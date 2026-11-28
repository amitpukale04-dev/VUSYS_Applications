using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Application.Models.AIAgents.TimesheetManagement;

public sealed class AITimesheetManagementRequestParam
{
    [DefaultValue("")]
    [Display(Name = "Message")]
    [StringLength(2000)]
    [Required(ErrorMessage = "Enter The Message.")]
    public string Message { get; set; } = string.Empty;

    [DefaultValue("")]
    [Display(Name = "Clarification Token")]
    public string? ClarificationToken { get; set; }
}

public sealed class AITimesheetManagementConfirmRequestParam
{
    [DefaultValue("")]
    [Display(Name = "Confirmation Token")]
    [Required(ErrorMessage = "Enter The Confirmation Token.")]
    public string ConfirmationToken { get; set; } = string.Empty;
}

/// <summary>Untrusted parameters extracted from the user message.</summary>
public sealed record AITimesheetManagementActionM
{
    public string Intent { get; init; } = "Unknown";
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public int? TimesheetMonth { get; init; }
    public int? TimesheetYear { get; init; }
    public string? Remarks { get; init; }
    public string EmployeeScope { get; init; } = "Unspecified";
}

public sealed record AITimesheetActor(int UserId, string EmployeeCode, bool IsApprover);

/// <summary>Only create after resolving and validating the target against the database.</summary>
public sealed record AITimesheetPendingAction(int UserId, string EmployeeCode, AITimesheetManagementActionM Action, string TargetEmployeeCode, int ProjectTimeSheetID, DateTimeOffset ExpiresAt);

public sealed record AITimesheetPendingClarification(int UserId, string EmployeeCode, AITimesheetManagementActionM Action, DateTimeOffset ExpiresAt);
