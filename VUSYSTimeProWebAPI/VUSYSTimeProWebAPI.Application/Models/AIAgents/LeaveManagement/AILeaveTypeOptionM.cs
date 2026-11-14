namespace VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

public sealed class AILeaveTypeOptionM
{
    /// <summary>
    /// ID
    /// </summary>
    public int ID { get; set; }
    
    /// <summary>
    /// Leave Type Code
    /// </summary>
    public string LeaveTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Leave Type
    /// </summary>
    public string LeaveType { get; set; } = string.Empty;

    /// <summary>
    /// Leave Type Description
    /// </summary>
    public string LeaveTypeDescription { get; set; } = string.Empty;
}

