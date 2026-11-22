using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using VUSYSTimeProWebApp.CustomAttribute;

namespace VUSYSTimeProWebApp.Models
{
    public class Users
    {

    }

    [Table("VUSYS_Users")]
    public class UsersM : CommonModel
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        public int Uid { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public string UserID { get; set; } = "";

        /// <summary>
        /// User Name
        /// </summary>
        [Display(Name = "User Name")]
        [Required(ErrorMessage = "Enter The User Name.")]
        public string UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        //[DataType(DataType.Password)]
        //[Required(ErrorMessage = "Enter The Password.")]
        //[RegularExpression("(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[*+\\/|!\"£$%^&*()#[\\]@~'?><,.=-_]).{13,}", ErrorMessage = "Password must be minimum 13 characters and contain one uppercase letter, one lowercase letter, one digit and one special character.")]
        public string Password { get; set; }

        /// <summary>
        /// Password Salt
        /// </summary>
        public string PasswordSalt { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Enter The Full Name.")]
        public string FullName { get; set; } = "";

        /// <summary>
        /// Email ID
        /// </summary>
        [Display(Name = "Email ID")]
        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", ErrorMessage = "Enter The Valid Email ID.")]
        public string UserEmailID { get; set; } = "";

        /// <summary>
        /// Email ID
        /// </summary>
        public string EmailID { get; set; } = "";

        /// <summary>
        /// Mobile No
        /// </summary>
        [Display(Name = "Mobile No")]
        [MaxLength(10, ErrorMessage = "Maximum {1} Characters Allowed.")]
        [Required(ErrorMessage = "Enter The Mobile Number.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter The Valid Mobile Number.")]
        public string MobileNo { get; set; } = "";

        /// <summary>
        /// User Active
        /// </summary>
        public string UserActive { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Required(ErrorMessage = "Select The Role.")]
        [Display(Name = "Role")]
        public int RoleID { get; set; }

        /// <summary>
        /// Role Name
        /// </summary>
        [Display(Name ="Role")]
        public string RoleName { get; set; }

        /// <summary>
        /// Roles
        /// </summary>
        public string Roles { get; set; }

        /// <summary>
        /// Is Reset
        /// </summary>
        public bool IsReset { get; set; }

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

        /// <summary>
        /// Is Password Expiry Link Available
        /// </summary>
        public bool IsPasswordExpiryLinkAvailable { get; set; }

        /// <summary>
        /// Password Expiry Link Date Time
        /// </summary>
        public DateTime PasswordExpiryLinkDateTime { get; set; }

        /// <summary>
        /// User Role
        /// </summary>
        public string UserRole { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [Display(Name = "Employee")]
        [Required(ErrorMessage = "Select The Employee.")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Login Date
        /// </summary>
        public DateTime LoginDate { get; set; }

        /// <summary>
        /// Logout Date
        /// </summary>
        public DateTime LogoutDate { get; set; }

        /// <summary>
        /// Last Login Date
        /// </summary>
        public DateTime LastLoginDate { get; set; }

        /// <summary>
        /// Profile User Image
        /// </summary>
        [Display(Name = "User Image")]
        [FileExtensionValidation(fileExtensions = "png,jpg,jpeg", ErrorMessage = "Upload Only Valid PNG/JPG/JPEG Image File.")]
        [NotMapped]
        [XmlIgnore]
        public IFormFile ProfileUserImage { get; set; }

        /// <summary>
        /// Image File Name
        /// </summary>
        public string UserImageFileName { get; set; }

        /// <summary>
        /// User Image
        /// </summary>
        public Byte[] UserImage { get; set; }

        /// <summary>
        /// Last Logout Date
        /// </summary>
        public string LastLogoutDate { get; set; }

        /// <summary>
        /// Remember Me
        /// </summary>
        public bool RememberMe { get; set; }

        /// <summary>
        /// IsSuperAdmin
        /// </summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        /// Is User
        /// </summary>
        public bool IsUser { get; set; }

        /// <summary>
        /// Is Email Verified
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// Plain Password
        /// </summary>
        public string PlainPassword { get; set; }

        /// <summary>
        /// Link URL
        /// </summary>
        public string LinkURL { get; set; }
    }

    public class UserLoggedInInfo
    {
        /// <summary>
        /// Full Name
        /// </summary>
        public string FullName { get; set; } = "";

        /// <summary>
        /// User Profile Image
        /// </summary>
        public string UserProfileImage { get; set; } = "";

        /// <summary>
        /// Last Logout Date
        /// </summary>
        public string LastLogoutDate { get; set; }

        /// <summary>
        /// User Image
        /// </summary>
        public Byte[] UserImage { get; set; }
    }

    public class EmployeeUserInfo
    {
        /// <summary>
        /// Full Name
        /// </summary>
        public string FullName { get; set; } = "";

        /// <summary>
        /// Mobile No
        /// </summary>
        public string MobileNo { get; set; } = "";

        /// <summary>
        /// Email ID
        /// </summary>
        public string UserEmailID { get; set; } = "";
    }

    [Table("VUSYS_Users_Token")]
    public class UserToken
    {
        /// <summary>
        /// ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// User Access Token
        /// </summary>
        public string UserAccessToken { get; set; }

        /// <summary>
        /// Is Deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        public int UpdatedBy { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Updated Date
        /// </summary>
        public DateTime UpdatedDate { get; set; }
    }
}
