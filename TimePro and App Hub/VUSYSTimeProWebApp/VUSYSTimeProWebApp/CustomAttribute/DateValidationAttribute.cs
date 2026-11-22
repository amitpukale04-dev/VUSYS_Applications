using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.CustomAttribute
{
    public class DateValidationAttribute : ValidationAttribute
    {
        public string FromDatePropertyName;
        public DateValidationAttribute() { }
        public DateValidationAttribute(string FromDatePropertyName, string errorMessage) : base(errorMessage)
        {
            this.FromDatePropertyName = FromDatePropertyName;
        }

        /// <summary>
        /// This Method Is Used to Validate From Date and To Date.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;
            try
            {
                // Using reflection we can get a reference to the From date property
                var containerType = validationContext.ObjectInstance.GetType();
                var field = containerType.GetProperty(this.FromDatePropertyName);
                var extensionValue = field.GetValue(validationContext.ObjectInstance, null);
                if (extensionValue == null)
                {
                    return validationResult;
                }
                var datatype = extensionValue.GetType();

                if (field == null)
                    return new ValidationResult(String.Format("Unknown property: {0}.", FromDatePropertyName));
                // Let's check that otherProperty is of type DateTime as we expect it to be
                if ((field.PropertyType == typeof(DateTime) || (field.PropertyType.IsGenericType && field.PropertyType == typeof(Nullable<DateTime>))))
                {
                    DateTime toValidate = (DateTime)value; //To Date
                    DateTime referenceProperty = (DateTime)field.GetValue(validationContext.ObjectInstance, null); //From Date
                    // if the To date is less than the from date, than the validationResult will be set to false and return
                    // a properly formatted error message

                    if (toValidate < referenceProperty)
                    {
                        validationResult = new ValidationResult(ErrorMessageString);
                    }
                }
                else
                {
                    validationResult = new ValidationResult("An error occurred while validating the property. OtherProperty is not of type DateTime");
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
