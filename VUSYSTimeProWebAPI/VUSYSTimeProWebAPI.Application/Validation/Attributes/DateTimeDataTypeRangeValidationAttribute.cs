using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class DateTimeDataTypeRangeValidationAttribute : ValidationAttribute
    {
        private readonly string _fromPeriodProperty;
        private readonly string _dateFormat;
        private readonly string _customErrorMessage;

        public DateTimeDataTypeRangeValidationAttribute(string fromPeriodProperty, string dateFormat, string errorMessage = null)
        {
            _fromPeriodProperty = fromPeriodProperty;
            _dateFormat = dateFormat;
            _customErrorMessage = errorMessage;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            try
            {
                PropertyInfo fromProp = validationContext.ObjectType.GetProperty(_fromPeriodProperty);

                if (fromProp == null)
                    return new ValidationResult($"Unknown property: {_fromPeriodProperty}");

                object fromPeriodValue = fromProp.GetValue(validationContext.ObjectInstance);

                if (fromPeriodValue == null || value == null)
                    return ValidationResult.Success;

                if (fromPeriodValue is DateTime fromDate && value is DateTime toDate)
                {
                    if (toDate < fromDate)
                    {
                        string message = _customErrorMessage ??
                            $"{validationContext.DisplayName} must be greater than or equal to {_fromPeriodProperty}.";

                        return new ValidationResult(message);
                    }

                    return ValidationResult.Success;
                }

                return new ValidationResult(
                    $"Invalid date value. Expected date format '{_dateFormat}'.");
            }
            catch (Exception)
            {
                return new ValidationResult(
                    $"Invalid date value. Expected date format '{_dateFormat}'.");
            }
        }
    }
}
