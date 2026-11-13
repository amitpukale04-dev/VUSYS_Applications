using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class Holiday
    {
    }

    [Table("VUSYS_HolidayMaster")]
    [Keyless]
    public class HolidayM : HolidaySharedCommon
    {

    }

    public class HolidaySharedCommon : HolidayCommon, ICommonModel
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

    public class HolidayCommon
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Holiday Id")]
        [Required(ErrorMessage = "Enter The Holiday Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Holiday Name
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Holiday Name")]
        [Required(ErrorMessage = "Enter The Holiday Name.")]
        public string HolidayName { get; set; }

        /// <summary>
        /// Holiday Date
        /// </summary>
        [Display(Name = "Holiday Date")]
        [Required(ErrorMessage = "Enter The Holiday Date."), DataType(DataType.Date)]
        [JsonConverter(typeof(CommonDateTimeDataTypeConverter))]
        [CommonDateTimeDataTypeValidation(ErrorMessage = "Enter The Valid Holiday Date in (yyyy-MM-dd) format.")]
        public DateTime HolidayDate { get; set; }

        /// <summary>
        /// Holiday Year
        /// </summary>
        [Display(Name = "Holiday Year")]
        [Required(ErrorMessage = "Enter The Holiday Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "HolidayYear", ErrorMessage = "Holiday Year Should Be Greater Than 0.")]
        public int HolidayYear { get; set; }

        /// <summary>
        /// Is Optional Holiday
        /// </summary>
        [Display(Name = "Is Optional Holiday")]
        public bool IsOptionalHoliday { get; set; } = false;

        /// <summary>
        /// Holiday Description
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Holiday Description")]
        [Required(ErrorMessage = "Enter The Holiday Description.")]
        public string HolidayDescription { get; set; }
    }

    public class HolidayDetailsRequestParam
    {
        /// <summary>
        /// Id
        /// </summary>
        [Display(Name = "Holiday Id")]
        [Required(ErrorMessage = "Enter The Holiday Id.")]
        public int Id { get; set; }

        /// <summary>
        /// Holiday Year
        /// </summary>
        [Display(Name = "Holiday Year")]
        [Required(ErrorMessage = "Enter The Holiday Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "HolidayYear", ErrorMessage = "Holiday Year Should Be Greater Than 0.")]
        public int HolidayYear { get; set; }
    }

    public class HolidayDetailsExportToPDFRequestParam
    {
        /// <summary>
        /// Holiday Year
        /// </summary>
        [Display(Name = "Holiday Year")]
        [Required(ErrorMessage = "Enter The Holiday Year.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "HolidayYear", ErrorMessage = "Holiday Year Should Be Greater Than 0.")]
        public int HolidayYear { get; set; }
    }

    public class HolidayRequestParameterCommon : HolidayCommon, IRequestParameterCommonModel
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