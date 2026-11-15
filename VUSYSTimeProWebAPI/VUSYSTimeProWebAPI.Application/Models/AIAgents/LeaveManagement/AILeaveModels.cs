using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

public sealed class AILeaveManagementRequestParam
{
    /// <summary>
    /// Message
    /// </summary>
    [DefaultValue("")]
    [Display(Name = "Message")]
    [StringLength(2000)]
    [Required(ErrorMessage = "Enter The Message.")]
    public string Message { get; set; }

    /// <summary>Token returned when the AI needs a half-day clarification.</summary>
    [DefaultValue("")]
    [Display(Name = "Confirmation Token")]
    public string? ClarificationToken { get; set; }
}

public sealed class AILeaveManagementConfirmRequestParam
{
    /// <summary>
    /// Confirmation Token
    /// </summary>
    [DefaultValue("")]
    [Display(Name = "Confirmation Token")]
    [Required(ErrorMessage = "Enter The Confirmation Token.")]
    public string ConfirmationToken { get; set; }
}

public sealed class AILeaveManagementActionM
{
    /// <summary>
    /// Intent
    /// </summary>
    public string Intent { get; set; } = "Unknown";

    /// <summary>
    /// Leave Type
    /// </summary>
    public string? LeaveType { get; set; }

    /// <summary>
    /// From Date
    /// </summary>
    public string? FromDate { get; set; }

    /// <summary>
    /// To Date
    /// </summary>
    public string? ToDate { get; set; }

    /// <summary>
    /// Is Half Day
    /// </summary>
    public bool IsHalfDay { get; set; }

    /// <summary>
    /// Half Day Type
    /// </summary>
    public string? HalfDayType { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Total Leave Days
    /// </summary>
    public decimal TotalLeaveDays { get; set; }

    /// <summary>
    /// Leave Status
    /// </summary>
    public int LeaveStatus { get; set; }

    /// <summary>
    /// Leave Request ID
    /// </summary>
    public int? LeaveRequestID { get; set; }

    /// <summary>
    /// Employee Code
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// Employee Name (for lookup when LeaveRequestID is omitted)
    /// </summary>
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Remarks
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>Exact: match leave dates exactly. Overlap: search requests overlapping the specified period.</summary>
    public string DateSearchMode { get; set; } = "Exact";
}

/// <summary>
/// This Method Is Used For Leave Actor.
/// </summary>
/// <param name="UserId"></param>
/// <param name="EmployeeCode"></param>
/// <param name="IsApprover"></param>
public sealed record AILeaveActor(int UserId, string EmployeeCode, bool IsApprover);

/// <summary>
/// This Method Is Used For Leave Request.
/// </summary>
/// <param name="Id"></param>
/// <param name="EmployeeCode"></param>
/// <param name="LeaveStatus"></param>
public sealed record AILeaveRequestSnapshot(int Id, string EmployeeCode, int LeaveStatus);

/// <summary>
/// This Method Is Used To Leave Pending Action.
/// </summary>
/// <param name="UserId"></param>
/// <param name="EmployeeCode"></param>
/// <param name="Action"></param>
/// <param name="LeaveTypeID"></param>
/// <param name="TargetEmployeeCode"></param>
/// <param name="ExpiresAt"></param>
public sealed record AILeavePendingAction(int UserId, string EmployeeCode, AILeaveManagementActionM Action, int? LeaveTypeID, string? TargetEmployeeCode, DateTimeOffset ExpiresAt);

/// <summary>
/// This Method Is Used To Pending half-day clarification associated with the authenticated employee.
/// </summary>
/// <param name="UserId"></param>
/// <param name="EmployeeCode"></param>
/// <param name="Action"></param>
/// <param name="ExpiresAt"></param>
public sealed record AILeavePendingClarification(int UserId, string EmployeeCode, AILeaveManagementActionM Action, DateTimeOffset ExpiresAt);

/// <summary>
/// This Model Is Used To Search Leave Requests Without Requiring LeaveRequestID.
/// </summary>
public sealed class AILeaveRequestSearchCriteriaM
{
    /// <summary>
    /// Leave Request ID
    /// </summary>
    public int? LeaveRequestID { get; set; }

    /// <summary>
    /// Employee Code
    /// </summary>
    public string? EmployeeCode { get; set; }

    /// <summary>
    /// Employee Name
    /// </summary>
    public string? EmployeeName { get; set; }

    /// <summary>
    /// From Date
    /// </summary>
    public string? FromDate { get; set; }

    /// <summary>
    /// To Date
    /// </summary>
    public string? ToDate { get; set; }

    /// <summary>
    /// Exact Date Range
    /// </summary>
    public bool ExactDateRange { get; set; } = true;

    /// <summary>AI intent used for database-side eligibility filtering.</summary>
    public string? Intent { get; set; }
}

/// <summary>
/// This Model Is Used To Display Authoritative Leave Request Details.
/// </summary>
public sealed class AILeaveRequestDetailsM
{
    /// <summary>
    /// Leave Request ID
    /// </summary>
    public int LeaveRequestID { get; set; }

    /// <summary>
    /// Employee Code
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// Employee Name
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Leave Type ID
    /// </summary>
    public int LeaveTypeID { get; set; }

    /// <summary>
    /// Leave Type
    /// </summary>
    public string LeaveType { get; set; } = string.Empty;

    /// <summary>
    /// From Date
    /// </summary>
    public string FromDate { get; set; } = string.Empty;

    /// <summary>
    /// To Date
    /// </summary>
    public string ToDate { get; set; } = string.Empty;

    /// <summary>
    /// Is Half Day
    /// </summary>
    public bool IsHalfDay { get; set; }

    /// <summary>
    /// Total Leave Days
    /// </summary>
    public decimal TotalLeaveDays { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Leave Status
    /// </summary>
    public int LeaveStatus { get; set; }

    /// <summary>
    /// Remarks
    /// </summary>
    public string Remarks { get; set; } = string.Empty;
}
