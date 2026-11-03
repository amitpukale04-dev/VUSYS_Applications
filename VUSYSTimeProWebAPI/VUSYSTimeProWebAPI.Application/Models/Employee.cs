using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class Employee
    {
        
    }

    [Table("VUSYS_Employees")]
    [Keyless]
    public class EmployeeM : EmployeeSharedCommon
    {
        
    }

    public class EmployeeSharedCommon : EmployeeCommon, ICommonModel
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

    public class EmployeeCommon
    {
        /// <summary>
        /// Employee ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Employee ID")]
        [Required(ErrorMessage = "Enter The Employee ID.")]
        public int EmployeeID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Enter The Full Name.")]
        public string FullName { get; set; }

        /// <summary>
        /// Gender ID
        /// </summary>
        [Display(Name = "Gender")]
        [Required(ErrorMessage = "Enter The Gender ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "GenderID", ErrorMessage = "Gender ID Should Be Greater Than 0.")]
        public int GenderID { get; set; }

        /// <summary>
		/// Date of Birth
		/// </summary>
		[DefaultValue("")]
        [Display(Name = "Date of Birth")]
        [DOBValidation(ErrorMessage = "Enter The Valid Date of Birth in (yyyy-MM-dd) Format and Should Not Be Future Date.")]
        public string DateofBirth { get; set; }

        /// <summary>
        /// Mobile Number
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Mobile Number")]
        [Required(ErrorMessage = "Enter The Mobile Number.")]
        [CommonValidation(CustomPropertyName = "MobileNo", ValidationTypePropertyName = "MobileNumber", ErrorMessage = "Enter The Valid Mobile Number.")]
        public string MobileNo { get; set; }

        /// <summary>
        /// Email ID
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Enter The Email ID.")]
        [CommonValidation(CustomPropertyName = "EmailID", ValidationTypePropertyName = "Email", ErrorMessage = "Enter The Valid Email ID.")]
        public string EmailID { get; set; }

        /// <summary>
        /// Country ID
        /// </summary>
        [Display(Name = "Country ID")]
        [Required(ErrorMessage = "Enter The Country ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "CountryID", ErrorMessage = "Country ID Should Be Greater Than 0.")]
        public int CountryID { get; set; }

        /// <summary>
        /// State ID
        /// </summary>
        [Display(Name = "State ID")]
        [Required(ErrorMessage = "Enter The State ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "StateID", ErrorMessage = "State ID Should Be Greater Than 0.")]
        public int StateID { get; set; }

        /// <summary>
        /// City Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "City")]
        [Required(ErrorMessage = "Enter The City.")]
        public string CityName { get; set; }

        /// <summary>
        /// PinCode
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Pin Code")]
        [Required(ErrorMessage = "Enter The Pin Code.")]
        [CommonValidation(CustomPropertyName = "PinCode", ValidationTypePropertyName = "Pincode", ErrorMessage = "Enter The Valid Pincode.")]
        public string PinCode { get; set; }

        /// <summary>
        /// Skills
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Skills")]
        [Required(ErrorMessage = "Enter The Skills.")]
        public string Skills { get; set; }

        /// <summary>
        /// Employee Type ID
        /// </summary>
        [Display(Name = "Employee Type ID")]
        [Required(ErrorMessage = "Enter The Employee Type ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "EmployeeTypeID", ErrorMessage = "Employee Type ID Should Be Greater Than 0.")]
        public int EmployeeTypeID { get; set; }

        /// <summary>
		/// Hire Date
		/// </summary>
		[DefaultValue("")]
        [Display(Name = "Hire Date")]
        [Required(ErrorMessage = "Enter The Hire Date.")]
        [CommonDateTimeValidation(ErrorMessage = "Enter The Valid Hire Date in (yyyy-MM-dd) Format.")]
        public string HireDate { get; set; }

        /// <summary>
        /// Parent Company
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Parent Company")]
        [Required(ErrorMessage = "Enter The Parent Company.")]
        public string ParentCompanyName { get; set; }

        /// <summary>
        /// Employee Profile Image
        /// </summary>
        public Byte[] EmployeeProfileUserImage { get; set; }

        /// <summary>
		/// Employee Image File Name
		/// </summary>
		public string EmployeeImageFileName { get; set; }
    }

    public class EmployeeRequestParameterCommon : EmployeeCommon, IRequestParameterCommonModel
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

    public class EmployeeDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Employee Id")]
        [Required(ErrorMessage = "Enter The Employee Id.")]
        public int Id { get; set; }
    }
}
