using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class ConfigureEmail
    {
    }

    [Table("VUSYS_EmailConfiguration")]
    public class ConfigureEmailM:CommonModel
    {
		#region [Configure Email Properties]

		/// <summary>
		/// Id
		/// </summary>
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// From Email
        /// </summary>
        [Display(Name = "Email ID")]
        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", ErrorMessage = "Enter The Valid Email ID.")]
        public string FromEmail { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [Required(ErrorMessage = "Enter The Password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }


        /// <summary>
        /// SMTP Host
        /// </summary>
        [Display(Name = "Smtp Host")]
        [Required(ErrorMessage = "Enter The Smtp Host.")]
        public string SmtpHost { get; set; }

        /// <summary>
        /// Port No
        /// </summary>
        [Display(Name = "Port No.")]
        [Required(ErrorMessage = "Enter The Port No.")]
        public int PortNo { get; set; }

        #endregion
    }
}
