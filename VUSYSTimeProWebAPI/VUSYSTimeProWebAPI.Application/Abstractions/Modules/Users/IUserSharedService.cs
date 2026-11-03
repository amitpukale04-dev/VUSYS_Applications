using System.Data;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Users
{
    public interface IUserSharedService
    {
        /// <summary>
        /// This Method Is Used To Get User Details.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="ParamType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetUserDetails(string EmailID, string ParamType, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method is Used to Update User Login or Logout Date.
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="Param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> UpdateUserLoginOrLogoutDate(int UserID, string Param, CancellationToken cancellationToken = default);
    }
}
