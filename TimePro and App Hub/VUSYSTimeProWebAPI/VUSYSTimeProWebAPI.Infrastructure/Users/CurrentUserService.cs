using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VUSYSTimeProWebAPI.Application.Abstractions.Users;

namespace VUSYSTimeProWebAPI.Infrastructure.Users;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Email Id
    /// </summary>
    public string EmailId => GetContextItem("email_id", "EmailID") ?? GetClaimValue("email_id", "EmailID", "EmailId", ClaimTypes.NameIdentifier, "sub") ?? "UnknownUser";

    /// <summary>
    /// User Name
    /// </summary>
    public string? UserName => GetContextItem("user_name", "UserName") ?? GetClaimValue("user_name", "UserName", ClaimTypes.Name, "name");

    /// <summary>
    /// Employee Code
    /// </summary>
    public string? EmployeeCode => GetContextItem("employee_code", "EmployeeCode") ?? GetClaimValue("employee_code", "EmployeeCode", "employeecode");

    /// <summary>
    /// Role Id
    /// </summary>
    public string RoleId => GetContextItem("role_id", "RoleID") ?? GetClaimValue("role_id", "RoleID", "RoleId", ClaimTypes.NameIdentifier, "sub") ?? "UnknownUser";

    /// <summary>
    /// Is Authenticated
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    /// <summary>
    /// This Method Is Used To Get Context Item.
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    private string? GetContextItem(params string[] keys)
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        foreach (string key in keys)
        {
            if (context.Items.TryGetValue(key, out object? value) && value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return value.ToString();
            }
        }
        return null;
    }

    /// <summary>
    /// This Method Is Used To Get Claim Value.
    /// </summary>
    /// <param name="claimTypes"></param>
    /// <returns></returns>
    private string? GetClaimValue(params string[] claimTypes)
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        foreach (string claimType in claimTypes)
        {
            string? claimValue = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(claimValue))
            {
                return claimValue;
            }
        }
        return null;
    }
}