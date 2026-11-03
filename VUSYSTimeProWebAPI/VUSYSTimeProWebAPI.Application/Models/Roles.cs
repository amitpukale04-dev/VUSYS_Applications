using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VUSYSTimeProWebAPI.Application.Common.Models;

namespace VUSYSTimeProWebAPI.Application.Models
{
	public class Roles
    {

	}

    [Table("VUSYS_RoleMaster")]
    public class RolesM : RolesSharedCommon
    {
        
    }

    public class RolesSharedCommon : RoleCommon, ICommonModel
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

    public class RoleCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Role Id")]
        [Required(ErrorMessage = "Enter The Role Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Role Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Role Name")]
        [Required(ErrorMessage = "Enter The Role Name.")]
        public string RoleName { get; set; }
    }

    public class RoleDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Role Id")]
        [Required(ErrorMessage = "Enter The Role Id.")]
        public int Id { get; set; }
    }

    public class RoleRequestParameterCommon : RoleCommon, IRequestParameterCommonModel
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
