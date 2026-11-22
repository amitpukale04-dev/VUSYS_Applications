using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
	public class State
    {

	}

    [Table("VUSYS_StateMaster")]
    public class StateM : StateSharedCommon
    {
        
    }

    public class StateSharedCommon : StateCommon, ICommonModel
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

    public class StateCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "State Id")]
        [Required(ErrorMessage = "Enter The State Id.")]
        public int Id { get; set; }

        /// <summary>
        /// State Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "State Code")]
        [Required(ErrorMessage = "Enter The State Code.")]
        public string StateCode { get; set; }

        /// <summary>
        /// State Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "State Name")]
        [Required(ErrorMessage = "Enter The State Name.")]
        public string StateName { get; set; }

        /// <summary>
        /// Country Id
        /// </summary>
        [Display(Name = "Country Id")]
        [Required(ErrorMessage = "Enter The Country Id.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "CountryID", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int CountryID { get; set; }
    }

    public class StateDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "State Id")]
        [Required(ErrorMessage = "Enter The State Id.")]
        public int Id { get; set; }
    }

    public class StateRequestParameterCommon : StateCommon, IRequestParameterCommonModel
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
