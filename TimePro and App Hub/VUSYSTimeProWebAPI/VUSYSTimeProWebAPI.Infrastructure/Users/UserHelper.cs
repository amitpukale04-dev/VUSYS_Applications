using CommonLibCore10;
using VUSYSTimeProWebAPI.Application.Abstractions.Users;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Users;

public sealed class UserHelper : IUserHelper
{
    private readonly ICurrentUserService _currentUserService;
    private const string UserHelperLogClassName = "UserHelper";

    public UserHelper(ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(currentUserService);
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// This Method Is Used To Get User Email ID. 
    /// </summary>
    /// <returns>Returns The Email ID of the Logged In User.</returns>
    public string GetUserEmailID()
    {
        string ReturnEmailID = string.Empty;
        try
        {
            ReturnEmailID = _currentUserService.EmailId;
        }
        catch (Exception ex)
        {
            ReturnEmailID = string.Empty;
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureUsersLogNamespace, $"{UserHelperLogClassName}.GetUserEmailID()", ex.Message, false);
        }
        return ReturnEmailID;
    }

    /// <summary>
    /// This Method Is Used To Get User Login Employee Code. 
    /// </summary>
    /// <returns>Returns The Employee Code of the Logged In User.</returns>
    public string GetUserEmployeeCode()
    {
        string ReturnEmployeeCode = string.Empty;
        try
        {
            ReturnEmployeeCode = _currentUserService.EmployeeCode;
        }
        catch (Exception ex)
        {
            ReturnEmployeeCode = string.Empty;
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureUsersLogNamespace, $"{UserHelperLogClassName}.GetUserEmployeeCode()", ex.Message, false);
        }
        return ReturnEmployeeCode;
    }

    /// <summary>
    /// This Method Is Used To Get User Login Role ID. 
    /// </summary>
    /// <returns>Returns The Role ID of the Logged In User.</returns>
    public string GetUserRoleID()
    {
        string ReturnRoleID = string.Empty;
        try
        {
            ReturnRoleID = _currentUserService.RoleId;
        }
        catch (Exception ex)
        {
            ReturnRoleID = string.Empty;
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureUsersLogNamespace, $"{UserHelperLogClassName}.GetUserRoleID()", ex.Message, false);
        }
        return ReturnRoleID;
    }
}