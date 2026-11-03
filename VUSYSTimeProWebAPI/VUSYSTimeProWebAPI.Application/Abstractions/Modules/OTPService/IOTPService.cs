using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.OTPService
{
    public interface IOTPService
    {
        #region [OTP]

        /// <summary>
        /// This Method Is Used To Generate Or Resend OTP.
        /// </summary>
        /// <param name="_objOTP"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GenerateOrResendOTP(OTP _objOTP, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Sent OTP Email.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="FullName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SendOTPEmail(string EmailID, string FullName, CancellationToken cancellationToken = default);

        #endregion [OTP]
    }
}
