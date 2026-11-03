using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.CustomAttribute
{
    public class OTPValidationAttribute: ValidationAttribute
    {
        public string OTPDigitsPropertyName;
        public OTPValidationAttribute() { }
        public OTPValidationAttribute(string OTPDigitsPropertyName, string errorMessage) : base(errorMessage)
        {
            this.OTPDigitsPropertyName = OTPDigitsPropertyName;
        }

        /// <summary>
        /// This Method Is Used to Validate OTP.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;
            try
            {
                if (this.OTPDigitsPropertyName != null || this.OTPDigitsPropertyName != "")
                {
                    var containerType = validationContext.ObjectInstance.GetType();
                    var field = containerType.GetProperty(this.OTPDigitsPropertyName);
                    var fieldValue = field.GetValue(validationContext.ObjectInstance, null);

                    if (fieldValue != null && fieldValue != string.Empty && fieldValue != "")
                    {
                        if(fieldValue.ToString().Length < 4)
                        {
                            validationResult = new ValidationResult(ErrorMessageString);
                        }
                    }
                    else
                    {
                        validationResult = new ValidationResult(ErrorMessageString);
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
