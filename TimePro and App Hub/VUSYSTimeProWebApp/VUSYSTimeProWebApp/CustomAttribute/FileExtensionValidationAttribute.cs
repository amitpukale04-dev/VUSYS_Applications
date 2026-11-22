using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.CustomAttribute
{
    public class FileExtensionValidationAttribute : ValidationAttribute
    {
        public List<string> AllowedExtensions;
        public string fileExtensions;

        public FileExtensionValidationAttribute() { }
        public FileExtensionValidationAttribute(string fileExtensions, string errorMessage) : base(errorMessage)
        {
            this.fileExtensions = fileExtensions;
        }

        /// <summary>
        /// This Method Is Used to Validate File Extension.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;

            try
            {
                if (this.fileExtensions != null || this.fileExtensions != "")
                {
                    this.AllowedExtensions = fileExtensions.Split(',').Select(x => x.Trim()).Select(x => Convert.ToString(x)).ToList();

                    IFormFile file = value as IFormFile;

                    if (file != null)
                    {
                        var fileName = file.FileName;

                        bool IsValidFile = AllowedExtensions.Any(y => fileName.EndsWith(y));

                        if (IsValidFile == false)
                        {
                            validationResult = new ValidationResult(ErrorMessageString);
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
