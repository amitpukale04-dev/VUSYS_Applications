using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class SecurityMaster : SecurityMasterSharedCommon
    {
        
    }

    public class SecurityMasterSharedCommon : SecurityMasterCommon, ICommonModel
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

    public class SecurityMasterCommon
    {
        /// <summary>
        /// Security Id
        /// </summary>
        [Display(Name = "Security Id")]
        [Required(ErrorMessage = "Enter The Security Id.")]
        public int SecurityId { get; set; }

        /// <summary>
        /// Security Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Security Name")]
        [Required(ErrorMessage = "Enter The Security Name.")]
        public string SecurityName { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Display(Name = "Role ID")]
        [Required(ErrorMessage = "Enter The Role ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "RoleID", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int RoleID { get; set; }

        /// <summary>
        /// Screen Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Screen Name")]
        [Required(ErrorMessage = "Enter The Screen Name.")]
        public string ScreenName { get; set; }

        /// <summary>
        /// Is Default
        /// </summary>
        [Display(Name = "Is Default")]
        public bool IsDefault { get; set; }
    }

    public class SecurityMasterDetailsRequestParam
    {
        /// <summary>
        /// Security
        /// </summary>
        [Display(Name = "Security Id")]
        [Required(ErrorMessage = "Enter The Security Id.")]
        public int SecurityId { get; set; }
    }

    public class SecurityMasterParameterCommon : SecurityMasterCommon, IRequestParameterCommonModel
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
}
