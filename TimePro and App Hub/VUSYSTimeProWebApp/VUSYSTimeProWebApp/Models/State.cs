using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class State
    {
    }

    [Table("VUSYS_StateMaster")]
    [Keyless]
    public class StateM : CommonModel
    {
        #region [State Properties]

        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// State Code
        /// </summary>
        [Display(Name = "State Code")]
        [Required(ErrorMessage = "Enter The State Code.")]
        public string StateCode { get; set; }

        /// <summary>
        /// State Name
        /// </summary>
        [Display(Name = "State Name")]
        [Required(ErrorMessage = "Enter The State Name.")]
        public string StateName { get; set; }

        /// <summary>
        /// Country Id
        /// </summary>
        [Display(Name = "Country")]
        [Required(ErrorMessage = "Select The Country.")]
        public int CountryID { get; set; } = 0;

		/// <summary>
		/// Country Name
		/// </summary>
		public string CountryName { get; set; }

		#endregion
	}
}
