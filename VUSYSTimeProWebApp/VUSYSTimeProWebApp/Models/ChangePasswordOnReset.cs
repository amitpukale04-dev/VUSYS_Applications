using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.Models
{
    public class ChangePasswordOnReset
    {
        #region [Change Password Properties]

        /// <summary>
        /// Email ID
        /// </summary>
        [Display(Name = "Email ID")]
        public string EmailID { get; set; }

        /// <summary>
        /// New Password
        /// </summary>
        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Enter The New Password.")]
        public string NewPassword { get; set; }

        /// <summary>
        /// Confirm Password
        /// </summary>
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Enter The Confirm Password.")]
        [Compare("NewPassword", ErrorMessage = "New Password and Confirm Password Must Match.")]
        public string ConfirmPassword { get; set; }

        #endregion [Change Password Properties]
    }

    public class ChangePassword: ChangePasswordOnReset
    {
        #region [Change Password Properties]

        /// <summary>
        /// User Name
        /// </summary>
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        /// <summary>
        /// Old Password
        /// </summary>
        [Display(Name = "Old Password")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Enter The Old Password.")]
        public string OldPassword { get; set; }


        #endregion [Change Password Properties]
    }

    public class ForgotPassword
    {
        #region [Forgot Password Properties]

        /// <summary>
		/// Email ID
		/// </summary>
		[Display(Name = "Email ID")]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [EmailAddress(ErrorMessage = "Enter The Valid Email ID.")]
        public string UserEmailID { get; set; } = "";

        /// <summary>
        /// Link URL
        /// </summary>
        public string LinkURL { get; set; } = "";

        #endregion [Forgot Password Properties]
    }
}
