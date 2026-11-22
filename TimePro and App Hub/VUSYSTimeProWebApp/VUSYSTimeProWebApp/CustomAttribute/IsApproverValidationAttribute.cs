using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.CustomAttribute
{
    public class IsApproverValidationAttribute : ValidationAttribute
    {
        public string IsApproverPropertyName;
        public string UserIDPropertyName;

        public IsApproverValidationAttribute() { }
        public IsApproverValidationAttribute(string IsApproverPropertyName, string UserIDPropertyName,  string errorMessage) : base(errorMessage)
        {
            this.IsApproverPropertyName = IsApproverPropertyName;
            this.UserIDPropertyName = UserIDPropertyName;
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
                var dbContext = validationContext.GetService(typeof(AppDbContext)) as AppDbContext;
                if (dbContext == null)
                {
                    throw new InvalidOperationException("Unable to resolve AppDbContext from ValidationContext.");
                }

                if (this.IsApproverPropertyName != null || this.IsApproverPropertyName != "" || this.IsApproverPropertyName == "IsApprover")
                {
                    var containerType = validationContext.ObjectInstance.GetType();
                    var field = containerType.GetProperty(this.IsApproverPropertyName.ToString());
                    var fieldValue = field.GetValue(validationContext.ObjectInstance, null);

                    var UserIDfield = containerType.GetProperty(this.UserIDPropertyName);
                    var UserIDfieldValue = UserIDfield.GetValue(validationContext.ObjectInstance, null);

                    if (Convert.ToBoolean(fieldValue) == true)
                    {
                        int Count = DropdownHelper.GetApproverCount(dbContext, Convert.ToInt32(UserIDfieldValue));
                        if(Count >= 1)
                        {
                            validationResult = new ValidationResult(ErrorMessageString);
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
