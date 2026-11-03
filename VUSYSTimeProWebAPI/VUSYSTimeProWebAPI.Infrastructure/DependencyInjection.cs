using CommonLibCore10;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.OTPService;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.EntityFramework;
using SimpleCrypto;
using VUSYSTimeProWebAPI.Application.Abstractions.Authentication;
using VUSYSTimeProWebAPI.Application.Abstractions.Caching;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.FileUploads;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Master;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Users;
using VUSYSTimeProWebAPI.Application.Abstractions.Persistence;
using VUSYSTimeProWebAPI.Application.Abstractions.Security;
using VUSYSTimeProWebAPI.Application.Abstractions.Users;
using VUSYSTimeProWebAPI.Application.Common.Options;
using VUSYSTimeProWebAPI.Application.Models.Authentication;
using VUSYSTimeProWebAPI.Infrastructure.Authentication;
using VUSYSTimeProWebAPI.Infrastructure.Caching;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Email;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Connections;
using VUSYSTimeProWebAPI.Infrastructure.Security;
using VUSYSTimeProWebAPI.Infrastructure.Services.FileUploads;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Master;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.OTPService;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Users;
using VUSYSTimeProWebAPI.Infrastructure.Users;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Configuration;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Configuration;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Employee;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Employee;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Project;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Project;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Timesheet;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Timesheet;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Reports;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.Reports;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.LeaveManagement;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.LeaveManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.FrontEndUserTabs;
using VUSYSTimeProWebAPI.Infrastructure.Services.Modules.FrontEndUserTabs;

namespace VUSYSTimeProWebAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {

        // Configuration
        services.Configure<JwtSettings>(configuration.GetSection("JWTKeys"));

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();

        // Database
        services.AddSingleton<IDbConnectionHelper, DbConnectionHelper>();
        services.AddSingleton<IDatabaseWarmUpService, DatabaseWarmUpService>();

        // --------------------------------------------------
        // Entity Framework Core
        // --------------------------------------------------

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            IDbConnectionHelper dbConnectionHelper = serviceProvider.GetRequiredService<IDbConnectionHelper>();

        string decryptedConnectionString = dbConnectionHelper.GetDBConnectionStringAsync("DevOrProd").GetAwaiter().GetResult();

        if (string.IsNullOrWhiteSpace(decryptedConnectionString))
        {
            throw new InvalidOperationException("The database connection string could not be loaded or decrypted.");
        }
            options.UseSqlServer(decryptedConnectionString);
        });

        // Dropdown
        services.AddScoped<IDropdownService, DropdownService>();

        // Authentication
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ITokenStoreService, TokenStoreService>();

        // Crypto Service
        services.AddScoped<ICryptoService, PBKDF2>();
        
        // Security
        services.AddScoped<IPasswordHashService, PasswordHashService>();

        // Email
        services.AddScoped<ISMTPCredentialService, SMTPCredentialService>();
        services.AddScoped<IEmailService, EmailService>();

        // Users
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // SQLDB helper
        services.AddScoped<SQLDBHelper>(serviceProvider =>
        {
            IDbConnectionHelper dbConnectionHelper =
                serviceProvider.GetRequiredService<IDbConnectionHelper>();

            ICurrentUserService currentUserService =
                serviceProvider.GetRequiredService<ICurrentUserService>();

            MyApplicationKeysOptions applicationKeys =
                serviceProvider.GetRequiredService<MyApplicationKeysOptions>();

            string decryptedConnectionString =
                dbConnectionHelper
                    .GetDBConnectionStringAsync("DevOrProd")
                    .GetAwaiter()
                    .GetResult();

            if (string.IsNullOrWhiteSpace(decryptedConnectionString))
            {
                throw new InvalidOperationException(
                    "The database connection string could not be loaded or decrypted.");
            }

            string userName =
                string.IsNullOrWhiteSpace(currentUserService.EmailId)
                    ? "UnknownUser"
                    : currentUserService.EmailId;

            bool requestDbScriptLoggingEnabled =
                Convert.ToBoolean(
                    applicationKeys.RequestDBScriptLoggingEnabled);

            return new SQLDBHelper(decryptedConnectionString, userName, requestDbScriptLoggingEnabled, applicationKeys.APIRequestDBScriptLog);
        });

        services.AddScoped<IUserHelper, UserHelper>();

        // Content Type
        services.AddSingleton<IContentTypeService, ContentTypeService>();

        // File Upload
        services.AddSingleton<IFileUploadService, FileUploadService>();

        // User Shared
        services.AddScoped<IUserSharedService, UserSharedService>();

        // User Login
        services.AddScoped<IUserLoginService, UserLoginService>();

        // User Tab
        services.AddScoped<IUserTabsService, UserTabsService>();

        // OTP
        services.AddScoped<IOTPService, OTPService>();

        // Master
        services.AddScoped<IMasterService, MasterService>();

        // Configuration
        services.AddScoped<IConfigurationService, ConfigurationService>();

        // Employee
        services.AddScoped<IEmployeeService, EmployeeService>();

        // Project
        services.AddScoped<IProjectService, ProjectService>();

        // Timesheet
        services.AddScoped<ITimesheetService, TimesheetService>();

        // Leave Management
        services.AddScoped<ILeaveManagementService, LeaveManagementService>();

        // Report
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}