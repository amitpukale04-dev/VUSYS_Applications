using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
	public class Users
    {

	}

    [Table("VUSYS_Users")]
    public class UsersM : UserSharedCommon
    {
        /// <summary>
        /// Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        /// <summary>
        /// Password Salt
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Password Salt")]
        public string PasswordSalt { get; set; }

        /// <summary>
        /// Email ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email ID")]
        public string EmailID { get; set; }

        /// <summary>
        /// Is Reset
        /// </summary>
        public bool IsReset { get; set; }

        /// <summary>
        /// Is Email Verified
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// Plain Password
        /// </summary>
        public string PlainPassword { get; set; }

        /// <summary>
        /// Is Password Expiry Link Available
        /// </summary>
        public bool IsPasswordExpiryLinkAvailable { get; set; }

        /// <summary>
        /// Password Expiry Link Date Time
        /// </summary>
        public DateTime PasswordExpiryLinkDateTime { get; set; }

        /// <summary>
        /// Image File Name
        /// </summary>
        public string UserImageFileName { get; set; }

        /// <summary>
        /// User Image
        /// </summary>
        public Byte[] UserImage { get; set; }
    }

    public class UserSharedCommon : UserCommon, ICommonModel
    {
        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

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

    public class UserCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "User Id")]
        [Required(ErrorMessage = "Enter The User Id.")]
        public int Id { get; set; }

        /// <summary>
        /// User Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "User Name")]
        [Required(ErrorMessage = "Enter The User Name.")]
        public string UserName { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Enter The Full Name.")]
        public string FullName { get; set; }

        /// <summary>
        /// Mobile Number
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Mobile Number")]
        [CommonValidation(CustomPropertyName = "MobileNo", ValidationTypePropertyName = "MobileNumber", ErrorMessage = "Enter The Valid Mobile Number.")]
        public string MobileNo { get; set; }

        /// <summary>
        /// User Email ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [CommonValidation(CustomPropertyName = "UserEmailID", ValidationTypePropertyName = "Email", ErrorMessage = "Enter The Valid Email ID.")]
        public string UserEmailID { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Display(Name = "Role ID")]
        [Required(ErrorMessage = "Enter The Role ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "RoleID", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int RoleID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        [Required(ErrorMessage = "Enter The Employee Code.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Is Approver
        /// </summary>
        [Display(Name = "Is Approver?")]
        [IsApproverValidation(IsApproverPropertyName = "IsApprover", UserIDPropertyName = "Id", ErrorMessage = "Only One Approver Is Allowed At a Time. Please Deactivate Or Modify the Current approver And Try Again.")]
        public bool IsApprover { get; set; }

        /// <summary>
        /// Is Enable Two Factor Authentication
        /// </summary>
        [Display(Name = "Enable Two Factor Authentication")]
        public bool IsEnableTwoFactorAuthentication { get; set; }
    }

    public class UserDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "User Id")]
        [Required(ErrorMessage = "Enter The User Id.")]
        public int Id { get; set; }
    }

    public class UserRequestParameterCommon : UserCommon, IRequestParameterCommonModel
    {
        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

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

    public class UserLogin: UserLoginCommon
    {
        /// <summary>
        /// Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Password")]
		[Required(ErrorMessage = "Enter The Password.")]
		public string Password { get; set; }
	}

	public class UserLoginCommon
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

    public class SchedulerUserLogin
    {
        /// <summary>
        /// Login ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Login ID")]
        [Required(ErrorMessage = "Enter The Login ID.")]
        public string LoginID { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Password")]
        [Required(ErrorMessage = "Enter The Password.")]
        public string Password { get; set; }
    }

    public class UserMenuAccessRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "User Id")]
        [Required(ErrorMessage = "Enter The User Id.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "UserId", ErrorMessage = "User ID Should Be Greater Than 0.")]
        public int UserId { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Display(Name = "Role ID")]
        [Required(ErrorMessage = "Enter The Role ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "RoleID", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int RoleID { get; set; }

        /// <summary>
        /// Param Type
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Param Type")]
        [Required(ErrorMessage = "Enter The Param Type(Menu/Action).")]
        public string ParamType { get; set; }
    }

    public class ChangePassword : UserLoginCommon
    {
        /// <summary>
        /// Old Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Old Password")]
        [Required(ErrorMessage = "Enter The Old Password.")]
        public string OldPassword { get; set; }

        /// <summary>
        /// New Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "New Password")]
        [Required(ErrorMessage = "Enter The New Password.")]
        [CommonValidation(CustomPropertyName = "NewPassword", ValidationTypePropertyName = "Password", ErrorMessage = "New Password must be minimum 8 characters and contain one uppercase letter, one lowercase letter, one digit and one special character.")]
        public string NewPassword { get; set; }

        /// <summary>
        /// Confirm Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Enter The Confirm Password.")]
        [Compare("NewPassword", ErrorMessage = "New Password and Confirm Password Must Match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPassword
    {
        /// <summary>
        /// Email ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [CommonValidation(CustomPropertyName = "EmailID", ValidationTypePropertyName = "Email", ErrorMessage = "Enter The Valid Email ID.")]
        public string EmailID { get; set; }

        /// <summary>
        /// New Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "New Password")]
        [Required(ErrorMessage = "Enter The New Password.")]
        [CommonValidation(CustomPropertyName = "NewPassword", ValidationTypePropertyName = "Password", ErrorMessage = "New Password must be minimum 8 characters and contain one uppercase letter, one lowercase letter, one digit and one special character.")]
        public string NewPassword { get; set; }

        /// <summary>
        /// Confirm Password
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Enter The Confirm Password.")]
        [Compare("NewPassword", ErrorMessage = "New Password and Confirm Password Must Match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ForgotPassword : UserLoginCommon
    {
        
    }

    public class CheckPasswordLinkAvailableRequestParam
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

    public class TokenRefreshRequest
    {
        /// <summary>
        /// Refresh Token
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Refresh Token")]
        [Required(ErrorMessage = "Enter The Refresh Token.")]
        public string RefreshToken { get; set; }
    }

    public class EncryptTextRequest
    {
        /// <summary>
        /// Text
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Text")]
        [Required(ErrorMessage = "Enter The Text.")]
        public string Text { get; set; }

        /// <summary>
        /// Encrypt Decrypt Key
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "EncryptDecryptKey")]
        [Required(ErrorMessage = "Enter The Encrypt/Decrypt Key.")]
        public string EncryptDecryptKey { get; set; }
    }

    public class EncryptTextResponse
    {
        /// <summary>
        /// Plain Text
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "PlainText")]
        public string PlainText { get; set; }

        /// <summary>
        /// Encrypted Text
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Encrypted Text")]
        public string EncryptedText { get; set; }
    }

    public class DecryptTextRequest
    {
        /// <summary>
        /// Encrypted Text
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Encrypted Text")]
        [Required(ErrorMessage = "Enter The Encrypted Text.")]
        public string EncryptedText { get; set; }

        /// <summary>
        /// Encrypt Decrypt Key
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "EncryptDecryptKey")]
        [Required(ErrorMessage = "Enter The Encrypt/Decrypt Key.")]
        public string EncryptDecryptKey { get; set; }
    }

    public class DecryptTextResponse
    {
        /// <summary>
        /// Decrypted Text
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Decrypted Text")]
        public string DecryptedText { get; set; }

        /// <summary>
        /// Encrypted Text
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Encrypted Text")]
        public string EncryptedText { get; set; }
    }
}
