using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface ILogin
    {
        /// <summary>
        /// This Method is Used to Get User Login Details.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="session"></param>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="Password"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        Task<string> GetUserLoginDetails(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, ISession session, string UserName, string EmailID, string Password, string Status);

        /// <summary>
        /// This Method is Used to Reset The Password.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="_objForgotPassword"></param>
        /// <returns></returns>
        Task<string> ResetPassword(AppDbContext db, IWebHostEnvironment webHostEnvironment, ForgotPassword _objForgotPassword);

        /// <summary>
        /// This Method is Used to Send OTP For Authentication.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="db"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="_objUsers"></param>
        /// <returns></returns>
        Task<string> OTPAuthentication(ISession session, AppDbContext db, IWebHostEnvironment webHostEnvironment, UsersM _objUsers);

        /// <summary>
        /// This Method is Used to Change the Password.
        /// </summary>
        /// <param name="_objChangePassword"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        Task<string> ChangePassword(ChangePassword _objChangePassword, string Status);

        /// <summary>
        /// This Method is Used to Change the Password On Reset.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="NewPassword"></param>
        /// <returns></returns>
        Task<string> ChangePasswordOnReset(string EmailID, string NewPassword);

		/// <summary>
		/// This Method is Used to Update User Login or Logout Date.
		/// </summary>
		/// <param name="UserID"></param>
		/// <param name="Param"></param>
		/// <returns></returns>
		Task<bool> UpdateUserLoginOrLogoutDate(int UserID, string Param);
    }
}
