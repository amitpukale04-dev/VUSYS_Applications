using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class EmployeeLeaveBalance
    {

    }

    [Table("VUSYS_EmployeeLeaveBalance")]
    [Keyless]
    public class EmployeeLeaveBalanceM : EmployeeLeaveBalanceSharedCommon
    {

    }

    public class EmployeeLeaveBalanceSharedCommon : EmployeeLeaveBalanceCommon, ICommonModel
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

        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }
    }

    public class EmployeeLeaveBalanceCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Employee Leave Balance Id")]
        [Required(ErrorMessage = "Enter The Employee Leave Balance Id.")]
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
        /// Leave Year
        /// </summary>
        [Display(Name = "Leave Year")]
        [Required(ErrorMessage = "Enter The Leave Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "LeaveYear", ErrorMessage = "Leave Year Should Be Greater Than 0.")]
        public int LeaveYear { get; set; }

        /// <summary>
        /// Opening Balance
        /// </summary>
        [Display(Name = "Opening Balance")]
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// Allocated Leave
        /// </summary>
        [Display(Name = "Allocated Leave")]
        public decimal AllocatedLeave { get; set; }

        /// <summary>
        /// Used Leave
        /// </summary>
        [Display(Name = "Used Leave")]
        public decimal UsedLeave { get; set; }

        /// <summary>
        /// Pending Leave
        /// </summary>
        [Display(Name = "Pending Leave")]
        public decimal PendingLeave { get; set; }

        /// <summary>
        /// Available Balance
        /// </summary>
        [Display(Name = "Available Balance")]
        public decimal AvailableBalance { get; set; }
    }

    public class EmployeeLeaveBalanceDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Employee Leave Balance Id")]
        [Required(ErrorMessage = "Enter The Employee Leave Balance Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Leave Year
        /// </summary>
        [Display(Name = "Leave Year")]
        [Required(ErrorMessage = "Enter The Leave Year.")]
        public int LeaveYear { get; set; }
    }

    public class EmployeeLeaveBalanceRequestParameterCommon : EmployeeLeaveBalanceCommon, IRequestParameterCommonModel
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

    public class EmployeeLeaveBalanceInitializeRequestParam
    {
        /// <summary>
        /// Leave Year
        /// </summary>
        [Display(Name = "Leave Year")]
        [Required(ErrorMessage = "Enter The Leave Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "LeaveYear", ErrorMessage = "Leave Year Should Be Greater Than 0.")]
        public int LeaveYear { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        [Display(Name = "Created By")]
        [Required(ErrorMessage = "Enter The Created By.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "CreatedBy", ErrorMessage = "Created By Should Be Greater Than 0.")]
        public int CreatedBy { get; set; }
    }

    public class CurrentLeaveBalanceDetailsRequestParam
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
        /// Leave Year
        /// </summary>
        [Display(Name = "Leave Year")]
        [Required(ErrorMessage = "Enter The Leave Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "LeaveYear", ErrorMessage = "Leave Year Should Be Greater Than 0.")]
        public int LeaveYear { get; set; }
    }
}