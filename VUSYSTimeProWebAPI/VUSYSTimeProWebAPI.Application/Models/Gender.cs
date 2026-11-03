using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class Gender
    {
    }

    [Table("VUSYS_GenderMaster")]
    [Keyless]
    public class GenderM : GenderSharedCommon
    {
        
    }

    public class GenderSharedCommon : GenderCommon, ICommonModel
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

    public class GenderCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Gender Id")]
        [Required(ErrorMessage = "Enter The Gender Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Gender
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Gender")]
        [Required(ErrorMessage = "Enter The Gender.")]
        public string Gender { get; set; }
    }

    public class GenderDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Gender Id")]
        [Required(ErrorMessage = "Enter The Gender Id.")]
        public int Id { get; set; }
    }

    public class GenderRequestParameterCommon : GenderCommon, IRequestParameterCommonModel
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
}
