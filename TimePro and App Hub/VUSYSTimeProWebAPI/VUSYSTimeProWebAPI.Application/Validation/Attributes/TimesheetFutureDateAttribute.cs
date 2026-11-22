using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class TimesheetFutureDateAttribute : ValidationAttribute
    {
        public string CustomErrorMessage;
        public TimesheetFutureDateAttribute()
        {
        }

        public TimesheetFutureDateAttribute(string errorMessage) : base(errorMessage)
        {
            this.CustomErrorMessage = errorMessage;
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
                if (value is DateTime dateTime)
                {
                    // If the date is in the future, return an error
                    if (dateTime > DateTime.Today)
                    {
                        validationResult = new ValidationResult(CustomErrorMessage);
                        return validationResult;
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
