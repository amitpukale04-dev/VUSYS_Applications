using VUSYSTimeProWebAPI.Application.Models.Authentication;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Authentication
{
    public interface ITokenStoreService
    {
        /// <summary>
        /// This Method Is Used To Add Token To Blacklist.
        /// </summary>
        /// <param name="Token"></param>
        void BlacklistToken(string Token);

        /// <summary>
        /// This Method Is Used To Check Whether The Token is Blacklisted Or Not.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        bool IsTokenBlacklisted(string Token);

        /// <summary>
        /// This Method Is Used To Save Token Details.
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="RoleID"></param>
        /// <param name="token"></param>
        /// <param name="isActive"></param>
        /// <param name="expiresAt"></param>
        /// <returns></returns>
        Task SaveTokenInfoAsync(string UserName, string EmailID, string EmployeeCode, string RoleID, string token, bool isActive, DateTime expiresAt);

        /// <summary>
        /// This Method Is Used To Get Token Details.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <returns></returns>
        Task<TokenInfo> GetTokenInfo(string EmailID);

        /// <summary>
        /// This Method Is Used To Delete Token Details.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <returns></returns>
        Task DeleteTokenAsync(string EmailID);

        /// <summary>
        /// This Method Is Used To Save Refresh Token.
        /// </summary>
        /// <param name="Token"></param>
        void Save(RefreshToken Token);

        /// <summary>
        /// This Method Is Used To Get Refresh Token.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        RefreshToken? Get(string Token);

        /// <summary>
        /// This Method Is Used To Get Refresh Token Details By Username.
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        RefreshToken? GetByUsername(string UserName);

        /// <summary>
        /// This Method Is Used To Revoke Refresh Token.
        /// </summary>
        /// <param name="Tokens"></param>
        void Revoke(string Tokens);
    }
}