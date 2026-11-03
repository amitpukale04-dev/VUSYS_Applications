using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class RoleMenuAccessFrontEnd : RequestParameterCommonModel
    {
        /// <summary>
        /// Role Menu Access Front End Id
        /// </summary>
        [Display(Name = "Role Menu Access Front End Id")]
        [Required(ErrorMessage = "Enter The Role Menu Access Front End Id.")]
        public int RoleMenuAccessFrontEndId { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Display(Name = "Role ID")]
        [Required(ErrorMessage = "Enter The Role ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "RoleID", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int RoleID { get; set; }

        /// <summary>
        /// Security ID
        /// </summary>
        [Display(Name = "Security ID")]
        [Required(ErrorMessage = "Enter The Security ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "SecurityID", ErrorMessage = "Security ID Should Be Greater Than 0.")]
        public int SecurityID { get; set; }
    }
}
