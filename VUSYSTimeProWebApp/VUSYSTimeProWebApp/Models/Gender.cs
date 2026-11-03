using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class Gender
    {
    }

    [Table("VUSYS_GenderMaster")]
    [Keyless]
    public class GenderM : CommonModel
    {
        #region [Gender Properties]

        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Gender
        /// </summary>
        [Display(Name = "Gender")]
        [Required(ErrorMessage = "Enter The Gender.")]
        public string Gender { get; set; }


        #endregion
    }
}
