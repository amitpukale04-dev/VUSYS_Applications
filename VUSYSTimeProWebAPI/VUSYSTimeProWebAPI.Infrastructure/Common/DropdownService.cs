using System.Data;
using System.Globalization;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.EntityFramework;

namespace VUSYSTimeProWebAPI.Infrastructure.Common;

public sealed class DropdownService : IDropdownService
{
    private const string DropdownServiceLogClassName = "DropdownService";
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Dropdown Service Class Constructor.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DropdownService(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    #region [Custom Dropdown Methods]

    /// <summary>
    /// This Method Is Used To Get Approver Count.
    /// </summary>
    /// <param name="UserID"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> GetApproverCountAsync(int UserID, CancellationToken cancellationToken = default)
    {
        int totalCount = 0;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalCount = await _dbContext.UserList.AsNoTracking().CountAsync(c => c.IsApprover == true && c.IsDeleted == false && c.Id != UserID, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetApproverCountAsync()", ex.Message, false);
            throw;
        }
        return totalCount;
    }

    /// <summary>
    /// This Method Is Used To Check For Password Link Available.
    /// </summary>
    /// <param name="EmailID"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> CheckForPasswordLinkAvailable(string EmailID, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        List<string> columns = new List<string> { "Status", "Message" };
        string TableName = "PASSWORD_LINK_AVAILABLE_RESPONSE";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userList = await _dbContext.UserList.AsNoTracking()
                    .Where(c => c.EmailID == EmailID && c.IsDeleted == false)
                    .OrderBy(c => c.FullName)
                    .Select(u => new UsersM
                    {
                        UserEmailID = u.EmailID,
                        IsPasswordExpiryLinkAvailable = u.IsPasswordExpiryLinkAvailable,
                        PasswordExpiryLinkDateTime = u.PasswordExpiryLinkDateTime
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (userList != null && userList.Count > 0)
            {
                bool IsPasswordLinkExpired = userList[0].IsPasswordExpiryLinkAvailable;
                DateTime PasswordLinkExpiredDateTime = userList[0].PasswordExpiryLinkDateTime;
                if (IsPasswordLinkExpired && (PasswordLinkExpiredDateTime >= DateTime.Now))
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), "Password Link Available !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "Password Link Expired !!!" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            else
            {
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "Password Link Expired !!!" }
                };
                finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.CheckForPasswordLinkAvailable()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Gender Name.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> GetGenderNameAsync(string EmployeeCode, CancellationToken cancellationToken = default)
    {
        string GenderName = string.Empty;
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(EmployeeCode);
            cancellationToken.ThrowIfCancellationRequested();

            string? gender = await ( from em in _dbContext.EmployeeList.AsNoTracking()
                        join gm in _dbContext.GenderMasterList.AsNoTracking()
                        on em.GenderID equals gm.Id
                        where em.EmployeeCode == EmployeeCode &&
                        em.IsDeleted == false
                    select gm.Gender
                ).FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(gender))
            {
                GenderName = gender.ToUpperInvariant();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetGenderNameAsync()", ex.Message, false);
            throw;
        }
        return GenderName;
    }

    /// <summary>
    /// This Method Is Used To Get Gender Name.
    /// </summary>
    /// <param name="GenderID"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> GetGenderNameAsync(int GenderID, CancellationToken cancellationToken = default)
    {
        string GenderName = string.Empty;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? gender = await (from g in _dbContext.GenderMasterList.AsNoTracking()
                                    where g.Id == GenderID &&
                                    g.IsDeleted == false
                                    select g.Gender
                ).FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(gender))
            {
                GenderName = gender.ToUpperInvariant();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetGenderNameAsync()", ex.Message, false);
            throw;
        }
        return GenderName;
    }

    /// <summary>
    /// This Method Is Used To Get Employee List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetEmployeeListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var employeeList = await _dbContext.EmployeeList.AsNoTracking()
                    .Where(c => c.IsDeleted == false)
                    .OrderBy(c => c.FullName)
                    .Select(c => new
                    {
                        ValueText = c.EmployeeCode,
                        DisplayText = c.FullName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (employeeList != null && employeeList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(employeeList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetEmployeeListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get User List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetUserListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userList = await _dbContext.UserList.AsNoTracking()
                    .Where(c => c.IsDeleted == true)
                    .OrderBy(c => c.FullName)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.FullName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if(userList != null && userList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(userList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetUserListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Role List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetRoleListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roleList = await _dbContext.RoleList.AsNoTracking()
                    .Where(c => c.IsDeleted == false)
                    .OrderBy(c => c.RoleName)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.RoleName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (roleList != null && roleList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(roleList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetEmployeeListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get User And Role List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetUserAndRoleListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userList =
                await (
                    from user in _dbContext.UserList.AsNoTracking()
                    join role in _dbContext.RoleList.AsNoTracking()
                        on user.RoleID equals role.Id
                    where user.IsDeleted == false
                    orderby user.FullName
                    select new
                    {
                        UserValueText = user.Id,
                        UserDisplayText = user.FullName,
                        RoleValueText = role.Id,
                        RoleDisplayText = role.RoleName
                    }
                )
                .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (userList != null && userList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(userList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetUserAndRoleListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Gender List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetGenderListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var genderList = await _dbContext.GenderMasterList.AsNoTracking()
                    .Where(c => c.IsDeleted == false)
                    .OrderBy(c => c.Gender)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.Gender
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (genderList != null && genderList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(genderList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetGenderListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Country List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetCountryListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var countryList = await _dbContext.CountryMasterList.AsNoTracking()
                    .Where(c => c.IsDeleted == false)
                    .OrderBy(c => c.CountryName)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.CountryName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (countryList != null && countryList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(countryList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetCountryListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get State List.
    /// </summary>
    /// <param name="CountryID"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetStateListAsync(int CountryID, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stateList = await _dbContext.StateMasterList.AsNoTracking()
                    .Where(c => c.CountryID == CountryID && c.IsDeleted == false)
                    .OrderBy(c => c.StateName)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.StateName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (stateList != null && stateList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(stateList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetStateListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Employee Type List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetEmployeeTypeListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var employeeTypeList = await _dbContext.EmployeeTypeMasterList.AsNoTracking()
                    .Where(c => c.IsDeleted == false)
                    .OrderBy(c => c.EmployeeType)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.EmployeeType
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (employeeTypeList != null && employeeTypeList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(employeeTypeList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetEmployeeTypeListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Project List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetProjectListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projectList = await _dbContext.ProjectList.AsNoTracking()
                    .Where(c => c.IsDeleted == false)
                    .OrderBy(c => c.ProjectName)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.ProjectName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (projectList != null && projectList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(projectList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetProjectListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Project List Based on Employee Code.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetProjectListBasedOnEmployeeCodeAsync(string EmployeeCode, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            ArgumentNullException.ThrowIfNull(EmployeeCode);
            cancellationToken.ThrowIfCancellationRequested();
            var projectList =
                await (
                from p in _dbContext.ProjectList.AsNoTracking()
                join epm in _dbContext.EmployeeProjectMappingList.AsNoTracking()
                on p.Id equals epm.ProjectID
                where epm.EmployeeCode == EmployeeCode
                && p.ProjectStatus == "2"
                && epm.IsDeleted == false
                orderby p.ProjectName
                select new
                {
                    ValueText = epm.ProjectID.ToString(),
                    DisplayText = p.ProjectName
                }
                ).ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (projectList != null && projectList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(projectList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetProjectListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get All Project List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetProjectListWithAllAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DropDownData> projectList = await _dbContext.ProjectList.AsNoTracking()
                    .Where(c => c.ProjectCode != "VUSYS_TIMELYAWAY" && c.IsDeleted == false)
                    .OrderBy(c => c.ProjectName)
                    .Select(c => new DropDownData
                    {
                        ValueText = c.Id.ToString(),
                        DisplayText = c.ProjectName
                    })
                    .ToListAsync(cancellationToken);

            projectList.Insert(0, new DropDownData
            {
                ValueText = "0",
                DisplayText = "ALL"
            });

            cancellationToken.ThrowIfCancellationRequested();
            if (projectList != null && projectList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(projectList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetProjectListWithAllAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Project Status List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetProjectStatusListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DropDownData> projectStatusList = new List<DropDownData>();
            projectStatusList.Add(new DropDownData { ValueText = "1", DisplayText = "Not Started" });
            projectStatusList.Add(new DropDownData { ValueText = "2", DisplayText = "In Progress" });
            projectStatusList.Add(new DropDownData { ValueText = "3", DisplayText = "Completed" });
            projectStatusList.Add(new DropDownData { ValueText = "4", DisplayText = "On Hold" });
            cancellationToken.ThrowIfCancellationRequested();
            if (projectStatusList != null && projectStatusList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(projectStatusList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetProjectStatusListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Project Activity List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetProjectActivityListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projectActivityList = await _dbContext.ProjectActivityList.AsNoTracking()
                    .Where(c => !new[] { "ON LEAVE", "HOLIDAY", "COMPOFF" }.Contains(c.ActivityName) && c.IsDeleted == false)
                    .OrderBy(c => c.ActivityName)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.ActivityName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (projectActivityList != null && projectActivityList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(projectActivityList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetProjectActivityListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Project Activity List For On Leave Async.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetProjectActivityListForOnLeaveAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projectActivityList = await _dbContext.ProjectActivityList.AsNoTracking()
                    .Where(c => new[] { "ON LEAVE", "HOLIDAY", "COMPOFF" }.Contains(c.ActivityName) && c.IsDeleted == false)
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.ActivityName
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (projectActivityList != null && projectActivityList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(projectActivityList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetProjectActivityListForOnLeaveAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Employees Working On Project With All List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> EmployeesWorkingOnProjectWithAllListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DropDownData> employeeList =
                await (
                from epm in _dbContext.EmployeeProjectMappingList.AsNoTracking()
                join e in _dbContext.EmployeeList.AsNoTracking()
                on epm.EmployeeCode equals e.EmployeeCode
                group e by new { e.EmployeeCode, e.FullName } into g
                orderby g.Key.FullName
                select new DropDownData
                {
                    ValueText = g.Key.EmployeeCode,
                    DisplayText = g.Key.FullName
                }
                )
                .ToListAsync(cancellationToken);

            employeeList.Insert(0, new DropDownData
            {
                ValueText = "0",
                DisplayText = "ALL"
            });

            cancellationToken.ThrowIfCancellationRequested();
            if (employeeList != null && employeeList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(employeeList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.EmployeesWorkingOnProjectWithAllListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Year List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetYearListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DropDownData> yearList = new List<DropDownData>();
            int currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 4, 5).Reverse().ToList();
            foreach (var year in years)
            {
                yearList.Add(new DropDownData
                {
                    ValueText = year.ToString(),
                    DisplayText = year.ToString()
                });
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (yearList != null && yearList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(yearList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetYearListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Month List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetMonthListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DropDownData> monthList = new List<DropDownData>();
            var monthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;
            foreach (var month in monthNames)
            {
                if (!string.IsNullOrEmpty(month))  // Skip empty entries in the array
                {
                    monthList.Add(new DropDownData { ValueText = month, DisplayText = month });
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (monthList != null && monthList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(monthList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetMonthListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Leave Type List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetLeaveTypeListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var leaveTypeList = await _dbContext.LeaveTypeMasterList.AsNoTracking()
                    .Where(c => c.IsDeleted == false)
                    .OrderBy(c => c.LeaveType)
                    .Select(c => new
                    {
                        ValueText = c.Id,
                        DisplayText = c.LeaveType
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (leaveTypeList != null && leaveTypeList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(leaveTypeList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetLeaveTypeListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Leave Balance Based on Leave Type, Employee Code and Leave Year.
    /// </summary>
    /// <param name="LeaveTypeID"></param>
    /// <param name="EmployeeCode"></param>
    /// <param name="LeaveYear"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetLeaveBalanceBasedOnLeaveTypeAndEmployeeCode(int LeaveTypeID, string EmployeeCode, int LeaveYear, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            ArgumentNullException.ThrowIfNull(EmployeeCode);
            cancellationToken.ThrowIfCancellationRequested();
            decimal AvailableBalance = await (from c in _dbContext.EmployeeLeaveBalanceList.AsNoTracking()
                                    where c.LeaveTypeID == LeaveTypeID && c.EmployeeCode == EmployeeCode && c.LeaveYear == LeaveYear && c.IsDeleted == false
                                    select c.AvailableBalance
                ).FirstOrDefaultAsync(cancellationToken);

            List<string> DetailColumns = new List<string> { "LeaveTypeID", "EmployeeCode", "AvailableBalance", "Status", "Message" };
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { LeaveTypeID.ToString(), EmployeeCode, AvailableBalance.ToString(), Helper.ToProperCase(MessageTypeConstants.Success), "Employee Balance Available." }
            };
            finalDataSet = Helper.GenerateCommonMessage(DetailColumns, rows, "EMPLOYEE_LEAVE_BALANCE_AVAILABLE");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetLeaveBalanceBasedOnLeaveTypeAndEmployeeCode()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Leave Approve Reject Status List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetLeaveApproveRejectStatusListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DropDownData> leaveStatusList = new List<DropDownData>();
            leaveStatusList.Add(new DropDownData { ValueText = ((int)LeaveStatusEnum.Approve).ToString(), DisplayText = LeaveStatusEnum.Approve.ToString() });
            leaveStatusList.Add(new DropDownData { ValueText = ((int)LeaveStatusEnum.Reject).ToString(), DisplayText = LeaveStatusEnum.Reject.ToString() });
            cancellationToken.ThrowIfCancellationRequested();
            if (leaveStatusList != null && leaveStatusList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(leaveStatusList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetLeaveApproveRejectStatusListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Leave Status List.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetLeaveStatusListAsync(CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DropDownData> leaveStatusList = new List<DropDownData>();
            leaveStatusList.Add(new DropDownData { ValueText = ((int)LeaveStatusEnum.Pending).ToString(), DisplayText = LeaveStatusEnum.Pending.ToString() });
            leaveStatusList.Add(new DropDownData { ValueText = ((int)LeaveStatusEnum.Approved).ToString(), DisplayText = LeaveStatusEnum.Approved.ToString() });
            leaveStatusList.Add(new DropDownData { ValueText = ((int)LeaveStatusEnum.Rejected).ToString(), DisplayText = LeaveStatusEnum.Rejected.ToString() });
            leaveStatusList.Add(new DropDownData { ValueText = ((int)LeaveStatusEnum.Cancelled).ToString(), DisplayText = LeaveStatusEnum.Cancelled.ToString() });
            cancellationToken.ThrowIfCancellationRequested();
            if (leaveStatusList != null && leaveStatusList.Count > 0)
            {
                DataTable dataTable = Helper.ConvertListToDataTable(leaveStatusList);
                dataTable.TableName = CommonHelper.DROPDOWN_LIST_RESPONSE;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                DataTable dataTable = new DataTable();
                finalDataSet.Tables.Add(dataTable);
                finalDataSet = Helper.CheckAndValidateDatasetTable(finalDataSet, Helper.ToProperCase(MessageTypeConstants.Error), CommonHelper.DROPDOWN_LIST_RESPONSE);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetLeaveStatusListAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    #endregion [Custom Dropdown Methods]

    #region [Custom Methods]

    /// <summary>
    /// This Method Is Used To Get Approver Details.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<UsersM>> GetApproverDetails(CancellationToken cancellationToken = default)
    {
        List<UsersM> UserDetails = new List<UsersM>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userList = await _dbContext.UserList.AsNoTracking()
                    .Where(c => c.IsApprover == true && c.IsDeleted == false)
                    .OrderBy(c => c.EmailID)
                    .Select(c => new UsersM
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        EmailID = c.EmailID,
                        EmployeeCode = c.EmployeeCode
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (userList != null && userList.Count > 0)
            {
                UserDetails = userList.ToList();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetApproverDetails()", ex.Message, false);
            throw;
        }
        return UserDetails;
    }

    /// <summary>
    /// This Method Is Used To Get User Details By Employee Code.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<UsersM>> GetUserDetailsByEmployeeCode(string EmployeeCode, CancellationToken cancellationToken = default)
    {
        List<UsersM> UserDetails = new List<UsersM>();
        try
        {
            ArgumentNullException.ThrowIfNull(EmployeeCode);
            cancellationToken.ThrowIfCancellationRequested();
            var userList = await _dbContext.UserList.AsNoTracking()
                    .Where(c => c.EmployeeCode == EmployeeCode && c.IsDeleted == false)
                    .OrderBy(c => c.EmailID)
                    .Select(c => new UsersM
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        EmailID = c.EmailID
                    })
                    .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (userList != null && userList.Count > 0)
            {
                UserDetails = userList.ToList();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetUserDetailsByEmployeeCode()", ex.Message, false);
            throw;
        }
        return UserDetails;
    }

    /// <summary>
    /// This Method Is Used To Check Timesheet Approved.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="TaskDate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> CheckTimesheetApprovedAsync(string EmployeeCode, DateTime? TaskDate = null, CancellationToken cancellationToken = default)
    {
        bool IsApproved = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTime effectiveDate = TaskDate ?? DateTime.Now;
            string TimesheetMonth = DateTimeHelper.GetMonthName(effectiveDate.Month);
            int TimesheetYear = effectiveDate.Year;
            IsApproved = await (from c in _dbContext.ProjectTimesheetList.AsNoTracking()
                                    where c.EmployeeCode == EmployeeCode && c.TimesheetMonth == TimesheetMonth && c.TimesheetYear == TimesheetYear && !c.IsDeleted
                                    select c.IsApproved
                ).FirstOrDefaultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.CheckTimesheetApprovedAsync()", ex.Message, false);
            throw;
        }
        return IsApproved;
    }

    /// <summary>
    /// This Method Is Used To Check Timesheet Approved.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="TimesheetMonth"></param>
    /// <param name="TimesheetYear"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> CheckTimesheetApprovedAsync(string EmployeeCode, string TimesheetMonth, int TimesheetYear, CancellationToken cancellationToken = default)
    {
        bool IsApproved = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsApproved = await (from c in _dbContext.ProjectTimesheetList.AsNoTracking()
                                where c.EmployeeCode == EmployeeCode && c.TimesheetMonth == TimesheetMonth && c.TimesheetYear == TimesheetYear && !c.IsDeleted
                                select c.IsApproved
                ).FirstOrDefaultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.CheckTimesheetApprovedAsync()", ex.Message, false);
            throw;
        }
        return IsApproved;
    }

    /// <summary>
    /// This Method Is Used To Check Timesheet Sent For Approval.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="TaskDate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> CheckTimesheetSentForApprovalAsync(string EmployeeCode, DateTime? TaskDate = null, CancellationToken cancellationToken = default)
    {
        bool IsSentForApproval = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTime effectiveDate = TaskDate ?? DateTime.Now;
            string TimesheetMonth = DateTimeHelper.GetMonthName(effectiveDate.Month);
            int TimesheetYear = effectiveDate.Year;
            IsSentForApproval = await (
                from c in _dbContext.ProjectTimesheetList.AsNoTracking()
                where c.EmployeeCode == EmployeeCode &&
                c.TimesheetMonth == TimesheetMonth &&
                c.TimesheetYear == TimesheetYear &&
                !c.IsDeleted &&
                c.IsSentForApproval
                select (c.IsApproved == true) ||
                (c.IsApproved == false && c.ApprovedOrRejectedReason == null)
                ).FirstOrDefaultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.CheckTimesheetSentForApprovalAsync()", ex.Message, false);
            throw;
        }
        return IsSentForApproval;
    }

    /// <summary>
    /// This Method Is Used To Check Timesheet Sent For Approval.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="TimesheetMonth"></param>
    /// <param name="TimesheetYear"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> CheckTimesheetSentForApprovalAsync(string EmployeeCode, string TimesheetMonth, int TimesheetYear, CancellationToken cancellationToken = default)
    {
        bool IsSentForApproval = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsSentForApproval = await (
                from c in _dbContext.ProjectTimesheetList.AsNoTracking()
                where c.EmployeeCode == EmployeeCode &&
                c.TimesheetMonth == TimesheetMonth &&
                c.TimesheetYear == TimesheetYear &&
                !c.IsDeleted &&
                c.IsSentForApproval
                select (c.IsApproved == true) ||
                (c.IsApproved == false && c.ApprovedOrRejectedReason == null)
                ).FirstOrDefaultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.CheckTimesheetSentForApprovalAsync()", ex.Message, false);
            throw;
        }
        return IsSentForApproval;
    }

    /// <summary>
    /// This Method Is Used To Check Current Month Timesheet Closed Status.
    /// </summary>
    /// <param name="EmployeeCode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> CheckCurrentMonthTimesheetClosedStatusAsync(string EmployeeCode, CancellationToken cancellationToken = default)
    {
        bool IsClosed = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            string TimesheetMonth = DateTimeHelper.GetMonthName(DateTime.Now.Month);
            int TimesheetYear = DateTime.Now.Year;
            IsClosed = await (from c in _dbContext.ProjectTimesheetList.AsNoTracking()
                                where c.EmployeeCode == EmployeeCode && c.TimesheetMonth == TimesheetMonth && c.TimesheetYear == TimesheetYear && !c.IsDeleted
                                select c.IsTimesheetClosed
                ).FirstOrDefaultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.CheckCurrentMonthTimesheetClosedStatusAsync()", ex.Message, false);
            throw;
        }
        return IsClosed;
    }

    /// <summary>
    /// This Method Is Used To Check Leave Request Status.
    /// </summary>
    /// <param name="LeaveRequestID"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> CheckLeaveRequestStatusAsync(int LeaveRequestID, CancellationToken cancellationToken = default)
    {
        int LeaveStatus = 0;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            LeaveStatus = await (from l in _dbContext.LeaveApplicationList.AsNoTracking()
                                 where l.Id == LeaveRequestID &&
                                 l.IsDeleted == false
                                 select l.LeaveStatus
                ).FirstOrDefaultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetApproverCountAsync()", ex.Message, false);
            throw;
        }
        return LeaveStatus;
    }

    #endregion [Custom Methods]

    #region [Employee Type]

    /// <summary>
    /// This Method Is Used To Save And Update Employee Type Details.
    /// </summary>
    /// <param name="_objEmployeeTypeM"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> SaveAndUpdateEmployeeTypeDetails(EmployeeTypeM _objEmployeeTypeM, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        List<string> columns = new List<string> { "ID", "Status", "Message" };
        string TableName = "EMPLOYEE_TYPE_DETAILS_RESPONSE";
        try
        {
            ArgumentNullException.ThrowIfNull(_objEmployeeTypeM);
            cancellationToken.ThrowIfCancellationRequested();
            if (_objEmployeeTypeM.Id == 0)
            {
                bool isExists = await _dbContext.EmployeeTypeMasterList.AsNoTracking().AnyAsync(x => x.EmployeeType == _objEmployeeTypeM.EmployeeType && x.IsDeleted == false, cancellationToken);
                if (isExists)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objEmployeeTypeM.Id.ToString(), Helper.ToProperCase(MessageTypeConstants.Error), "The Employee Type Details Already Exists." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }

                EmployeeTypeM mapping = new EmployeeTypeM
                {
                    EmployeeType = _objEmployeeTypeM.EmployeeType,
                    CreatedBy = _objEmployeeTypeM.CreatedBy,
                    CreatedDate = DateTime.Now,
                    UpdatedBy = 0,
                    UpdatedDate = Convert.ToDateTime(DateTimeFormatConstants.DB_DEFAULT_DATETIME),
                    IsDeleted = false
                };
                await _dbContext.EmployeeTypeMasterList.AddAsync(mapping, cancellationToken);
                int affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);
                if (affectedRows > 0)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { mapping.Id.ToString(), affectedRows > 0 ? Helper.ToProperCase(MessageTypeConstants.Success) : Helper.ToProperCase(MessageTypeConstants.Error), affectedRows > 0 ? "Employee Type Added Successfully." : "Failed To Add Employee Type" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            else
            {
                EmployeeTypeM? existingData = await _dbContext.EmployeeTypeMasterList.FirstOrDefaultAsync(x => x.Id == _objEmployeeTypeM.Id, cancellationToken);
                if (existingData == null)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objEmployeeTypeM.Id.ToString(), Helper.ToProperCase(MessageTypeConstants.Error), "Employee Type Details Not Found." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }

                bool isExists = await _dbContext.EmployeeTypeMasterList
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id != _objEmployeeTypeM.Id &&
                             x.EmployeeType == _objEmployeeTypeM.EmployeeType &&
                             x.IsDeleted == false,
                        cancellationToken);

                if (isExists)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objEmployeeTypeM.Id.ToString(), Helper.ToProperCase(MessageTypeConstants.Error), "The Employee Type Details Already Exists." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }

                existingData.EmployeeType = _objEmployeeTypeM.EmployeeType;
                existingData.IsDeleted = _objEmployeeTypeM.IsDeleted;
                existingData.UpdatedBy = _objEmployeeTypeM.UpdatedBy;
                existingData.UpdatedDate = DateTime.Now;
                int affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

                if (affectedRows > 0)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { existingData.Id.ToString(), affectedRows > 0 ? Helper.ToProperCase(MessageTypeConstants.Success) : Helper.ToProperCase(MessageTypeConstants.Error), affectedRows > 0 ? "Employee Type Updated Successfully." : "Failed To Update Employee Type" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.SaveAndUpdateEmployeeTypeDetails()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Employee Type Data.
    /// </summary>
    /// <param name="_objEmployeeTypeDetailsRequestParam"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetEmployeeTypeDataAsync(EmployeeTypeDetailsRequestParam _objEmployeeTypeDetailsRequestParam, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        string DetailTable = "EMPLOYEE_TYPE_RESPONSE";
        string ListTable = "EMPLOYEE_TYPE_LIST_RESPONSE";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = _dbContext.EmployeeTypeMasterList.AsNoTracking().AsQueryable();
            if (_objEmployeeTypeDetailsRequestParam.Id > 0)
            {
                query = query.Where(x => x.Id == _objEmployeeTypeDetailsRequestParam.Id);
            }

            var employeeTypeData = await query
                .OrderBy(x => x.EmployeeType)
                .Select(x => new
                {
                    x.Id,
                    x.EmployeeType,
                    x.IsDeleted
                })
                .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (employeeTypeData != null && employeeTypeData.Count > 0)
            {
                List<EmployeeTypeDataList> employeeTypeList = employeeTypeData
                    .Select((item, index) => new EmployeeTypeDataList
                    {
                        Status = Helper.ToProperCase(MessageTypeConstants.Success),
                        Id = item.Id,
                        EmployeeType = item.EmployeeType,
                        IsDeleted = item.IsDeleted,
                        ActiveStatus = item.IsDeleted == false ? "Active": "Inactive"
                    })
                    .ToList();

                DataTable dataTable = Helper.ConvertListToDataTable(employeeTypeList);
                dataTable.TableName = _objEmployeeTypeDetailsRequestParam.Id > 0 ? DetailTable : ListTable;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                if(_objEmployeeTypeDetailsRequestParam.Id > 0)
                {
                    List<string> DetailColumns = new List<string> { "ID", "Status", "Message" };
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objEmployeeTypeDetailsRequestParam.Id.ToString(), Helper.ToProperCase(MessageTypeConstants.Error), "Employee Type Details Not Found." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(DetailColumns, rows, DetailTable);
                }
                else
                {
                    List<string> ListColumns = new List<string> { "Status", "Message" };
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "Employee Type List Not Found." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(ListColumns, rows, ListTable);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetEmployeeTypeDataAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    #endregion [Employee Type]

    #region [Employee Project Mapping]

    /// <summary>
    /// This Method Is Used To Save And Update Employee Project Mapping Details.
    /// </summary>
    /// <param name="_objEmployeeProjectMappingM"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> SaveAndUpdateEmployeeProjectMappingDetails(EmployeeProjectMappingM _objEmployeeProjectMappingM, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        List<string> columns = new List<string> { "ID", "Status", "Message" };
        string TableName = "EMPLOYEE_PROJECT_MAPPING_DETAILS_RESPONSE";
        try
        {
            ArgumentNullException.ThrowIfNull(_objEmployeeProjectMappingM);
            cancellationToken.ThrowIfCancellationRequested();
            if (_objEmployeeProjectMappingM.Id == 0)
            {
                bool isExists = await _dbContext.EmployeeProjectMappingList
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.ProjectID == _objEmployeeProjectMappingM.ProjectID &&
                             x.EmployeeCode == _objEmployeeProjectMappingM.EmployeeCode &&
                             x.IsDeleted == false,
                        cancellationToken);

                if (isExists)
                {
                    List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { _objEmployeeProjectMappingM.Id.ToString(), Helper.ToProperCase(MessageTypeConstants.Error), "The Employee Project Mapping Details Already Exists." }
                };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }

                EmployeeProjectMappingM mapping = new EmployeeProjectMappingM
                {
                    EmployeeCode = _objEmployeeProjectMappingM.EmployeeCode,
                    ProjectID = _objEmployeeProjectMappingM.ProjectID,
                    IsNotifyToEmployee = _objEmployeeProjectMappingM.IsNotifyToEmployee,
                    CreatedBy = _objEmployeeProjectMappingM.CreatedBy,
                    CreatedDate = DateTime.Now,
                    UpdatedBy = 0,
                    UpdatedDate = Convert.ToDateTime(DateTimeFormatConstants.DB_DEFAULT_DATETIME),
                    IsDeleted = false
                };
                await _dbContext.EmployeeProjectMappingList.AddAsync(mapping, cancellationToken);
                int affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);
                if (affectedRows > 0)
                {
                    List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { mapping.Id.ToString(), affectedRows > 0 ? Helper.ToProperCase(MessageTypeConstants.Success) : Helper.ToProperCase(MessageTypeConstants.Error), affectedRows > 0 ? "Employee Project Mapping Added Successfully." : "Failed To Add Employee Project Mapping" }
                };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            else
            {
                EmployeeProjectMappingM? existingData = await _dbContext.EmployeeProjectMappingList.FirstOrDefaultAsync(x => x.Id == _objEmployeeProjectMappingM.Id, cancellationToken);
                if (existingData == null)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objEmployeeProjectMappingM.Id.ToString(), Helper.ToProperCase(MessageTypeConstants.Error), "Employee Project Mapping Details Not Found." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }

                bool isExists = await _dbContext.EmployeeProjectMappingList
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id != _objEmployeeProjectMappingM.Id &&
                             x.ProjectID == _objEmployeeProjectMappingM.ProjectID &&
                             x.EmployeeCode == _objEmployeeProjectMappingM.EmployeeCode &&
                             x.IsDeleted == false,
                        cancellationToken);

                if (isExists)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objEmployeeProjectMappingM.Id.ToString(), Helper.ToProperCase(MessageTypeConstants.Error), "The Employee Project Mapping Details Already Exists." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    return finalDataSet;
                }

                existingData.EmployeeCode = _objEmployeeProjectMappingM.EmployeeCode;
                existingData.ProjectID = _objEmployeeProjectMappingM.ProjectID;
                existingData.IsNotifyToEmployee = _objEmployeeProjectMappingM.IsNotifyToEmployee;
                existingData.IsDeleted = _objEmployeeProjectMappingM.IsDeleted;
                existingData.UpdatedBy = _objEmployeeProjectMappingM.UpdatedBy;
                existingData.UpdatedDate = DateTime.Now;
                int affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

                if (affectedRows > 0)
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { existingData.Id.ToString(), affectedRows > 0 ? Helper.ToProperCase(MessageTypeConstants.Success) : Helper.ToProperCase(MessageTypeConstants.Error), affectedRows > 0 ? "Employee Project Mapping Updated Successfully." : "Failed To Update Employee Project Mapping" }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.SaveAndUpdateEmployeeProjectMappingDetails()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    /// <summary>
    /// This Method Is Used To Get Employee Project Mapping Data.
    /// </summary>
    /// <param name="_objEmployeeProjectMappingDetailsRequestParam"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DataSet> GetEmployeeProjectMappingDataAsync(EmployeeProjectMappingDetailsRequestParam _objEmployeeProjectMappingDetailsRequestParam, CancellationToken cancellationToken = default)
    {
        DataSet finalDataSet = new DataSet();
        string DetailTable = "EMPLOYEE_PROJECT_MAPPING_RESPONSE";
        string ListTable = "EMPLOYEE_PROJECT_MAPPING_LIST_RESPONSE";
        try
        {
            ArgumentNullException.ThrowIfNull(_objEmployeeProjectMappingDetailsRequestParam);
            cancellationToken.ThrowIfCancellationRequested();

            var query = from employeeProjectMapping in _dbContext.EmployeeProjectMappingList.AsNoTracking()
                        join projectMaster in _dbContext.ProjectList.AsNoTracking()
                        on employeeProjectMapping.ProjectID equals projectMaster.Id
                        join employeeMaster in _dbContext.EmployeeList.AsNoTracking()
                        on employeeProjectMapping.EmployeeCode equals employeeMaster.EmployeeCode
                        select new
                        {
                            EmployeeProjectMappingID = employeeProjectMapping.Id,
                            employeeProjectMapping.ProjectID,
                            employeeProjectMapping.EmployeeCode,
                            projectMaster.ProjectCode,
                            projectMaster.ProjectName,
                            projectMaster.ClientName,
                            projectMaster.ProjectDescription,
                            projectMaster.StartDate,
                            projectMaster.EndDate,
                            EmployeeName = employeeMaster.FullName,
                            EmployeeEmailID = employeeMaster.EmailID,
                            employeeProjectMapping.IsNotifyToEmployee,
                            employeeProjectMapping.IsDeleted
                        };

            if (_objEmployeeProjectMappingDetailsRequestParam.Id > 0)
            {
                query = query.Where(x => x.EmployeeProjectMappingID == _objEmployeeProjectMappingDetailsRequestParam.Id);
            }

            if (_objEmployeeProjectMappingDetailsRequestParam.ProjectID > 0)
            {
                query = query.Where(x => x.ProjectID == _objEmployeeProjectMappingDetailsRequestParam.ProjectID);
            }

            if (!string.IsNullOrWhiteSpace(_objEmployeeProjectMappingDetailsRequestParam.EmployeeCode))
            {
                query = query.Where(x => x.EmployeeCode == _objEmployeeProjectMappingDetailsRequestParam.EmployeeCode);
            }

            var employeeProjectMappingData = await query
                .OrderBy(x => x.ProjectName)
                .ThenBy(x => x.EmployeeName)
                .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (employeeProjectMappingData != null && employeeProjectMappingData.Count > 0)
            {
                List<EmployeeProjectMappingDataList> empProjectMappingList =
                    employeeProjectMappingData
                        .Select((item, index) =>
                            new EmployeeProjectMappingDataList
                            {
                                Status = Helper.ToProperCase(MessageTypeConstants.Success),
                                EmployeeProjectMappingID = item.EmployeeProjectMappingID,
                                ProjectID = item.ProjectID,
                                ProjectCode = item.ProjectCode ?? string.Empty,
                                ProjectName = item.ProjectName ?? string.Empty,
                                ClientName = item.ClientName ?? string.Empty,
                                ProjectDescription = item.ProjectDescription ?? string.Empty,
                                StartDate = item.StartDate.ToString(DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT),
                                EndDate = item.EndDate.ToString(DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT),
                                EmployeeCode = item.EmployeeCode ?? string.Empty,
                                EmployeeName = item.EmployeeName ?? string.Empty,
                                EmployeeEmailID = item.EmployeeEmailID ?? string.Empty,
                                IsNotifyToEmployee = item.IsNotifyToEmployee,
                                IsDeleted = item.IsDeleted,
                                ActiveStatus = item.IsDeleted == false ? "Active" : "Inactive"
                            })
                        .ToList();

                DataTable dataTable = Helper.ConvertListToDataTable(empProjectMappingList);
                bool isDetailRequest = _objEmployeeProjectMappingDetailsRequestParam.Id > 0 || (_objEmployeeProjectMappingDetailsRequestParam.ProjectID > 0 && !string.IsNullOrWhiteSpace(_objEmployeeProjectMappingDetailsRequestParam.EmployeeCode));
                dataTable.TableName = isDetailRequest ? DetailTable : ListTable;
                finalDataSet.Tables.Add(dataTable);
            }
            else
            {
                bool isDetailRequest = _objEmployeeProjectMappingDetailsRequestParam.Id > 0 || (_objEmployeeProjectMappingDetailsRequestParam.ProjectID > 0 && !string.IsNullOrWhiteSpace(_objEmployeeProjectMappingDetailsRequestParam.EmployeeCode));

                if (isDetailRequest)
                {
                    List<string> DetailColumns = new List<string> { "ID", "ProjectID", "EmployeeCode", "Status", "Message" };
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { _objEmployeeProjectMappingDetailsRequestParam.Id.ToString(), _objEmployeeProjectMappingDetailsRequestParam.ProjectID.ToString(), _objEmployeeProjectMappingDetailsRequestParam.EmployeeCode, Helper.ToProperCase(MessageTypeConstants.Error), "Employee Project Mapping Details Not Found." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(DetailColumns, rows, DetailTable);
                }
                else
                {
                    List<string> ListColumns = new List<string> { "Status", "Message" };
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "Employee Project Mapping List Not Found." }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(ListColumns, rows, ListTable);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{DropdownServiceLogClassName}.GetEmployeeProjectMappingDataAsync()", ex.Message, false);
            throw;
        }
        return finalDataSet;
    }

    #endregion [Employee Project Mapping]
}