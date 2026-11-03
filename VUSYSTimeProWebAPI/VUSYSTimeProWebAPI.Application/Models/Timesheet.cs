using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class Timesheet
    {
    }

    public class LogTimesheetListRequestParam
    {
        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        [Display(Name = "Project Time Sheet ID")]
        [Required(ErrorMessage = "Enter The Project Time Sheet ID.")]
        public int ProjectTimeSheetID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// From Date
        /// </summary>
        [Display(Name = "From Date")]
        [Required(ErrorMessage = "Enter The From Date."), DataType(DataType.Date)]
        [CommonDateTimeDataTypeValidation(ErrorMessage = "Enter The Valid From Date in (yyyy-MM-dd) format.")]
        public DateTime FromDate { get; set; }

        /// <summary>
        /// To Date
        /// </summary>
        [Display(Name = "To Date")]
        [Required(ErrorMessage = "Enter The To Date."), DataType(DataType.Date)]
        [JsonConverter(typeof(CommonDateTimeDataTypeConverter))]
        [CommonDateTimeDataTypeValidation(ErrorMessage = "Enter The Valid To Date in (yyyy-MM-dd) format.")]
        [DateTimeDataTypeRangeValidation("FromDate", "yyyy-MM-dd", "To Date Should Be Greater Than Start Date.")]
        public DateTime ToDate { get; set; }
    }

    public class GetTimesheetDetailsRequestParam
    {
        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        [Display(Name = "Project Time Sheet ID")]
        [Required(ErrorMessage = "Enter The Project Time Sheet ID.")]
        public int ProjectTimeSheetID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Task Date
        /// </summary>
        [Display(Name = "Task Date")]
        [Required(ErrorMessage = "Enter The Task Date."), DataType(DataType.Date)]
        [CommonDateTimeDataTypeValidation(ErrorMessage = "Enter The Valid Task Date in (yyyy-MM-dd) format.")]
        public DateTime TaskDate { get; set; }

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

    public class TimesheetSubmissionRequestParam : TimesheetReportRequestParam
    {
        /// <summary>
        /// Param Type
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Param Type")]
        [Required(ErrorMessage = "Enter The Param Type (GetEmployeeProjectTimesheetForReport/GetEmployeeProjectTimesheetForSubmission)")]
        public string ParamType { get; set; }
    }

    public class ProjectTimesheetApproveRejection : TimesheetReportRequestParam
    {
        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        [Display(Name = "Project Time Sheet ID")]
        [Required(ErrorMessage = "Enter The Project Time Sheet ID.")]
        public int ProjectTimeSheetID { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Action")]
        [Required(ErrorMessage = "Enter The Action(View/Export).")]
        public string Action { get; set; }
    }

    public class CheckTimesheetApprovalStatusRequestParam : TimesheetReportRequestParam
    {
        
    }

    public class TimesheetSubmitForApprovalRequestParam : TimesheetReportRequestParam
    {
        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        [Display(Name = "Project Time Sheet ID")]
        [Required(ErrorMessage = "Enter The Project Time Sheet ID.")]
        public int ProjectTimeSheetID { get; set; }

        /// <summary>
        /// Approve Status
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Approve Status")]
        [Required(ErrorMessage = "Enter The Approve Status.")]
        public string ApproveStatus { get; set; }
    }

    public class TimesheetApproveRejectRequestParam : TimesheetReportRequestParam
    {
        /// <summary>
        /// Project Time Sheet ID
        /// </summary>
        [Display(Name = "Project Time Sheet ID")]
        [Required(ErrorMessage = "Enter The Project Time Sheet ID.")]
        public int ProjectTimeSheetID { get; set; }

        /// <summary>
        /// Remarks
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Remarks")]
        [Required(ErrorMessage = "Enter The Remarks.")]
        public string Remarks { get; set; }

        /// <summary>
        /// User Token
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "User Token")]
        public string UserToken { get; set; }

        /// <summary>
        /// Source
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Source")]
        public string Source { get; set; }

        /// <summary>
        /// Approve Status
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Approve Status")]
        [Required(ErrorMessage = "Enter The Approve Status.")]
        public string ApproveStatus { get; set; }
    }
}
