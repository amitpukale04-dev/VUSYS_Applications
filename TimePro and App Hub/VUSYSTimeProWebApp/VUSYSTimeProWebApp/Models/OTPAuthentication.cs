using System.ComponentModel.DataAnnotations;
using VUSYSTimeProWebApp.CustomAttribute;

namespace VUSYSTimeProWebApp.Models
{
    public class OTPAuthentication
    {
        #region [OTP Authentication Properties]

        /// <summary>
        /// Email ID
        /// </summary>
        public string EmailID { get; set; }

        /// <summary>
        /// Digit 1
        /// </summary>
        [MaxLength(1)]
        [RegularExpression("^[0-9]$", ErrorMessage = "Only single digit is allowed.")]
        public string Digit1 { get; set; }

        /// <summary>
        /// Digit 2
        /// </summary>
        [MaxLength(1)]
        [RegularExpression("^[0-9]$", ErrorMessage = "Only single digit is allowed.")]
        public string Digit2 { get; set; }

        /// <summary>
        /// Digit 3
        /// </summary>
        [MaxLength(1)]
        [RegularExpression("^[0-9]$", ErrorMessage = "Only single digit is allowed.")]
        public string Digit3 { get; set; }

        /// <summary>
        /// Digit 4
        /// </summary>
        [MaxLength(1)]
        [RegularExpression("^[0-9]$", ErrorMessage = "Only single digit is allowed.")]
        public string Digit4 { get; set; }

        [OTPValidationAttribute(OTPDigitsPropertyName= "OTPDigits", ErrorMessage = "Please Enter 4 Digits of OTP.")]
        public string OTPDigits
        {
            get
            {
                return $"{Digit1}{Digit2}{Digit3}{Digit4}";
            }
        }

        ///// <summary>
        ///// Custom validation for missing OTP digits
        ///// </summary>
        //[Required(ErrorMessage = "Please enter all four digits.")]
        //public bool IsOTPValid => !string.IsNullOrEmpty(Digit1) && !string.IsNullOrEmpty(Digit2) && !string.IsNullOrEmpty(Digit3) && !string.IsNullOrEmpty(Digit4);

        #endregion [OTP Authentication Properties]
    }
}
