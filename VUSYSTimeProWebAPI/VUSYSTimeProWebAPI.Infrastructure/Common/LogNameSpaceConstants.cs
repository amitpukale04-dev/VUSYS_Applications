namespace VUSYSTimeProWebAPI.Infrastructure.Common
{
    public static class LogNameSpaceConstants
    {
        #region [API Layer]

        /// <summary>
        /// API Layer
        /// </summary>
        public static readonly string APILayer = "VUSYSTimeProWebAPI.Api";

        /// <summary>
        /// Middleware Log Namespace
        /// </summary>
        public static readonly string MiddlewareLogNamespace = $"{APILayer}.Middlewares";

        /// <summary>
        /// Services Log Namespace
        /// </summary>
        public static readonly string ServicesLogNamespace = $"{APILayer}.Services";

        /// <summary>
        /// Files Log Namespace
        /// </summary>
        public static readonly string FilesLogNamespace = $"{ServicesLogNamespace}.Files";

        /// <summary>
        /// File Uploads Log Namespace
        /// </summary>
        public static readonly string FileUploadsLogNamespace = $"{ServicesLogNamespace}.FileUploads";

        /// <summary>
        /// Validation Log Namespace
        /// </summary>
        public static readonly string ValidationLogNamespace = $"{APILayer}.Validation";

        #endregion [API Layer]


        #region [Infrastructure Layer]

        /// <summary>
        /// Infrastructure Layer
        /// </summary>
        public static readonly string InfrastructureLayer = "VUSYSTimeProWebAPI.Infrastructure";

        /// <summary>
        /// Infrastructure Authentication Log Namespace
        /// </summary>
        public static readonly string InfrastructureAuthenticationLogNamespace = $"{InfrastructureLayer}.Authentication";

        /// <summary>
        /// Infrastructure Common Log Namespace
        /// </summary>
        public static readonly string InfrastructureCommonLogNamespace = $"{InfrastructureLayer}.Common";

        /// <summary>
        /// Infrastructure Email Log Namespace
        /// </summary>
        public static readonly string InfrastructureEmailLogNamespace = $"{InfrastructureLayer}.Email";

        /// <summary>
        /// Infrastructure Users Log Namespace
        /// </summary>
        public static readonly string InfrastructureUsersLogNamespace = $"{InfrastructureLayer}.Users";

        /// <summary>
        /// Infrastructure Persistence Connection Log Namespace
        /// </summary>
        public static readonly string InfrastructurePersistenceConnectionsLogNamespace = $"{InfrastructureLayer}.Persistence.Connections";

        /// <summary>
        /// Infrastructure Security Log Namespace
        /// </summary>
        public static readonly string InfrastructureSecurityLogNamespace = $"{InfrastructureLayer}.Security";

        /// <summary>
        /// Infrastructure Services Log Namespace
        /// </summary>
        public static readonly string InfrastructureServicesLogNamespace = $"{InfrastructureLayer}.Services";

        /// <summary>
        /// Infrastructure File Uploads Log Namespace
        /// </summary>
        public static readonly string InfrastructureFileUploadsLogNamespace = $"{InfrastructureServicesLogNamespace}.FileUploads";

        /// <summary>
        /// Infrastructure Modules Log Namespace
        /// </summary>
        public static readonly string InfrastructureModulesLogNamespace = $"{InfrastructureServicesLogNamespace}.Modules";

        /// <summary>
        /// Infrastructure Configuration Log Namespace
        /// </summary>
        public static readonly string InfrastructureConfigurationLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.Configuration";

        /// <summary>
        /// Infrastructure Employee Log Namespace
        /// </summary>
        public static readonly string InfrastructureEmployeeLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.Employee";

        /// <summary>
        /// Infrastructure Leave Management Log Namespace
        /// </summary>
        public static readonly string InfrastructureLeaveManagementLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.LeaveManagement";

        /// <summary>
        /// Infrastructure Master Log Namespace
        /// </summary>
        public static readonly string InfrastructureMasterLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.Master";

        /// <summary>
        /// Infrastructure Project Log Namespace
        /// </summary>
        public static readonly string InfrastructureProjectLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.Project";

        /// <summary>
        /// Infrastructure OTP Service Log Namespace
        /// </summary>
        public static readonly string InfrastructureOTPServiceLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.OTPService";

        /// <summary>
        /// Infrastructure Reports Log Namespace
        /// </summary>
        public static readonly string InfrastructureReportsServiceLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.Reports";

        /// <summary>
        /// Infrastructure Timesheet Log Namespace
        /// </summary>
        public static readonly string InfrastructureTimesheetLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.Timesheet";

        /// <summary>
        /// Infrastructure Users Log Namespace
        /// </summary>
        public static readonly string InfrastructureServiceUsersLogNamespace = $"{InfrastructureServicesLogNamespace}.{InfrastructureModulesLogNamespace}.Users";

        #endregion [Infrastructure Layer]
    }
}
