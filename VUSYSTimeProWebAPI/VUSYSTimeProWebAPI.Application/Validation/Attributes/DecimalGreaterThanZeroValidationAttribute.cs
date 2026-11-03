using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class DecimalGreaterThanZeroValidationAttribute : ValidationAttribute
    {
        public string DecimalNumberPropertyName;
        public DecimalGreaterThanZeroValidationAttribute() { }
        public DecimalGreaterThanZeroValidationAttribute(string DecimalNumberPropertyName, string errorMessage) : base(errorMessage)
        {
            this.DecimalNumberPropertyName = DecimalNumberPropertyName;
        }

        /// <summary>
        /// This Method Is Used to Validate Decimal.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;
            try
            {
                // Using reflection we can get a reference to the Decimal property
                var containerType = validationContext.ObjectInstance.GetType();
                var field = containerType.GetProperty(this.DecimalNumberPropertyName);
                var extensionValue = field.GetValue(validationContext.ObjectInstance, null);
                if (extensionValue == null)
                {
                    return validationResult;
                }
                var datatype = extensionValue.GetType();

                if (field == null)
                    return new ValidationResult(String.Format("Unknown property: {0}.", DecimalNumberPropertyName));
                // Let's check that otherProperty is of type DateTime as we expect it to be
                if ((field.PropertyType == typeof(decimal) || (field.PropertyType.IsGenericType && field.PropertyType == typeof(Nullable<decimal>))))
                {
                    decimal referenceProperty = (decimal)field.GetValue(validationContext.ObjectInstance, null); //Decimal value

                    if (referenceProperty == Convert.ToDecimal(0.00))
                    {
                        validationResult = new ValidationResult(ErrorMessageString);
                    }
                }
                else
                {
                    validationResult = new ValidationResult("An error occurred while validating the property. OtherProperty is not of type Decimal");
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
