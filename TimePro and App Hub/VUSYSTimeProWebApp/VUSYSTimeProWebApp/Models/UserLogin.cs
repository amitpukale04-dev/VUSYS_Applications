using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.Models
{
    public class UserLogin
    {
        #region [User Login Properties]

		/// <summary>
		/// Email ID
		/// </summary>
		[Display(Name = "Email ID")]
		[Required(ErrorMessage = "Enter The Email ID.")]
		[EmailAddress(ErrorMessage = "Enter The Valid Email ID.")]
		public string UserEmailID { get; set; } = "";

		/// <summary>
		/// Password
		/// </summary>
		[Required(ErrorMessage = "Enter The Password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Remember Me
        /// </summary>
        [Display(Name ="Remember Me")]
        public bool RememberMe { get; set; }

        #endregion
    }
}
