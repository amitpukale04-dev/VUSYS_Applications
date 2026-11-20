using System.Collections.Generic;

namespace VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;

/// <summary>
/// This Model Is Used To Define A Validated Dynamic Leave Report.
/// </summary>
public sealed class AILeaveReportDefinitionM
{
    /// <summary>
    /// Report Type
    /// </summary>
    public string ReportType { get; set; } = "LeaveApplications";
    
    /// <summary>
    /// Date Field
    /// </summary>
    public string DateField { get; set; } = "LeavePeriod";
    
    /// <summary>
    /// From Date
    /// </summary>
    public string? FromDate { get; set; }
    
    /// <summary>
    /// To Date
    /// </summary>
    public string? ToDate { get; set; }
    
    /// <summary>
    /// Leave Status
    /// </summary>
    public int? LeaveStatus { get; set; }
    
    /// <summary>
    /// Leave Year
    /// </summary>
    public int? LeaveYear { get; set; }
    
    /// <summary>
    /// Leave Type ID
    /// </summary>
    public int? LeaveTypeID { get; set; }
    
    /// <summary>
    /// Max Rows
    /// </summary>
    public int MaxRows { get; set; } = 200;
    
    /// <summary>
    /// Columns
    /// </summary>
    public List<string> Columns { get; set; } = [];
    
    /// <summary>
    /// Group By
    /// </summary>
    public List<string> GroupBy { get; set; } = [];
    
    /// <summary>
    /// Aggregate
    /// </summary>
    public string Aggregate { get; set; } = "None";

    /// <summary>
    /// Aggregate Column
    /// </summary>
    public string? AggregateColumn { get; set; }
}
