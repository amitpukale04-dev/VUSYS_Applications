using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class ProjectActivity
    {
    }

    [Table("VUSYS_ProjectActivity")]
    [Keyless]
    public class ProjectActivityM : CommonModel
    {
        #region [Project Activity Properties]

        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Activity Name
        /// </summary>
        [Display(Name = "Activity Name")]
        [Required(ErrorMessage = "Enter The Activity Name.")]
        public string ActivityName { get; set; }


        #endregion
    }
}
