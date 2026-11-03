using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class ProjectActivity
    {
    }

    [Table("VUSYS_ProjectActivity")]
    [Keyless]
    public class ProjectActivityM : ProjectActivitySharedCommon
    {

    }

    public class ProjectActivitySharedCommon : ProjectActivityCommon, ICommonModel
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

    public class ProjectActivityCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Project Activity Id")]
        [Required(ErrorMessage = "Enter The Project Activity Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Activity Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Activity Name")]
        [Required(ErrorMessage = "Enter The Activity Name.")]
        public string ActivityName { get; set; }
    }

    public class ProjectActivityDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Project Activity Id")]
        [Required(ErrorMessage = "Enter The Project Activity Id.")]
        public int Id { get; set; }
    }

    public class ProjectActivityRequestParameterCommon : ProjectActivityCommon, IRequestParameterCommonModel
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
