using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace VUSYSAppHub.Models
{
    public class ConfigureEmail
    {
        /// <summary>
        /// Display Name
        /// </summary>
        public string DisplayName { get; set; }

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
        /// Display Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// From Email ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "From Email ID")]
        public string FromEmailID { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        /// <summary>
        /// Smtp Host
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Smtp Host")]
        public string SmtpHost { get; set; }

        /// <summary>
        /// Port No
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Port No")]
        public string PortNo { get; set; }

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
        /// Reply To Emails
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Reply To Emails")]
        public string ReplyToEmails { get; set; }

        /// <summary>
        /// Reply To Display Names
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Reply To Display Names")]
        public string ReplyToDisplayNames { get; set; }

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

        /// <summary>
        /// Folder Path
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Folder Path")]
        public string FolderPath { get; set; }

        /// <summary>
        /// File Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "File Name")]
        public string FileName { get; set; }

        /// <summary>
        /// Is Multiple Files
        /// </summary>
        [Display(Name = "Is Multiple Files")]
        public bool IsMultipleFiles { get; set; }

        /// <summary>
        /// Has Attachment
        /// </summary>
        [Display(Name = "Has Attachment")]
        public bool HasAttachment { get; set; }

        /// <summary>
        /// attachments
        /// </summary>
        public IEnumerable<Attachment>? attachments { get; set; }
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
