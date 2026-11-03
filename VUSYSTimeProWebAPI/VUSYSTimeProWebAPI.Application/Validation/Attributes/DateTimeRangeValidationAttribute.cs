using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class DateTimeRangeValidationAttribute : ValidationAttribute
    {
        private readonly string _fromPeriodProperty;
        private readonly string _dateFormat;
        private readonly string _customErrorMessage;

        public DateTimeRangeValidationAttribute(string fromPeriodProperty, string dateFormat, string errorMessage = null)
        {
            _fromPeriodProperty = fromPeriodProperty;
            _dateFormat = dateFormat;
            _customErrorMessage = errorMessage;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var toPeriodStr = value as string;

            PropertyInfo fromProp = validationContext.ObjectType.GetProperty(_fromPeriodProperty);
            if (fromProp == null)
                return new ValidationResult($"Unknown property: {_fromPeriodProperty}");

            var fromPeriodStr = fromProp.GetValue(validationContext.ObjectInstance) as string;

            // Skip if empty (assume [Required] is used separately)
            if (string.IsNullOrWhiteSpace(fromPeriodStr) || string.IsNullOrWhiteSpace(toPeriodStr))
                return ValidationResult.Success;

            if (DateTime.TryParseExact(fromPeriodStr, _dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDate) && DateTime.TryParseExact(toPeriodStr, _dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDate))
            {
                if (toDate < fromDate)
                {
                    var message = _customErrorMessage ?? "{0} must be greater than or equal to {1} using format '{2}'.";
                    return new ValidationResult( string.Format(message, validationContext.DisplayName, _fromPeriodProperty, _dateFormat));
                }
            }
            else
            {
                return new ValidationResult($"Invalid date format. Expected '{_dateFormat}'.");
            }
            return ValidationResult.Success;
        }
    }

}
