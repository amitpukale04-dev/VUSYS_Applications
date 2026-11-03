using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VUSYSAppHub.Models
{
    public class ConfigureEmail
    {
        /// <summary>
        /// From Email ID
        /// </summary>
        public string FromEmailID { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// SmtpHost
        /// </summary>
        public string SmtpHost { get; set; }

        /// <summary>
        /// PortNo
        /// </summary>
        public string PortNo { get; set; }
    }

    public class SendEmailRequestParam
    {
        /// <summary>
        /// To Email
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "To Email")]
        [Required(ErrorMessage = "Enter The To Email.")]
        [EmailAddress(ErrorMessage = "Enter The Valid To Email ID.")]
        public string ToEmail { get; set; }

        /// <summary>
        /// CC Email
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "CC Email")]
        public string CCEmail { get; set; }

        /// <summary>
        /// BCC Email
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "BCC Email")]
        public string BCCEmail { get; set; }

        /// <summary>
        /// Email Subject
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email Subject")]
        [Required(ErrorMessage = "Enter The Email Subject.")]
        public string EmailSubject { get; set; }

        /// <summary>
        /// Email Body
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email Body")]
        [Required(ErrorMessage = "Enter The Email Body.")]
        public string EmailBody { get; set; }
    }

    public class SendEmailResponseParam
    {
        /// <summary>
        /// Email Sent
        /// </summary>
        public bool EmailSent { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }
}
