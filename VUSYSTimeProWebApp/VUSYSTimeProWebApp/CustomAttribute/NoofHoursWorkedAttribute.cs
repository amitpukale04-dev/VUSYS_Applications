using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.CustomAttribute
{
    public class NoofHoursWorkedAttribute : ValidationAttribute
    {
        public string IsOnLeavePropertyName;
        public string NoofHoursWorkedPropertyName;

        public NoofHoursWorkedAttribute()
        {
        }

        public NoofHoursWorkedAttribute(string IsOnLeavePropertyName, string NoofHoursWorkedPropertyName, string errorMessage) : base(errorMessage)
        {
            this.IsOnLeavePropertyName = IsOnLeavePropertyName;
            this.NoofHoursWorkedPropertyName = NoofHoursWorkedPropertyName;
        }

        /// <summary>
        /// This Method Is Used to Validate Mobile Number.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;
            try
            {
                if (this.IsOnLeavePropertyName != null && this.IsOnLeavePropertyName != "")
                {
                    var containerType = validationContext.ObjectInstance.GetType();
                    var field = containerType.GetProperty(this.IsOnLeavePropertyName);
                    var fieldValue = field.GetValue(validationContext.ObjectInstance, null);
                    bool IsOnLeave = Convert.ToBoolean(fieldValue);

                    var NoofHoursWorkedfield = containerType.GetProperty(this.NoofHoursWorkedPropertyName);
                    var NoofHoursWorkedfieldValue = NoofHoursWorkedfield.GetValue(validationContext.ObjectInstance, null);

                    if (!IsOnLeave)
                    {
                        if(NoofHoursWorkedfieldValue != null)
                        {
                            if (Convert.ToDecimal(NoofHoursWorkedfieldValue) < 0)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        else
                        {
                            validationResult = new ValidationResult(ErrorMessageString);
                            return validationResult;
                        }
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
