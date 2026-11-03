using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class EmployeeProjectMapping
    {
    }

    [Table("VUSYS_EmployeeProjectMapping")]
    public class EmployeeProjectMappingM : EmployeeProjectMappingSharedCommon
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

    public class EmployeeProjectMappingSharedCommon : EmployeeProjectMappingCommon
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

    public class EmployeeProjectMappingCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Employee Project Mapping ID")]
        [Required(ErrorMessage = "Enter The Employee Project Mapping ID.")]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Project ID
        /// </summary>
        [Display(Name = "Project ID")]
        [Required(ErrorMessage = "Enter The Project ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "ProjectID", ErrorMessage = "Project ID Should Be Greater Than 0.")]
        public int ProjectID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Is Notify To Employee
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsNotifyToEmployee { get; set; }
    }

    public class EmployeeProjectMappingRequestParameterCommon : EmployeeProjectMappingCommon, IRequestParameterCommonModel
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

    public class EmployeeProjectMappingDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Employee Project Mapping ID")]
        [Required(ErrorMessage = "Enter The Employee Project Mapping ID.")]
        public int Id { get; set; }

        /// <summary>
        /// Project ID
        /// </summary>
        [Display(Name = "Project ID")]
        [Required(ErrorMessage = "Enter The Project ID.")]
        public int ProjectID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }
    }

    public class EmployeeProjectMappingDataList : CommonModel
    {
        /// <summary>
        /// Employee Project Mapping ID
        /// </summary>
        public int EmployeeProjectMappingID { get; set; } = 0;

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
        /// Project Description
        /// </summary>
        public string ProjectDescription { get; set; }

        /// <summary>
        /// Client Name
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// End Date
        /// </summary>
        public string EndDate { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Employee Name
        /// </summary>
        public string EmployeeName { get; set; }

        /// <summary>
        /// Employee Email ID
        /// </summary>
        public string EmployeeEmailID { get; set; }

        /// <summary>
        /// Is Notify To Employee
        /// </summary>
        public bool IsNotifyToEmployee { get; set; }

        /// <summary>
        /// Active Status
        /// </summary>
        public string ActiveStatus { get; set; }
    }
}
