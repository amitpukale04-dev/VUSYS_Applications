using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Users
{
    public interface IUserLoginService
    {
        /// <summary>
        /// This Method Is Used To Get User Login Details.
        /// </summary>
        /// <param name="objUserLogin"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetUserLoginDetails(UserLogin objUserLogin, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Scheduler User Login Details.
        /// </summary>
        /// <param name="objSchedulerUserLogin"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetSchedulerUserLoginDetails(SchedulerUserLogin objSchedulerUserLogin, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Refresh Token.
        /// </summary>
        /// <param name="objTokenRefreshRequest"></param>
        /// <returns></returns>
        Task<DataSet> GetRefreshToken(TokenRefreshRequest objTokenRefreshRequest);

        /// <summary>
        /// This Method Is Used To Change The Password.
        /// </summary>
        /// <param name="_objChangePassword"></param>
        /// <param name="Source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		Task<DataSet> ChangePassword(ChangePassword _objChangePassword, string Source, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Send Request For Reset Password.
        /// </summary>
        /// <param name="_objForgotPassword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		Task<DataSet> ForgotPassword(ForgotPassword _objForgotPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Check For Password Link Available.
        /// </summary>
        /// <param name="_objCheckPasswordLinkAvailableRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
		Task<DataSet> CheckForPasswordLinkAvailable(CheckPasswordLinkAvailableRequestParam _objCheckPasswordLinkAvailableRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Logout.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> Logout(string EmailID, CancellationToken cancellationToken = default);
    }
}
