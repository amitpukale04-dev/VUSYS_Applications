using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.CustomAttribute
{
    public class NumberGreaterThanZeroValidationAttribute : ValidationAttribute
    {
        public string NumberPropertyName;
        public NumberGreaterThanZeroValidationAttribute() { }
        public NumberGreaterThanZeroValidationAttribute(string NumberPropertyName, string errorMessage) : base(errorMessage)
        {
            this.NumberPropertyName = NumberPropertyName;
        }

        /// <summary>
        /// This Method Is Used to Validate Number.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;
            try
            {
                // Using reflection we can get a reference to the Number property
                var containerType = validationContext.ObjectInstance.GetType();
                var field = containerType.GetProperty(this.NumberPropertyName);
                var extensionValue = field.GetValue(validationContext.ObjectInstance, null);
                if (extensionValue == null)
                {
                    return validationResult;
                }
                var datatype = extensionValue.GetType();

                if (field == null)
                    return new ValidationResult(String.Format("Unknown property: {0}.", NumberPropertyName));
                // Let's check that otherProperty is of type DateTime as we expect it to be
                if ((field.PropertyType == typeof(int) || (field.PropertyType.IsGenericType && field.PropertyType == typeof(Nullable<int>))))
                {
                    int referenceProperty = (int)field.GetValue(validationContext.ObjectInstance, null); //Integer Number

                    if (referenceProperty == 0)
                    {
                        validationResult = new ValidationResult(ErrorMessageString);
                    }
                }
                else
                {
                    validationResult = new ValidationResult("An error occurred while validating the property. OtherProperty is not of type Number");
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
