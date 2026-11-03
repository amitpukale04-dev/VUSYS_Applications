using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace VUSYSTimeProWebApp.Models
{
    public class AppDbContext(DbContextOptions<AppDbContext> options):IdentityDbContext<IdentityUser>(options)
    {
        /// <summary>
        /// User List
        /// </summary>
        public DbSet<UsersM> UserList { get; set; }

        /// <summary>
        /// Employee List
        /// </summary>
        public DbSet<EmployeeM> EmployeeList { get; set; }

        /// <summary>
        /// Employee Type List
        /// </summary>
        public DbSet<EmployeeTypeM> EmployeeTypeMasterList { get; set; }

        /// <summary>
        /// Project Timesheet List
        /// </summary>
        public DbSet<ProjectTimesheet> ProjectTimesheetList { get; set; }

        /// <summary>
        /// Project List
        /// </summary>
        public DbSet<ProjectM> ProjectList { get; set; }

        /// <summary>
        /// Employee Project Mapping List
        /// </summary>
        public DbSet<EmployeeProjectMappingM> EmployeeProjectMappingList { get; set; }

        /// <summary>
        /// Role List
        /// </summary>
        public DbSet<RolesM> RoleList { get; set; }

        /// <summary>
        /// Configure Email List
        /// </summary>
        public DbSet<ConfigureEmailM> ConfigureEmailList { get; set; }

        /// <summary>
        /// Gender Master List
        /// </summary>
        public DbSet<GenderM> GenderMasterList { get; set; }

        /// <summary>
        /// Country Master List
        /// </summary>
        public DbSet<CountryM> CountryMasterList { get; set; }

        /// <summary>
        /// State Master List
        /// </summary>
        public DbSet<StateM> StateMasterList { get; set; }

        /// <summary>
        /// Project Activity List
        /// </summary>
        public DbSet<ProjectActivityM> ProjectActivityList { get; set; }
    }

}
