using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.CustomAttribute
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
				IFormFile[] UploadedFiles = value as IFormFile[];

				if (UploadedFiles != null)
				{
					foreach (var file in UploadedFiles)
					{
						var maxFileSizeInBytes = MaxFileSizeInMB * 1024 * 1024;

						if (file.Length > maxFileSizeInBytes)
						{
							//return new ValidationResult($"File size cannot exceed {MaxFileSizeInMB} MB.");
							return new ValidationResult(ErrorMessageString);
						}
					}
				}

				//This is for Single File Upload
				//if (value is IFormFile file)
				//{
				//	var maxFileSizeInBytes = MaxFileSizeInMB * 1024 * 1024;

				//	if (file.Length > maxFileSizeInBytes)
				//	{
				//		//return new ValidationResult($"File size cannot exceed {MaxFileSizeInMB} MB.");
				//		return new ValidationResult(ErrorMessageString);
				//	}
				//}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return validationResult;
		}
	}
}
