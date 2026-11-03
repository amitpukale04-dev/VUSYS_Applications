using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class CommonDateTimeDataTypeValidationAttribute : ValidationAttribute
    {
        public string CustomErrorMessage;

        public CommonDateTimeDataTypeValidationAttribute() { }

        public CommonDateTimeDataTypeValidationAttribute(string errorMessage) : base(errorMessage)
        {
            this.CustomErrorMessage = errorMessage;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            try
            {
                // Case 1: If the value is null or empty, skip validation.
                // RequiredAttribute will handle mandatory field validation.
                if (value == null || value.ToString() == "" || value.ToString() == string.Empty)
                {
                    return ValidationResult.Success;
                }

                // Case 2: Validate DateTime datatype.
                if (value is DateTime dateTimeValue)
                {
                    if (dateTimeValue == DateTime.MinValue)
                    {
                        return new ValidationResult(
                            ErrorMessageString ?? "Enter The Valid Date.");
                    }

                    return ValidationResult.Success;
                }

                // Case 3: Validate string date using allowed formats.
                if (value is string dateStr)
                {
                    bool isValidDate = TryParseDateString(dateStr);

                    if (!isValidDate)
                    {
                        return new ValidationResult(
                            ErrorMessageString ?? "Enter The Valid Date.");
                    }

                    return ValidationResult.Success;
                }

                // Case 4: Invalid datatype.
                return new ValidationResult(
                    ErrorMessageString ?? "Enter The Valid Date.");
            }
            catch (Exception ex)
            {
                return new ValidationResult(
                    $"An unexpected error occurred during validation: {ex.Message}");
            }
        }

        /// <summary>
        /// This Method Is Used To Validate Supported DateTime Formats.
        /// </summary>
        /// <param name="dateStr"></param>
        /// <returns></returns>
        private bool TryParseDateString(string dateStr)
        {
            DateTime parsedDate;

            return DateTime.TryParseExact(
                dateStr,
                new[]
                {
                // Date-only formats
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "yyyy/MM/dd",
                "dd/MM/yyyy",

                // Date + Time (no seconds)
                "yyyy-MM-dd HH:mm",
                "MM/dd/yyyy hh:mm tt",
                "yyyy/MM/dd HH:mm",
                "dd/MM/yyyy HH:mm",
                "yyyy-MM-ddTHH:mm",

                // Date + Time with seconds
                "yyyy-MM-dd HH:mm:ss",
                "MM/dd/yyyy hh:mm:ss tt",
                "yyyy/MM/dd HH:mm:ss",
                "dd/MM/yyyy HH:mm:ss",

                // Date + Time with milliseconds
                "yyyy-MM-dd HH:mm:ss.fff",
                "MM/dd/yyyy hh:mm:ss.fff tt",
                "yyyy/MM/dd HH:mm:ss.fff",
                "dd/MM/yyyy HH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss.fff"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDate);
        }
    }
}
