using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using VUSYSTimeProWebAPI.Api.Validation.Attributes;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Api.Models.FileUploads
{
    public class EmployeeRequestParam : EmployeeRequestParameterCommon
    {
        /// <summary>
        /// Profile User Image
        /// </summary>
        [Display(Name = "User Image")]
        [FileExtensionValidation(fileExtensions = "png,jpg,jpeg", ErrorMessage = "Upload Only Valid PNG/JPG/JPEG Image File.")]
        [NotMapped]
        [XmlIgnore]
        public IFormFile ProfileUserImage { get; set; }
    }
}
