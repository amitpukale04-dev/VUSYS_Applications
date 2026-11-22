using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class Country
    {
    }

    [Table("VUSYS_CountryMaster")]
    [Keyless]
    public class CountryM : CommonModel
    {
        #region [Country Properties]

        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Country Code
        /// </summary>
        [Display(Name = "Country Code")]
        [Required(ErrorMessage = "Enter The Country Code.")]
        public string CountryCode { get; set; }

        /// <summary>
        /// Country Name
        /// </summary>
        [Display(Name = "Country Name")]
        [Required(ErrorMessage = "Enter The Country Name.")]
        public string CountryName { get; set; }


        #endregion
    }
}
