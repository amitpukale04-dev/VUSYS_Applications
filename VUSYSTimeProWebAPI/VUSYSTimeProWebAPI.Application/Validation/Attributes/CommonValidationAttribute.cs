using System.ComponentModel.DataAnnotations;
using CommonLibCore10;

namespace VUSYSTimeProWebAPI.Application.Validation.Attributes
{
    public class CommonValidationAttribute : ValidationAttribute
    {
        public string CustomPropertyName;
        public string ValidationTypePropertyName;
        public CommonValidationAttribute() { }
        public CommonValidationAttribute(string CustomPropertyName, string ValidationTypePropertyName, string errorMessage) : base(errorMessage)
        {
            this.CustomPropertyName = CustomPropertyName;
            this.ValidationTypePropertyName = ValidationTypePropertyName;
        }

        /// <summary>
        /// This Method Is Used to Validate Common Fields.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult validationResult = ValidationResult.Success;
            try
            {
                if (this.CustomPropertyName != null && this.CustomPropertyName != "" && this.ValidationTypePropertyName != null && this.ValidationTypePropertyName != "")
                {
                    var containerType = validationContext.ObjectInstance.GetType();
                    var field = containerType.GetProperty(this.CustomPropertyName);
                    var fieldValue = field.GetValue(validationContext.ObjectInstance, null);

                    var fieldType = containerType.GetProperty(this.ValidationTypePropertyName);
                    var fieldTypeValue = this.ValidationTypePropertyName;

                    if (fieldValue != null && fieldValue.ToString() != string.Empty && fieldValue.ToString() != "")
                    {
                        if(fieldTypeValue.ToString() == "Email")
                        {
                            string[] MultipleEmails = fieldValue.ToString().Split(',');
                            if (MultipleEmails.Length > 0)
                            {
                                if (MultipleEmails.Length == 1)
                                {
                                    bool IsValid = ValidationHelper.EmailValidation(fieldValue.ToString().Trim());

                                    if (IsValid == false)
                                    {
                                        validationResult = new ValidationResult(ErrorMessageString);
                                        return validationResult;
                                    }
                                }
                                else
                                {
                                    foreach (string Email in MultipleEmails)
                                    {
                                        bool IsValid = ValidationHelper.EmailValidation(Email.Trim());

                                        if (IsValid == false)
                                        {
                                            validationResult = new ValidationResult(ErrorMessageString);
                                            return validationResult;
                                        }
                                    }
                                }
                            }
                        }
                        if (fieldTypeValue.ToString() == "MobileNumber")
                        {
                            bool IsValid = ValidationHelper.MobileNumberValidation(fieldValue.ToString());

                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        if (fieldTypeValue.ToString() == "Password")
                        {
                            bool IsValid = ValidationHelper.PasswordValidation(fieldValue.ToString());

                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        if (fieldTypeValue.ToString() == "Pincode")
                        {
                            bool IsValid = ValidationHelper.PincodeValidation(fieldValue.ToString());

                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        if (fieldTypeValue.ToString() == "PANNumber")
                        {
                            bool IsValid = ValidationHelper.PANNumberValidation(fieldValue.ToString());

                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        if (fieldTypeValue.ToString() == "Website")
                        {
                            bool IsValid = ValidationHelper.WebsiteValidation(fieldValue.ToString());

                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        if (fieldTypeValue.ToString() == "IFSCCode")
                        {
                            bool IsValid = ValidationHelper.IFSCCodeValidation(fieldValue.ToString());

                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        if (fieldTypeValue.ToString() == "BankAccountNumber")
                        {
                            bool IsValid = ValidationHelper.BankAccountNumberValidation(fieldValue.ToString());

                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
                        }
                        if (fieldTypeValue.ToString() == "IPAddress")
                        {
                            bool IsValid = ValidationHelper.IPAddressValidation(fieldValue.ToString());
                            if (IsValid == false)
                            {
                                validationResult = new ValidationResult(ErrorMessageString);
                                return validationResult;
                            }
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
