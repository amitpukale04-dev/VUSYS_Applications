using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VUSYSTimeProWebAPI.Application.Common.Models;

namespace VUSYSTimeProWebAPI.Application.Models
{
	public class Country
    {

	}

    [Table("VUSYS_CountryMaster")]
    public class CountryM : CountrySharedCommon
    {
        
    }

    public class CountrySharedCommon : CountryCommon, ICommonModel
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

    public class CountryCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Country Id")]
        [Required(ErrorMessage = "Enter The Country Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Country Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Country Code")]
        [Required(ErrorMessage = "Enter The Country Code.")]
        public string CountryCode { get; set; }

        /// <summary>
        /// Country Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Country Name")]
        [Required(ErrorMessage = "Enter The Country Name.")]
        public string CountryName { get; set; }
    }

    public class CountryDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Country Id")]
        [Required(ErrorMessage = "Enter The Country Id.")]
        public int Id { get; set; }
    }

    public class CountryRequestParameterCommon : CountryCommon, IRequestParameterCommonModel
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
