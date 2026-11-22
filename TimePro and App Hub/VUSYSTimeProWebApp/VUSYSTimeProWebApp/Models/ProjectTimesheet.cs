using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebApp.CustomAttribute;
using Microsoft.EntityFrameworkCore;

namespace VUSYSTimeProWebApp.Models
{
    [Table("VUSYS_ProjectTimeSheet")]
    [Keyless]
    public class ProjectTimesheet:CommonModel
    {
        #region [Project Timesheet Properties]

        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectTimeSheetID { get; set; } = 0;

        /// <summary>
        /// Employee Code
        /// </summary>
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Timesheet Month
        /// </summary>
        [Display(Name = "Timesheet Month")]
        public string TimesheetMonth { get; set; }

        /// <summary>
        /// Timesheet Year
        /// </summary>
        [Display(Name = "Timesheet Year")]
        public int TimesheetYear { get; set; }

        /// <summary>
        /// Project Time Sheet Detail ID
        /// </summary>
        public int ProjectTimeSheetDetailID { get; set; } = 0;

        /// <summary>
        /// Project ID
        /// </summary>
        [Required(ErrorMessage = "Select The Project.")]
        [Display(Name = "Project")]
        public int ProjectID { get; set; }

        /// <summary>
        /// Project Activity ID
        /// </summary>
        [Required(ErrorMessage = "Select The Activity.")]
        [Display(Name = "Activity")]
        public int ProjectActivityID { get; set; }

        /// <summary>
        /// Project Name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Activity Name
        /// </summary>
        public string ActivityName { get; set; }

        /// <summary>
        /// Client Name
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Task Date
        /// </summary>
        [Required(ErrorMessage = "Select The Task Date."), DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [TimesheetFutureDate(ErrorMessage = "Task Date Should Not Be Future Date.")]
        [Display(Name = "Task Date")]
        public DateTime TaskDate { get; set; }

        /// <summary>
        /// Task Description
        /// </summary>
        [Display(Name = "Task Description")]
        [Required(ErrorMessage = "Enter The Task Description.")]
        public string TaskDescription { get; set; }

        /// <summary>
        /// Task Description Raw
        /// </summary>
        public string TaskDescriptionRaw { get; set; }

        /// <summary>
        /// No of Hours Worked
        /// </summary>
        [Display(Name = "No. of Hours")]
        [DataType(DataType.Currency)]
        //[Required(ErrorMessage = "Enter The No. of Hours Worked.")]
        [NoofHoursWorked(IsOnLeavePropertyName = "IsOnLeave", NoofHoursWorkedPropertyName = "NoofHoursWorked", ErrorMessage = "Enter The No. of Hours.")]
        public string NoofHoursWorked { get; set; }

        /// <summary>
        /// Is On Leave
        /// </summary>
        [Display(Name = "Is On Leave")]
        public bool IsOnLeave { get; set; }

        /// <summary>
        /// Total Hours Worked
        /// </summary>
        public decimal TotalHoursWorked { get; set; }

        /// <summary>
        /// Project Status
        /// </summary>
        public string ProjectStatus { get; set; }

        /// <summary>
        /// Display Task Date
        /// </summary>
        public string DisplayTaskDate { get; set; }

        /// <summary>
        /// Is Timesheet Closed
        /// </summary>
        public bool IsTimesheetClosed { get; set; }

        /// <summary>
        /// Is Sent For Approval
        /// </summary>
        public bool IsSentForApproval { get; set; }

        /// <summary>
        /// Is Approved
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Mode
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Approved Or Rejected Reason
        /// </summary>
        public string ApprovedOrRejectedReason { get; set; }

        #endregion
    }

    public class ProjectTimesheetFilter
    {
        /// <summary>
        /// From Date
        /// </summary>
        [Required(ErrorMessage = "Select The From Date."), DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime FromTaskDate { get; set; }

        /// <summary>
        /// To Date
        /// </summary>
        [Required(ErrorMessage = "Select The To Date."), DataType(DataType.Date)]
        [DateValidation(FromDatePropertyName = "FromTaskDate", ErrorMessage = "To Date Should Be Greater Than From Date.")]
        [Display(Name = "To Date")]
        public DateTime ToTaskDate { get; set; }
    }

    public class ProjectTimesheetSubmissionFilter
    {
        /// <summary>
        /// Month
        /// </summary>
        [Required(ErrorMessage = "Select The Month.")]
        [Display(Name = "Month")]
        public string TimesheetMonth { get; set; }

        /// <summary>
        /// Year
        /// </summary>
        [Required(ErrorMessage = "Select The Year.")]
        [Display(Name = "Year")]
        public int TimesheetYear { get; set; }
    }

    public class ProjectTimesheetReportFilter: ProjectTimesheetSubmissionFilter
    {
        /// <summary>
        /// Employee Code
        /// </summary>
        [Display(Name = "Employee")]
        public string EmployeeCode { get; set; }
    }

    public class LeaveReportFilter : ProjectTimesheetSubmissionFilter
    {
        /// <summary>
        /// Employee Code
        /// </summary>
        [Display(Name = "Employee")]
        public string EmployeeCode { get; set; }
    }

    public class ProjectTimesheetSubmissionApproveRejectReportList
    {
        /// <summary>
        /// SL No
        /// </summary>
        public Int64 SLNo { get; set; }

        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        public int ProjectTimeSheetID { get; set; } = 0;

        /// <summary>
        /// Employee Code
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Timesheet Month
        /// </summary>
        public string TimeSheetMonth { get; set; }

        /// <summary>
        /// Timesheet Year
        /// </summary>
        public int TimeSheetYear { get; set; }

        /// <summary>
        /// Total Hours Worked
        /// </summary>
        public decimal TotalHoursWorked { get; set; }

        /// <summary>
        /// Timesheet Approval Status
        /// </summary>
        public string TimesheetApprovalStatus { get; set; }

        /// <summary>
        /// Approved Or Rejected Reason
        /// </summary>
        public string ApprovedOrRejectedReason { get; set; }

        /// <summary>
        /// Approved Or Rejected By User
        /// </summary>
        public string ApprovedOrRejectedByUser { get; set; }

        /// <summary>
        /// Approved Or Rejected Date Time
        /// </summary>
        public string ApprovedOrRejectedDateTime { get; set; }

        /// <summary>
        /// Is Show Send For Approval Button
        /// </summary>
        public bool IsShowSendForApprovalButton { get; set; }

        /// <summary>
        /// Is Show Send Notification Button
        /// </summary>
        public bool IsShowSendNotificationButton { get; set; }

        /// <summary>
        /// Is Sent For Approval
        /// </summary>
        public bool IsSentForApproval { get; set; }

        /// <summary>
        /// Is Available To Resend For Approval
        /// </summary>
        public bool IsAvailableToResendForApproval { get; set; }

        /// <summary>
        /// Is Show View Details Button
        /// </summary>
        public bool IsShowViewDetailsButton { get; set; }

        /// <summary>
        /// Is Show Approve Reject Button
        /// </summary>
        public bool IsShowApproveRejectButton { get; set; }

        /// <summary>
        /// Other Parameters
        /// </summary>
        public string OtherParameters { get; set; }

        /// <summary>
        /// Approved Rejected Parameters
        /// </summary>
        public string ApprovedRejectedParameters { get; set; }
    }

    public class ProjectTimesheetApproveRejection
    {
        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        public int ProjectTimeSheetID { get; set; } = 0;

        /// <summary>
        /// Employee Code
        /// </summary>
        [Display(Name = "Employee")]
        [Required(ErrorMessage = "Enter The Employee.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Timesheet Month
        /// </summary>
        [Display(Name = "Timesheet Month")]
        [Required(ErrorMessage = "Enter The Timesheet Month.")]
        public string TimesheetMonth { get; set; }

        /// <summary>
        /// Timesheet Year
        /// </summary>
        [Display(Name = "Timesheet Year")]
        [Required(ErrorMessage = "Enter The Timesheet Year.")]
        public int TimesheetYear { get; set; }

        /// <summary>
        /// Project Time Sheet Approval Status ID
        /// </summary>
        [Required(ErrorMessage = "Select The Approval Status.")]
        [Display(Name = "Approval Status")]
        public string ProjectTimeSheetApprovalStatusID { get; set; }

        /// <summary>
        /// Remarks
        /// </summary>
        [Required(ErrorMessage = "Enter The Remarks.")]
        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        /// <summary>
        /// Source
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// User Token
        /// </summary>
        public string UserToken { get; set; }

        /// <summary>
        /// Project TimeSheet Approval Response Status
        /// </summary>
        public string ProjectTimeSheetApprovalResponseStatus { get; set; }

        /// <summary>
        /// Project TimeSheet Approval Response Message
        /// </summary>
        public string ProjectTimeSheetApprovalResponseMessage { get; set; }

        /// <summary>
        /// Is On Page Load
        /// </summary>
        public bool IsOnPageLoad { get; set; }
    }

    public class DownloadTimesheetFilter
    {
        /// <summary>
        /// Employee Code
        /// </summary>
        [Display(Name = "Employee")]
        [Required(ErrorMessage = "Select The Employee.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Timesheet Month
        /// </summary>
        [Display(Name = "Timesheet Month")]
        [Required(ErrorMessage = "Select The Timesheet Month.")]
        public string TimesheetMonth { get; set; }

        /// <summary>
        /// Timesheet Year
        /// </summary>
        [Display(Name = "Timesheet Year")]
        [Required(ErrorMessage = "Select The Timesheet Year.")]
        public int TimesheetYear { get; set; }
    }

    public enum ProjectTimesheetApprovalStatus
    {
        Approve = 1,
        Reject = 0
    }

    public class ProjectTimesheetApproveRejectFilter: ProjectTimesheetSubmissionFilter
    {
        /// <summary>
        /// Employee
        /// </summary>
        [Display(Name = "Employee")]
        public string EmployeeCode { get; set; }
    }

    public class ProjectTimesheetDownloadList
    {
        /// <summary>
        /// SL No
        /// </summary>
        public Int64 SLNo { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Display Task Date
        /// </summary>
        public string DisplayTaskDate { get; set; }

        /// <summary>
        /// Client Name
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Project Name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Activity Name
        /// </summary>
        public string ActivityName { get; set; }

        /// <summary>
        /// Task Description
        /// </summary>
        public string TaskDescription { get; set; }

        /// <summary>
        /// Total Hours Worked
        /// </summary>
        public decimal TotalHoursWorked { get; set; }
    }

    public class ProjectAllocatedConsumedReportFilter
    {
        /// <summary>
        /// Project ID
        /// </summary>
        [Display(Name = "Project")]
        public int ProjectID { get; set; }
    }

    public class ProjectAllocatedConsumedReportList
    {
        /// <summary>
        /// SL No
        /// </summary>
        public Int64 SLNo { get; set; }

        /// <summary>
        /// Project ID
        /// </summary>
        public int ProjectID { get; set; }

        /// <summary>
        /// Project Code
        /// </summary>
        public string ProjectCode { get; set; }

        /// <summary>
        /// Project Name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Client Name
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Allocated Hours
        /// </summary>
        public decimal AllocatedHours { get; set; }

        /// <summary>
        /// Consumed Hours
        /// </summary>
        public decimal ConsumedHours { get; set; }

        /// <summary>
        /// Requirement Gathering And Analysis
        /// </summary>
        public decimal RequirementGatheringAndAnalysis { get; set; }

        /// <summary>
        /// System Design
        /// </summary>
        public decimal SystemDesign { get; set; }

        /// <summary>
        /// Frontend Development
        /// </summary>
        public decimal FrontendDevelopment { get; set; }

        /// <summary>
        /// Backend Development
        /// </summary>
        public decimal BackendDevelopment { get; set; }

        /// <summary>
        /// Code Review
        /// </summary>
        public decimal CodeReview { get; set; }

        /// <summary>
        /// System Testing
        /// </summary>
        public decimal SystemTesting { get; set; }

        /// <summary>
        /// Project Management
        /// </summary>
        public decimal ProjectManagement { get; set; }

        /// <summary>
        /// Deployment
        /// </summary>
        public decimal Deployment { get; set; }

        /// <summary>
        /// Post Deployment Monitoring
        /// </summary>
        public decimal PostDeploymentMonitoring { get; set; }

        /// <summary>
        /// Fix Post Release Bugs
        /// </summary>
        public decimal FixPostReleaseBugs { get; set; }

        /// <summary>
        /// Maintenance
        /// </summary>
        public decimal Maintenance { get; set; }

        /// <summary>
        /// Knowledge Transfer
        /// </summary>
        public decimal KnowledgeTransfer { get; set; }

        /// <summary>
        /// Learning
        /// </summary>
        public decimal Learning { get; set; }
    }

    public class LeaveReportList
    {
        /// <summary>
        /// SL No
        /// </summary>
        public Int64 SLNo { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Email ID
        /// </summary>
        public string EmailID { get; set; }

        /// <summary>
        /// OnLeave
        /// </summary>
        public int OnLeave { get; set; }

        /// <summary>
        /// Holiday
        /// </summary>
        public int Holiday { get; set; }

        /// <summary>
        /// CompOff
        /// </summary>
        public int CompOff { get; set; }
    }
}
