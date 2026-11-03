using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Infrastructure.Persistence.EntityFramework;

public sealed class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    /// <summary>
    /// User List
    /// </summary>
    public DbSet<UsersM> UserList { get; set; }

    /// <summary>
    /// Role List
    /// </summary>
    public DbSet<RolesM> RoleList { get; set; }

    /// <summary>
    /// Country Master List
    /// </summary>
    public DbSet<CountryM> CountryMasterList { get; set; }

    /// <summary>
    /// State Master List
    /// </summary>
    public DbSet<StateM> StateMasterList { get; set; }

    /// <summary>
    /// Employee List
    /// </summary>
    public DbSet<EmployeeM> EmployeeList { get; set; }

    /// <summary>
    /// Employee Type List
    /// </summary>
    public DbSet<EmployeeTypeM> EmployeeTypeMasterList { get; set; }

    /// <summary>
    /// Gender Master List
    /// </summary>
    public DbSet<GenderM> GenderMasterList { get; set; }

    /// <summary>
    /// Holiday Master List
    /// </summary>
    public DbSet<HolidayM> HolidayMasterList { get; set; }

    /// <summary>
    /// Leave Type Master List
    /// </summary>
    public DbSet<LeaveTypeM> LeaveTypeMasterList { get; set; }

    /// <summary>
    /// Configure Email List
    /// </summary>
    public DbSet<ConfigureEmailM> ConfigureEmailList { get; set; }

    /// <summary>
    /// Project List
    /// </summary>
    public DbSet<ProjectM> ProjectList { get; set; }

    /// <summary>
    /// Employee Project Mapping List
    /// </summary>
    public DbSet<EmployeeProjectMappingM> EmployeeProjectMappingList { get; set; }

    /// <summary>
    /// Project Activity List
    /// </summary>
    public DbSet<ProjectActivityM> ProjectActivityList { get; set; }

    /// <summary>
    /// Project Timesheet List
    /// </summary>
    public DbSet<ProjectTimesheet> ProjectTimesheetList { get; set; }

    /// <summary>
    /// Employee Leave Balance List
    /// </summary>
    public DbSet<EmployeeLeaveBalanceM> EmployeeLeaveBalanceList { get; set; }

    /// <summary>
    ///Leave Application List
    /// </summary>
    public DbSet<LeaveApplicationM> LeaveApplicationList { get; set; }

    
}