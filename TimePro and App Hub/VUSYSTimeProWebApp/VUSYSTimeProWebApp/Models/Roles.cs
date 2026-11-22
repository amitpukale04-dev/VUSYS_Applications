using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class Roles
    {
    }

    [Table("VUSYS_RoleMaster")]
    [Keyless]
    public class RolesM:CommonModel
    {
        #region [Roles Properties]

        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Role
        /// </summary>
        [Display(Name = "Role Name")]
        [Required(ErrorMessage = "Enter The Role Name.")]
        public string RoleName { get; set; }


        #endregion
    }
}
