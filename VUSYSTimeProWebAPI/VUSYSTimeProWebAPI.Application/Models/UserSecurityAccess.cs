using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class UserSecurityAccess : RequestParameterCommonModel
    {
        /// <summary>
        /// Security Access Id
        /// </summary>
        [Display(Name = "Security Access Id")]
        [Required(ErrorMessage = "Enter The Security Access Id.")]
        public int SecurityAccessId { get; set; }

        /// <summary>
        /// Security ID
        /// </summary>
        [Display(Name = "Security ID")]
        [Required(ErrorMessage = "Enter The Security ID.")]
        public int SecurityID { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Display(Name = "Role ID")]
        [Required(ErrorMessage = "Enter The Role ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "RoleID", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int RoleID { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        [Display(Name = "User ID")]
        [Required(ErrorMessage = "Enter The User ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "UserID", ErrorMessage = "User ID Should Be Greater Than 0.")]
        public int UserID { get; set; }
    }
}
