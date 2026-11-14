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
    /// Leave Request ID
    /// </summary>
    public int? LeaveRequestID { get; set; }
    
    /// <summary>
    /// Remarks
    /// </summary>
    public string? Remarks { get; set; }
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
