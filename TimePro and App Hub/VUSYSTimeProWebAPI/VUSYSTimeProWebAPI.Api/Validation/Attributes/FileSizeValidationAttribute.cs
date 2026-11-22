using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Api.Validation.Attributes
{
	public class FileSizeValidationAttribute : ValidationAttribute
	{
		public int MaxFileSizeInMB;
		public FileSizeValidationAttribute() { }
		public FileSizeValidationAttribute(int maxFileSizeInMB, string errorMessage) : base(errorMessage)
		{
			this.MaxFileSizeInMB = maxFileSizeInMB;
		}

		/// <summary>
		/// This Method Is Used to Validate File Size.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="validationContext"></param>
		/// <returns></returns>
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			ValidationResult validationResult = ValidationResult.Success;
			try
			{
                //if (value is IFormFile file)
                //{
                //	var maxFileSizeInBytes = MaxFileSizeInMB * 1024 * 1024;

                //	if (file.Length > maxFileSizeInBytes)
                //	{
                //		//return new ValidationResult($"File size cannot exceed {MaxFileSizeInMB} MB.");
                //		return new ValidationResult(ErrorMessageString);
                //	}
                //}

                if (value is IFormFile single)
                    return ValidateOne(single);

                if (value is IEnumerable<IFormFile> many)
                {
                    foreach (var f in many)
                    {
                        var res = ValidateOne(f);
                        if (res != ValidationResult.Success)
                            return res;
                    }
                    return ValidationResult.Success;
                }
            }
			catch (Exception ex)
			{
				throw ex;
			}
			return validationResult;
		}

        private ValidationResult ValidateOne(IFormFile file)
        {
            if (file is null) return ValidationResult.Success;

            const long MinPdfSizeBytes = 1024;
            if (file.Length < MinPdfSizeBytes)
            {
                return new ValidationResult($"The uploaded file size is less than 1 KB. File size must be between 1 KB and {MaxFileSizeInMB} MB.");
            }
            var maxBytes = (long)MaxFileSizeInMB * 1024L * 1024L;
            if (file.Length > maxBytes)
                return new ValidationResult(
                    ErrorMessage ?? $"Each file must be ≤ {MaxFileSizeInMB} MB."
                );

            return ValidationResult.Success;
        }
    }
}
