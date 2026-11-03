using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mail;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    [Table("VUSYS_EmailConfiguration")]
    public class ConfigureEmailM : ConfigureEmailSharedCommon
    {
        /// <summary>
        /// Display Name
        /// </summary>
        public string DisplayName { get; set; }
    }

    public class ConfigureEmailSharedCommon : ConfigureEmail, ICommonModel
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

    public class ConfigureEmailRequestParameterCommon : ConfigureEmail, IRequestParameterCommonModel
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

    public class ConfigureEmail
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Email Configuration Id")]
        [Required(ErrorMessage = "Enter The Email Configuration Id.")]
        public int Id { get; set; }

        /// <summary>
        /// From Email ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [CommonValidation(CustomPropertyName = "FromEmail", ValidationTypePropertyName = "Email", ErrorMessage = "Enter The Valid Email ID.")]
        public string FromEmail { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        /// <summary>
        /// SMTP Host
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Smtp Host")]
        [Required(ErrorMessage = "Enter The Smtp Host.")]
        public string SmtpHost { get; set; }

        /// <summary>
        /// Port No
        /// </summary>
        [Display(Name = "Port No.")]
        [Required(ErrorMessage = "Enter The Port No.")]
        public int PortNo { get; set; }
    }

    public class ConfigureEmailDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Email Configuration Id")]
        [Required(ErrorMessage = "Enter The Email Configuration Id.")]
        public int Id { get; set; }
    }

    public class SendEmailRequestParam
    {
        /// <summary>
        /// From Display Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "From Display Name")]
        public string FromDisplayName { get; set; }

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

    public class EmailAttachment
    {
        /// <summary>
        /// File Name
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// File Bytes
        /// </summary>
        public byte[] FileBytes { get; set; }
        
        /// <summary>
        /// Content Type
        /// </summary>
        public string ContentType { get; set; }
    }
}
