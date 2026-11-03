using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class LeaveApplication
    {

    }

    [Table("VUSYS_LeaveApplication")]
    [Keyless]
    public class LeaveApplicationM : LeaveApplicationSharedCommon
    {

    }

    public class LeaveApplicationSharedCommon : LeaveApplicationCommon, ICommonModel
    {
        /// <summary>
        /// Half Day Type
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Half Day Type")]
        public string HalfDayType { get; set; }

        /// <summary>
        /// Total Leave Days
        /// </summary>
        [Display(Name = "Total Leave Days")]
        [DecimalGreaterThanZeroValidation(DecimalNumberPropertyName = "TotalLeaveDays", ErrorMessage = "Total Leave Days Should Be Greater Than 0.")]
        public decimal TotalLeaveDays { get; set; }

        /// <summary>
        /// Leave Status
        /// </summary>
        [Display(Name = "Leave Status")]
        public int LeaveStatus { get; set; } = 1;

        /// <summary>
        /// Action Type
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Action Type")]
        public string ActionType { get; set; }

        /// <summary>
        /// Approver Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Approver Employee Code")]
        public string ApproverEmployeeCode { get; set; } = string.Empty;

        /// <summary>
        /// Approved Or Rejected By
        /// </summary>
        [Display(Name = "Approved Or Rejected By")]
        public int ApprovedOrRejectedBy { get; set; }

        /// <summary>
        /// Approved Or Rejected Remarks
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Approved Or Rejected Remarks")]
        public string ApprovedOrRejectedRemarks { get; set; } = string.Empty;

        /// <summary>
        /// Remarks
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Remarks")]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// Action By
        /// </summary>
        [Display(Name = "Action By")]
        public int ActionBy { get; set; }

        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; } = false;

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

    public class LeaveApplicationCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Leave Application Id")]
        [Required(ErrorMessage = "Enter The Leave Application Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Leave Type Id
        /// </summary>
        [Display(Name = "Leave Type Id")]
        [Required(ErrorMessage = "Enter The Leave Type Id.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "LeaveTypeID", ErrorMessage = "Leave Type Id Should Be Greater Than 0.")]
        public int LeaveTypeID { get; set; }

        /// <summary>
		/// From Date
		/// </summary>
		[DefaultValue("")]
        [Display(Name = "From Date")]
        [Required(ErrorMessage = "Enter The From Date.")]
        [CommonDateTimeValidation(ErrorMessage = "Enter The Valid From Date in (yyyy-MM-dd) Format.")]
        public string FromDate { get; set; }

        /// <summary>
        /// To Date
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "To Date")]
        [Required(ErrorMessage = "Enter The To Date.")]
        [CommonDateTimeValidation(ErrorMessage = "Enter The Valid To Date in (yyyy-MM-dd) Format.")]
        public string ToDate { get; set; }

        /// <summary>
        /// Is Half Day
        /// </summary>
        [Display(Name = "Is Half Day")]
        public bool IsHalfDay { get; set; } = false;

        /// <summary>
        /// Reason
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Reason")]
        [Required(ErrorMessage = "Enter The Reason.")]
        public string Reason { get; set; }
    }

    public class LeaveApplicationRequestParameterCommon : LeaveApplicationCommon, IRequestParameterCommonModel
    {
        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; } = false;

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

    public class GetTotalNoofLeaveDaysRequestParam
    {
        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Leave Type Id
        /// </summary>
        [Display(Name = "Leave Type Id")]
        [Required(ErrorMessage = "Enter The Leave Type Id.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "LeaveTypeID", ErrorMessage = "Leave Type Id Should Be Greater Than 0.")]
        public int LeaveTypeID { get; set; }

        /// <summary>
		/// From Date
		/// </summary>
		[DefaultValue("")]
        [Display(Name = "From Date")]
        [Required(ErrorMessage = "Enter The From Date.")]
        [CommonDateTimeValidation(ErrorMessage = "Enter The Valid From Date in (yyyy-MM-dd) Format.")]
        public string FromDate { get; set; }

        /// <summary>
        /// To Date
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "To Date")]
        [Required(ErrorMessage = "Enter The To Date.")]
        [CommonDateTimeValidation(ErrorMessage = "Enter The Valid To Date in (yyyy-MM-dd) Format.")]
        public string ToDate { get; set; }

        /// <summary>
        /// Is Half Day
        /// </summary>
        [Display(Name = "Is Half Day")]
        public bool IsHalfDay { get; set; } = false;
    }

    public class LeaveApplicationDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Leave Application Id")]
        public int Id { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }
    }

    public class LeaveApplicationApproveRejectCancelRequestParam
    {
        /// <summary>
        /// Leave Application Id
        /// </summary>
        [Display(Name = "Leave Application Id")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "Id", ErrorMessage = "Leave Application Id Should Be Greater Than 0.")]
        public int Id { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Action Type
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Action Type")]
        [Required(ErrorMessage = "Enter The Action Type.")]
        [RegularExpression("^(APPROVE|REJECT|CANCEL)$", ErrorMessage = "Action Type Must Be APPROVE, REJECT Or CANCEL.")]
        public string ActionType { get; set; }

        /// <summary>
        /// Action By
        /// </summary>
        [Display(Name = "Action By")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "ActionBy", ErrorMessage = "Action By Should Be Greater Than 0.")]
        public int ActionBy { get; set; }

        /// <summary>
        /// Remarks
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Remarks")]
        [Required(ErrorMessage = "Enter The Reason.")]
        public string Remarks { get; set; }
    }

    public enum LeaveStatusEnum
    {
        Pending = 1,
        Approve = 2,
        Approved = 2,
        Reject = 3,
        Rejected = 3,
        Cancelled = 4
    }
}