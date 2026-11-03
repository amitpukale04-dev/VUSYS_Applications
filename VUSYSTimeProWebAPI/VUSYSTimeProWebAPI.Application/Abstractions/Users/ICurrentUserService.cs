namespace VUSYSTimeProWebAPI.Application.Abstractions.Users;

public interface ICurrentUserService
{
    /// <summary>
    /// Get Email Id
    /// </summary>
    string EmailId { get; }

    /// <summary>
    /// User Name
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Employee Code
    /// </summary>
    string? EmployeeCode { get; }

    /// <summary>
    /// RoleId
    /// </summary>
    string? RoleId { get; }

    /// <summary>
    /// Is Authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}