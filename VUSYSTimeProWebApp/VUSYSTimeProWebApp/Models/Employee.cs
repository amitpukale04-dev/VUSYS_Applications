using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using VUSYSTimeProWebApp.CustomAttribute;

namespace VUSYSTimeProWebApp.Models
{
    public class Employee
    {
    }

    [Table("VUSYS_Employees")]
    [Keyless]
    public class EmployeeM:CommonModel
    {
        #region [Employee Properties]

        /// <summary>
        /// Employee ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeID { get; set; } = 0;

        /// <summary>
        /// Employee Code
        /// </summary>
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Enter The Full Name.")]
        public string FullName { get; set; }

        /// <summary>
        /// Gender ID
        /// </summary>
        [Required(ErrorMessage = "Select The Gender.")]
        [Display(Name = "Gender")]
        public int GenderID { get; set; }

        /// <summary>
        /// Gender Name
        /// </summary>
        public string GenderName { get; set; }

        /// <summary>
        /// Date of Birth
        /// </summary>
        [Required(ErrorMessage = "Select The Date of Birth."), DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        //[DateofBirthValidation()]
        [NoFutureDate(ErrorMessage = "Date of Birth Should Not Be Future Date.")]
        [Display(Name = "Date of Birth")]
        public DateTime DateofBirth { get; set; }

        /// <summary>
        /// Display Date of Birth
        /// </summary>
        public string DisplayDateofBirth { get; set; }

        /// <summary>
        /// Mobile No
        /// </summary>
        [Display(Name = "Mobile No")]
        [MaxLength(10, ErrorMessage = "Maximum {1} Characters Allowed.")]
        [Required(ErrorMessage = "Enter The Mobile Number.")]
        [CommonValidation(CustomPropertyName = "MobileNo", ValidationTypePropertyName = "MobileNumber", ErrorMessage = "Enter The Valid Mobile Number.")]
        public string MobileNo { get; set; }

        /// <summary>
        /// Email ID
        /// </summary>
        [Display(Name = "Email ID")]
        [EmailAddress(ErrorMessage = "Enter The Valid Email ID.")]
        [Required(ErrorMessage = "Enter The Email ID.")]
        public string EmailID { get; set; }

        /// <summary>
        /// Country ID
        /// </summary>
        [Required(ErrorMessage = "Select The Country.")]
        [Display(Name = "Country")]
        public int CountryID { get; set; }

        /// <summary>
        /// Country Name
        /// </summary>
        public string CountryName { get; set; }

        /// <summary>
        /// State ID
        /// </summary>
        [Required(ErrorMessage = "Select The State.")]
        [Display(Name = "State")]
        public int StateID { get; set; }

        /// <summary>
        /// State Name
        /// </summary>
        public string StateName { get; set; }

        /// <summary>
        /// City Name
        /// </summary>
        [Display(Name = "City")]
        [Required(ErrorMessage = "Enter The City.")]
        public string CityName { get; set; }

        /// <summary>
        /// Pin Code
        /// </summary>
        [Display(Name = "Pincode")]
        [MaxLength(6, ErrorMessage = "Maximum {1} Characters Allowed.")]
        [Required(ErrorMessage = "Enter The Pincode.")]
        [CommonValidation(CustomPropertyName = "PinCode", ValidationTypePropertyName = "Pincode", ErrorMessage = "Enter The Valid Pincode.")]
        public string PinCode { get; set; }

        /// <summary>
        /// Skills
        /// </summary>
        [Display(Name = "Skills")]
        [Required(ErrorMessage = "Enter The Skills.")]
        public string Skills { get; set; }

        /// <summary>
        /// Employee Type ID
        /// </summary>
        [Required(ErrorMessage = "Select The Employee Type.")]
        [Display(Name = "Employee Type")]
        public int EmployeeTypeID { get; set; }

        /// <summary>
        /// Employee Type
        /// </summary>
        public string EmployeeType { get; set; }

        /// <summary>
        /// Hire Date
        /// </summary>
        [Required(ErrorMessage = "Select The Hire Date."), DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; }

        /// <summary>
        /// Hire Date
        /// </summary>
        public string DisplayHireDate { get; set; }

        /// <summary>
        /// Parent Company
        /// </summary>
        [Display(Name = "Parent Company")]
        [Required(ErrorMessage = "Enter The Parent Company.")]
        public string ParentCompanyName { get; set; }

        /// <summary>
        /// Profile User Image
        /// </summary>
        [Display(Name = "User Image")]
        [FileExtensionValidation(fileExtensions = "png,jpg,jpeg", ErrorMessage = "Upload Only Valid PNG/JPG/JPEG Image File.")]
        [NotMapped]
        [XmlIgnore]
        public IFormFile ProfileUserImage { get; set; }

        /// <summary>
        /// Employee Profile Image
        /// </summary>
        public Byte[] EmployeeProfileUserImage { get; set; }

        /// <summary>
		/// Employee Image File Name
		/// </summary>
		public string EmployeeImageFileName { get; set; }

        #endregion
    }
}
