using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class EmployeeProjectMapping
    {

    }

    [Table("VUSYS_EmployeeProjectMapping")]
    //[Keyless]
    public class EmployeeProjectMappingM
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Project ID
        /// </summary>
        [Required(ErrorMessage = "Select The Project.")]
        [Display(Name = "Project")]
        public int ProjectID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [Required(ErrorMessage = "Select The Employee.")]
        [Display(Name = "Employee")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Obsolete")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Notify To Employee
        /// </summary>
        [Display(Name = "Notify To Employee")]
        public bool IsNotifyToEmployee { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        public int UpdatedBy { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Updated Date
        /// </summary>
        public DateTime UpdatedDate { get; set; }
    }

    public class EmployeeProjectMappingDataList:CommonModel
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
    }
}
