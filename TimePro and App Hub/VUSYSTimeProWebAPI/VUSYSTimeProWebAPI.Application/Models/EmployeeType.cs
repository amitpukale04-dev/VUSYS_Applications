using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VUSYSTimeProWebAPI.Application.Common.Models;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class EmployeeType
    {
    }

    [Table("VUSYS_EmployeeTypeMaster")]
    public class EmployeeTypeM : EmployeeTypeSharedCommon
    {
        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Updated Date
        /// </summary>
        public DateTime UpdatedDate { get; set; }

    }

    public class EmployeeTypeSharedCommon : EmployeeTypeCommon
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

    public class EmployeeTypeCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Employee Type ID")]
        [Required(ErrorMessage = "Enter The Employee Type ID.")]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Employee Type
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Type")]
        [Required(ErrorMessage = "Enter The Employee Type.")]
        public string EmployeeType { get; set; }
    }

    public class EmployeeTypeRequestParameterCommon : EmployeeTypeCommon, IRequestParameterCommonModel
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

    public class EmployeeTypeDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Employee Type Id")]
        [Required(ErrorMessage = "Enter The Employee Type Id.")]
        public int Id { get; set; }
    }

    public class EmployeeTypeDataList : CommonModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// Employee Type
        /// </summary>
        public string EmployeeType { get; set; }

        /// <summary>
        /// Active Status
        /// </summary>
        public string ActiveStatus { get; set; }
    }
}
