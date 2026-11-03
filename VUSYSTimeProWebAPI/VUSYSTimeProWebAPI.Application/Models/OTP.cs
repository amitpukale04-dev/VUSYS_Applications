using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class OTP
    {
        /// <summary>
        /// Email ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [CommonValidation(CustomPropertyName = "EmailID", ValidationTypePropertyName = "Email", ErrorMessage = "Enter The Valid Email ID.")]
        public string EmailID { get; set; }
    }
}
