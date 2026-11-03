using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class Project
    {
    }

    [Table("VUSYS_Projects")]
    [Keyless]
    public class ProjectM : ProjectSharedCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
    }

    public class ProjectSharedCommon : ProjectCommon, ICommonModel
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

    public class ProjectCommon
    {
        /// <summary>
        /// Project ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Project ID")]
        [Required(ErrorMessage = "Enter The Project ID.")]
        public int ProjectID { get; set; }

        /// <summary>
        /// Project Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Project Code")]
        [Required(ErrorMessage = "Enter The Project Code.")]
        public string ProjectCode { get; set; }

        /// <summary>
        /// Client Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Client Name")]
        [Required(ErrorMessage = "Enter The Client Name.")]
        public string ClientName { get; set; }

        /// <summary>
        /// Project Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Project Name")]
        [Required(ErrorMessage = "Enter The Project Name.")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Project Description
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Project Description")]
        [Required(ErrorMessage = "Enter The Project Description.")]
        public string ProjectDescription { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        [Display(Name = "Start Date")]
        [Required(ErrorMessage = "Enter The Start Date."), DataType(DataType.Date)]
        //[JsonConverter(typeof(CommonDateTimeDataTypeConverter))]
        [CommonDateTimeDataTypeValidation(ErrorMessage = "Enter The Valid Start Date in (yyyy-MM-dd) format.")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End Date
        /// </summary>
        [Display(Name = "End Date")]
        [Required(ErrorMessage = "Enter The End Date."), DataType(DataType.Date)]
        [JsonConverter(typeof(CommonDateTimeDataTypeConverter))]
        [CommonDateTimeDataTypeValidation(ErrorMessage = "Enter The Valid End Date in (yyyy-MM-dd) format.")]
        [DateTimeDataTypeRangeValidation("StartDate","yyyy-MM-dd","End Date Should Be Greater Than Start Date.")]
        public DateTime EndDate { get; set; }

        /// <summary>
		/// Allocated Hours
		/// </summary>
		[Display(Name = "Allocated Hours")]
        [DataType(DataType.Currency)]
        [Required(ErrorMessage = "Enter The Allocated Hours.")]
        public decimal AllocatedHours { get; set; }

        /// <summary>
		/// Consumed Hours
		/// </summary>
		[Display(Name = "Consumed Hours")]
        [DataType(DataType.Currency)]
        [Required(ErrorMessage = "Enter The Consumed Hours.")]
        public decimal ConsumedHours { get; set; }

        /// <summary>
        /// Project Status
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Project Status")]
        [Required(ErrorMessage = "Enter The Project Status.")]
        public string ProjectStatus { get; set; }
    }

    public class ProjectRequestParameterCommon : ProjectCommon, IRequestParameterCommonModel
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

    public class ProjectDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Project Id")]
        [Required(ErrorMessage = "Enter The Project Id.")]
        public int Id { get; set; }
    }
}
