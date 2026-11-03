namespace VUSYSTimeProWebAPI.Application.Abstractions.Users;

public interface IUserHelper
{
    /// <summary>
    /// This Method Is Used To Get User Email ID. 
    /// </summary>
    /// <returns>Returns The Email ID of the Logged In User.</returns>
    string GetUserEmailID();

    /// <summary>
    /// This Method Is Used To Get User Login Employee Code. 
    /// </summary>
    /// <returns>Returns The Employee Code of the Logged In User.</returns>
    string GetUserEmployeeCode();

    /// <summary>
    /// This Method Is Used To Get User Login Role ID. 
    /// </summary>
    /// <returns>Returns The Role ID of the Logged In User.</returns>
    string GetUserRoleID();
}