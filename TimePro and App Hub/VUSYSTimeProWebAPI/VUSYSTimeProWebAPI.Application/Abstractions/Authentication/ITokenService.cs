using System.Security.Claims;
using VUSYSTimeProWebAPI.Application.Models.Authentication;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Authentication;

public interface ITokenService
{
    /// <summary>
    /// This Method Is Used To Generate Token.
    /// </summary>
    /// <param name="UserName"></param>
    /// <param name="EmailID"></param>
    /// <param name="EmployeeCode"></param>
    /// <param name="RoleID"></param>
    /// <returns></returns>
    string GenerateToken(string UserName, string EmailID, string EmployeeCode, string RoleID);

    /// <summary>
    /// This Method Is Used To Get Token Expiration In Minutes.
    /// </summary>
    /// <returns></returns>
    int GetTokenExpirationInMinutes();

    /// <summary>
    /// This Method Is Used To Checked Whether Token Is Expired Or Not.
    /// </summary>
    /// <param name="Token"></param>
    /// <returns></returns>
    bool IsTokenExpired(string Token);

    /// <summary>
    /// This Method Is Used To Get Token Details.
    /// </summary>
    /// <param name="Token"></param>
    /// <returns></returns>
    ClaimsPrincipal? GetTokenDetails(string Token);

    /// <summary>
    /// This Method Is Used To Generate Refresh Token.
    /// </summary>
    /// <param name="UserName"></param>
    /// <param name="EmailID"></param>
    /// <param name="EmployeeCode"></param>
    /// <param name="RoleID"></param>
    /// <returns></returns>
    string GenerateRefreshToken(string UserName, string EmailID, string EmployeeCode, string RoleID);

    /// <summary>
    /// This Method Is Used To Get Refresh Token Expiration In Days.
    /// </summary>
    /// <returns></returns>
    int GetRefreshTokenExpirationInDays();

    /// <summary>
    /// This Method Is Used To Get Refresh To Store.
    /// </summary>
    /// <param name="UserName"></param>
    /// <param name="EmailID"></param>
    /// <param name="EmployeeCode"></param>
    /// <param name="RoleID"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    RefreshToken GetRefreshTokenStore(string UserName, string EmailID, string EmployeeCode, string RoleID, string Token);

    /// <summary>
    /// This Method Is Used To Validate Refresh Token.
    /// </summary>
    /// <param name="RefreshToken"></param>
    /// <returns></returns>
    ClaimsPrincipal? ValidateRefreshToken(string RefreshToken);
}