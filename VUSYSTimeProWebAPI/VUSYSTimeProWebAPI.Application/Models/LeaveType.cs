using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class LeaveType
    {

    }

    [Table("VUSYS_LeaveTypeMaster")]
    [Keyless]
    public class LeaveTypeM : LeaveTypeSharedCommon
    {

    }

    public class LeaveTypeSharedCommon : LeaveTypeCommon, ICommonModel
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

    public class LeaveTypeCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Leave Type Id")]
        [Required(ErrorMessage = "Enter The Leave Type Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Leave Type Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Leave Type Code")]
        [Required(ErrorMessage = "Enter The Leave Type Code.")]
        public string LeaveTypeCode { get; set; }

        /// <summary>
        /// Leave Type
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Leave Type")]
        [Required(ErrorMessage = "Enter The Leave Type.")]
        public string LeaveType { get; set; }

        /// <summary>
        /// Leave Type Description
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Leave Type Description")]
        [Required(ErrorMessage = "Enter The Leave Type Description.")]
        public string LeaveTypeDescription { get; set; }

        /// <summary>
        /// Default Leave Count
        /// </summary>
        [Display(Name = "Default Leave Count")]
        public decimal DefaultLeaveCount { get; set; }

        /// <summary>
        /// Is Paid Leave
        /// </summary>
        [Display(Name = "Is Paid Leave")]
        public bool IsPaidLeave { get; set; } = false;

        /// <summary>
        /// Allow Half Day
        /// </summary>
        [Display(Name = "Allow Half Day")]
        public bool AllowHalfDay { get; set; } = false;
    }

    public class LeaveTypeDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Leave Type Id")]
        [Required(ErrorMessage = "Enter The Leave Type Id.")]
        public int Id { get; set; }
    }

    public class LeaveTypeRequestParameterCommon : LeaveTypeCommon, IRequestParameterCommonModel
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
}