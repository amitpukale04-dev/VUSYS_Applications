using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Application.Models
{
    public class DropdownRequestParam
    {
        /// <summary>
        /// Source Table Name for dropdown values
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Source Table")]
        [Required(ErrorMessage = "Enter The Source Table.")]
        public string SourceTable { get; set; }

        /// <summary>
        /// Optional argument to filter or refine dropdown items
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Argument Value")]
        public string ArgumentValue { get; set; }

        /// <summary>
        /// Optional argument to filter or refine dropdown items
        /// </summary>
        [DefaultValue("")]
        [Display(Name = "Argument Value1")]
        public string ArgumentValue1 { get; set; }
    }

    public class DropDownData
    {
        /// <summary>
        /// Display Text
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Value Text
        /// </summary>
        public string ValueText { get; set; }
    }
}
