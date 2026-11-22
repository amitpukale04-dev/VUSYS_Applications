using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class CommonDateTimeValidationAttribute : ValidationAttribute
    {
        public string CustomErrorMessage;
        public CommonDateTimeValidationAttribute() { }

        public CommonDateTimeValidationAttribute(string errorMessage) : base(errorMessage)
        {
            this.CustomErrorMessage = errorMessage;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            try
            {
                // Case 1: If the value is null, we skip validation because Nullable DateTime? is allowed to be null
                if (value == null || value.ToString() == "" || value.ToString() == string.Empty)
                {
                    return ValidationResult.Success;
                }

                // Case 2: Check if the value is a Nullable DateTime (DateTime?)
                //if (value is DateTime? nullableDateTime)
                //{
                //    // Check if it has a value and if the value is DateTime.MinValue
                //    if (nullableDateTime.HasValue)
                //    {
                //        if (nullableDateTime.Value == DateTime.MinValue)
                //        {
                //            return new ValidationResult(ErrorMessageString ?? "Date cannot be DateTime.MinValue.");
                //        }
                //    }
                //}
                // Case 3: Check if the value is a non-nullable DateTime
                if (value is DateTime dateTimeValue)
                {
                    if (dateTimeValue == DateTime.MinValue)
                    {
                        return new ValidationResult(ErrorMessageString ?? "Date cannot be DateTime.MinValue.");
                    }
                }
                // Case 4: Check if the value is a string and attempt to parse it as a valid date string
                else if (value is string dateStr)
                {
                    bool isValidDate = TryParseDateString(dateStr);
                    if (!isValidDate)
                    {
                        return new ValidationResult(ErrorMessageString ?? "Invalid date string format.");
                    }
                }

                return ValidationResult.Success;
            }
            catch (Exception ex)
            {
                // Return error message if an unexpected exception occurs
                return new ValidationResult($"An unexpected error occurred during validation: {ex.Message}");
            }
        }

        // Helper method to parse and validate a date string
        private bool TryParseDateString(string dateStr)
        {
            DateTime parsedDate;
            return DateTime.TryParseExact(dateStr, new[]
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
                CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
        }
    }
}
