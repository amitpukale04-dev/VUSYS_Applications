using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Api.Validation.Attributes
{
    public class NotAllowFileExtensionValidationAttribute : ValidationAttribute
    {
        public List<string> BlockedExtensions;
        public string fileExtensions;

        public NotAllowFileExtensionValidationAttribute()
        {
            ErrorMessage = "The File with the {0} is not allowed";
        }

        public NotAllowFileExtensionValidationAttribute(string fileExtensions)
        {
            this.fileExtensions = fileExtensions;
            ErrorMessage = "The File with the {0} is not allowed";
        }

        public NotAllowFileExtensionValidationAttribute(string fileExtensions, string errorMessage) : base(errorMessage)
        {
            this.fileExtensions = fileExtensions;
        }

        /// <summary>
        /// This Method Is Used to Validate Not Allowed File Extensions.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;

            try
            {
                var defaultBlockedExtensions = new List<string>
                {
                    // 1. Archive / Compressed Files
                    ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".cab",

                    // 2. Audio Files
                    ".mp3", ".wav", ".aac", ".flac", ".ogg", ".m4a", ".wma", ".aiff",

                    // 3. Video Files
                    ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".3gp",

                    // 4. Executable / System Files
                    ".exe", ".dll", ".bat", ".cmd", ".msi", ".sh", ".bin",

                    // 5. Code / Development Files
                    ".cs", ".js", ".ts", ".html", ".css", ".json", ".xml", ".yaml", ".yml", ".sql", ".config", ".cshtml", ".razor", ".py", ".java", ".cpp", ".c", ".php",

                    // 6. Database Files
                    ".db", ".sqlite", ".mdb", ".accdb", ".bak", ".log",

                    // 7. Fonts
                    ".ttf", ".otf", ".woff", ".woff2",

                    // 8. Design / Graphics Files
                    ".psd", ".ai", ".eps", ".xd", ".fig", ".sketch",

                    // 9. Configuration / Settings Files
                    ".ini", ".env", ".conf", ".properties",

                    // 10. Miscellaneous Files
                    ".csv", ".rtf", ".md", ".ics", ".vcf", ".dat", ".tmp",

                    // 11. Email / Messaging
                    ".pst", ".ost", ".mbox",

                    // 12. CAD / Engineering Files
                    ".dwg", ".dxf", ".stl", ".step",

                    // 13. Virtual Machine / Container Files
                    ".vhd", ".vmdk", ".ova", ".ovf", ".dockerfile",

                    // 14. Mobile / App Packages
                    ".apk", ".ipa", ".aab",

                    // 15. Logs & Monitoring
                    ".trace"
                };

                if (!string.IsNullOrWhiteSpace(this.fileExtensions))
                {
                    this.BlockedExtensions = fileExtensions
                        .Split(',')
                        .Select(x => x.Trim().ToLower())
                        .ToList();
                }
                else
                {
                    this.BlockedExtensions = defaultBlockedExtensions;
                }

                if (value == null)
                {
                    return ValidationResult.Success;
                }

                if (value is IFormFile singleFile)
                {
                    string extension = Path.GetExtension(singleFile.FileName)?.ToLower() ?? string.Empty;

                    bool isBlocked = BlockedExtensions.Any(x => x.Equals(extension, StringComparison.OrdinalIgnoreCase));

                    if (isBlocked)
                    {
                        validationResult = new ValidationResult(string.Format(ErrorMessageString, extension));
                    }
                }
                else if (value is IEnumerable<IFormFile> files)
                {
                    foreach (var file in files)
                    {
                        if (file == null)
                            continue;

                        string extension = Path.GetExtension(file.FileName)?.ToLower() ?? string.Empty;

                        bool isBlocked = BlockedExtensions.Any(x => x.Equals(extension, StringComparison.OrdinalIgnoreCase));

                        if (isBlocked)
                        {
                            validationResult = new ValidationResult(string.Format(ErrorMessageString, extension));
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return validationResult;
        }
    }
}