using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebAPI.Api.Validation.Attributes;

namespace VUSYSTimeProWebAPI.Api.Models.FileUploads
{
    public class CommonFileUploadRequestParam
    {
        /// <summary>
        /// Folder Full Path
        /// </summary>
        [Display(Name = "Folder Full Path")]
        [Required(ErrorMessage = "Enter The Folder Full Path.")]
        public string FolderFullPath { get; set; }

        /// <summary>
        /// Base Path
        /// </summary>
        [Display(Name = "Base Path")]
        [Required(ErrorMessage = "Enter The Base Path.")]
        public string BasePath { get; set; }

        /// <summary>
        /// Base Path Error Message
        /// </summary>
        [Display(Name = "Base Path Error Message")]
        [Required(ErrorMessage = "Enter The Base Path Error Message.")]
        public string BasePathErrorMessage { get; set; }

        /// <summary>
        /// Custom File Name
        /// </summary>
        [Display(Name = "Custom File Name")]
        [Required(ErrorMessage = "Enter The Custom File Name.")]
        public string CustomFileName { get; set; }

        /// <summary>
        /// Dynamic Headers
        /// </summary>
        public List<string> DynamicHeaders { get; set; } = new();

        /// <summary>
        /// Dynamic Rows
        /// </summary>
        public List<string> DynamicRows { get; set; } = new();

        /// <summary>
        /// File Upload
        /// </summary>
        [Display(Name = "File Upload")]
        [Required(ErrorMessage = "Select The File To Upload.")]
        [FileExtensionValidation(fileExtensions = "pdf,PDF", ErrorMessage = "Upload Only Valid PDF File.")]
        [FileSizeValidation(MaxFileSizeInMB = 15, ErrorMessage = "File Size Cannot Exceed 15 MB.")]
        public List<IFormFile> FileData { get; set; } = new();

        /// <summary>
        /// Table Name
        /// </summary>
        [Display(Name = "Table Name")]
        [Required(ErrorMessage = "Enter The Table Name.")]
        public string TableName { get; set; }
    }

    public class CommonViewFileUploadRequestParam
    {
        /// <summary>
        /// Folder Full Path
        /// </summary>
        [Display(Name = "Folder Full Path")]
        [Required(ErrorMessage = "Enter The Folder Full Path.")]
        public string FolderFullPath { get; set; }

        /// <summary>
        /// Base Path
        /// </summary>
        [Display(Name = "Base Path")]
        [Required(ErrorMessage = "Enter The Base Path.")]
        public string BasePath { get; set; }

        /// <summary>
        /// Base Path Error Message
        /// </summary>
        [Display(Name = "Base Path Error Message")]
        [Required(ErrorMessage = "Enter The Base Path Error Message.")]
        public string BasePathErrorMessage { get; set; }

        /// <summary>
        /// Dynamic Headers
        /// </summary>
        public List<string> DynamicHeaders { get; set; } = new();

        /// <summary>
        /// Dynamic Rows
        /// </summary>
        public List<string> DynamicRows { get; set; } = new();

        /// <summary>
        /// File Name
        /// </summary>
        [Display(Name = "File Name")]
        public string FileName { get; set; }

        /// <summary>
        /// Custom File Name
        /// </summary>
        [Display(Name = "Custom File Name")]
        [Required(ErrorMessage = "Enter The Custom File Name.")]
        public string CustomFileName { get; set; }

        /// <summary>
        /// Is Include Custom File Name
        /// </summary>
        [Display(Name = "Is Include Custom File Name")]
        [Required(ErrorMessage = "Enter Is Include Custom File Name.")]
        public bool IsIncludeCustomFileName { get; set; }

        /// <summary>
        /// Table Name
        /// </summary>
        [Display(Name = "Table Name")]
        [Required(ErrorMessage = "Enter The Table Name.")]
        public string TableName { get; set; }
    }
}
