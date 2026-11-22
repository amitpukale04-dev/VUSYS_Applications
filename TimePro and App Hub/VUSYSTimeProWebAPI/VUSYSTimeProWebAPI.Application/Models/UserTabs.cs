using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebAPI.Application.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class UserTabs
    {
    }

    public class UserTabM
    {
        /// <summary>
        /// Path
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Path")]
        [Required(ErrorMessage = "Enter The Path.")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Label
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Label")]
        [Required(ErrorMessage = "Enter The Label.")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// SVG Icon
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "SVG Icon")]
        public string SvgIcon { get; set; } = string.Empty;

        /// <summary>
        /// Tone
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Tone")]
        public string Tone { get; set; } = string.Empty;

        /// <summary>
        /// Closable
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Closable")]
        public bool Closable { get; set; }
    }

    public class UserTabsRequestParam
    {
        /// <summary>
        /// User ID
        /// </summary>
        [Display(Name = "User ID")]
        [Required(ErrorMessage = "Enter The User ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "UserId", ErrorMessage = "User ID Should Be Greater Than 0.")]
        public int UserId { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Display(Name = "Role ID")]
        [Required(ErrorMessage = "Enter The Role ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "UserId", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int RoleId { get; set; }
    }

    public class SaveUserTabsRequestParam
    {
        /// <summary>
        /// User ID
        /// </summary>
        [Display(Name = "User ID")]
        [Required(ErrorMessage = "Enter The User ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "UserId", ErrorMessage = "User ID Should Be Greater Than 0.")]
        public int UserId { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Display(Name = "Role ID")]
        [Required(ErrorMessage = "Enter The Role ID.")]
        [NumberGreaterThanZeroValidation(NumberPropertyName = "UserId", ErrorMessage = "Role ID Should Be Greater Than 0.")]
        public int RoleId { get; set; }

        /// <summary>
        /// Tabs
        /// </summary>
        [Display(Name = "Tabs")]
        [Required(ErrorMessage = "Enter The Tabs.")]
        public List<UserTabM> Tabs { get; set; } = new List<UserTabM>();
    }

    public class UserTabsFileM
    {
        /// <summary>
        /// User Id
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// Tabs
        /// </summary>
        public List<UserTabM> Tabs { get; set; } = new List<UserTabM>();
    }

    public class UserTabsResponseM
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Tabs
        /// </summary>
        public List<UserTabM> Tabs { get; set; } = new List<UserTabM>();
    }
}