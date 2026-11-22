using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class Report
    {
    }

    public class TimesheetReportRequestParam
    {
        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Timesheet Month
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Timesheet Month")]
        [Required(ErrorMessage = "Enter The Timesheet Month.")]
        public string TimesheetMonth { get; set; }

        /// <summary>
        /// Timesheet Year
        /// </summary>
        [Display(Name = "Timesheet Year")]
        [Required(ErrorMessage = "Enter The Timesheet Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "TimesheetYear", ErrorMessage = "Timesheet Year Should Be Greater Than 0.")]
        public int TimesheetYear { get; set; }
    }

    public class DownloadTimesheetReportRequestParam
    {
        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Timesheet Month
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Timesheet Month")]
        [Required(ErrorMessage = "Enter The Timesheet Month.")]
        public string TimesheetMonth { get; set; }

        /// <summary>
        /// Timesheet Year
        /// </summary>
        [Display(Name = "Timesheet Year")]
        [Required(ErrorMessage = "Enter The Timesheet Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "TimesheetYear", ErrorMessage = "Timesheet Year Should Be Greater Than 0.")]
        public int TimesheetYear { get; set; }
    }

    public class ProjectAllocatedConsumedReportRequestParam
    {
        /// <summary>
        /// Project ID
        /// </summary>
        [Display(Name = "Project ID")]
        [Required(ErrorMessage = "Enter The Project ID.")]
        public int ProjectID { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Action")]
        [Required(ErrorMessage = "Enter The Action(Search/Export).")]
        public string Action { get; set; }
    }

    public class EmployeeLeaveReportRequestParam
    {
        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Leave Month
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Leave Month")]
        [Required(ErrorMessage = "Enter The Leave Month.")]
        public string LeaveMonth { get; set; }

        /// <summary>
        /// Leave Year
        /// </summary>
        [Display(Name = "Leave Year")]
        [Required(ErrorMessage = "Enter The Leave Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "LeaveYear", ErrorMessage = "Leave Year Should Be Greater Than 0.")]
        public int LeaveYear { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Action")]
        [Required(ErrorMessage = "Enter The Action(Search/Export).")]
        public string Action { get; set; }
    }
}
