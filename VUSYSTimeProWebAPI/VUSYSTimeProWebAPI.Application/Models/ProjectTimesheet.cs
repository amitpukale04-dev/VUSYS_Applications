using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    [Table("VUSYS_ProjectTimeSheet")]
    [Keyless]
    public class ProjectTimesheet : ProjectTimesheetSharedCommon
    {
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
    }

    public class ProjectTimesheetSharedCommon : ProjectTimesheetCommon, ICommonModel
    {
        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        [Display(Name = "Created By")]
        public int CreatedBy { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        [Display(Name = "Updated By")]
        public int UpdatedBy { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }
    }

    public class ProjectTimesheetRequestParameterCommon : ProjectTimesheetCommon, IRequestParameterCommonModel
    {
        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        [Display(Name = "Created By")]
        public int CreatedBy { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        [Display(Name = "Updated By")]
        public int UpdatedBy { get; set; }
    }

    public class ProjectTimesheetCommon
    {
        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Project TimeSheet ID")]
        [Required(ErrorMessage = "Enter The Project TimeSheet ID.")]
        public int ProjectTimeSheetID { get; set; } = 0;

        /// <summary>
        /// Project Time Sheet Detail ID
        /// </summary>
        [Display(Name = "Project TimeSheet Detail ID")]
        [Required(ErrorMessage = "Enter The Project TimeSheet Detail ID.")]
        public int ProjectTimeSheetDetailID { get; set; } = 0;

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Project ID
        /// </summary>
        [Display(Name = "Project ID")]
        [Required(ErrorMessage = "Enter The Project ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "ProjectID", ErrorMessage = "Project ID Should Be Greater Than 0.")]
        public int ProjectID { get; set; }

        /// <summary>
        /// Project Activity ID
        /// </summary>
        [Display(Name = "Project Activity ID")]
        [Required(ErrorMessage = "Enter The Project Activity ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "ProjectActivityID", ErrorMessage = "Project Activity ID Should Be Greater Than 0.")]
        public int ProjectActivityID { get; set; }

        /// <summary>
        /// Task Date
        /// </summary>
        [Display(Name = "Task Date")]
        [Required(ErrorMessage = "Enter The Task Date."), DataType(DataType.Date)]
        [JsonConverter(typeof(CommonDateTimeDataTypeConverter))]
        [CommonDateTimeDataTypeValidation(ErrorMessage = "Enter The Valid Task Date in (yyyy-MM-dd) format.")]
        [TimesheetFutureDate(ErrorMessage = "Task Date Should Not Be Future Date.")]
        public DateTime TaskDate { get; set; }

        /// <summary>
        /// Task Description
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Task Description")]
        [Required(ErrorMessage = "Enter The Task Description.")]
        public string TaskDescription { get; set; }

        /// <summary>
        /// Task Description Raw
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Task Description Raw")]
        [Required(ErrorMessage = "Enter The Task Description Raw.")]
        public string TaskDescriptionRaw { get; set; }

        /// <summary>
        /// No of Hours Worked
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "No of Hours Worked")]
        [DataType(DataType.Currency)]
        [NoofHoursWorked(IsOnLeavePropertyName = "IsOnLeave", NoofHoursWorkedPropertyName = "NoofHoursWorked", ErrorMessage = "Enter The No. of Hours Worked.")]
        public string NoofHoursWorked { get; set; }

        /// <summary>
        /// Is On Leave
        /// </summary>
        //[DefaultValue(false)]
        [Display(Name = "Is On Leave")]
        public bool IsOnLeave { get; set; }

    }

    public class ProjectTimesheetSubmissionApproveRejectReportList
    {
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
    }
}
