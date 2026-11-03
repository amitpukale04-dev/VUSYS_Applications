using System.Text.Json.Serialization;

namespace VUSYSAppHub.Models
{
    public sealed class PortalApp
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProductGroup { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string IconCss { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool OpenInNewTab { get; set; }
        public int SortOrder { get; set; }
        public string? AppType { get; set; }          // WebApp, WindowsService, TaskScheduler
        public string? MonitorName { get; set; }      // Service name or task name
        public string? ServerName { get; set; }

        // New field for status indicator
        // Allowed values: Live, Down, Maintenance
        public string Status { get; set; } = "Live";
    }

    public sealed class ApplicationSectionGroup
    {
        public string ApplicationName { get; set; } = string.Empty;
        public List<PortalApp> DevelopmentApps { get; set; } = new();
        public List<PortalApp> StagingApps { get; set; } = new();
        public List<PortalApp> ProductionApps { get; set; } = new();
        public int TotalCount => DevelopmentApps.Count + StagingApps.Count + ProductionApps.Count;
    }

    public sealed class PortalApplicationRoot
    {
        [JsonPropertyName("PortalApplications")]
        public List<PortalApplicationGroup> PortalApplications { get; set; } = new();
    }

    public sealed class PortalApplicationGroup
    {
        [JsonPropertyName("ApplicationName")]
        public string ApplicationName { get; set; } = string.Empty;

        [JsonPropertyName("Environments")]
        public List<PortalEnvironmentGroup> Environments { get; set; } = new();
    }

    public sealed class PortalEnvironmentGroup
    {
        [JsonPropertyName("Environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("Applications")]
        public List<PortalApplicationItem> Applications { get; set; } = new();
    }

    public sealed class PinnedEnvironmentGroup
    {
        public string Environment { get; set; } = string.Empty;
        public List<PortalApp> Apps { get; set; } = new();
    }

    public sealed class PortalApplicationItem
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("ProductGroup")]
        public string ProductGroup { get; set; } = string.Empty;

        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("Url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("IconCss")]
        public string IconCss { get; set; } = string.Empty;

        [JsonPropertyName("Environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("IsActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("OpenInNewTab")]
        public bool OpenInNewTab { get; set; }

        [JsonPropertyName("SortOrder")]
        public int SortOrder { get; set; }
    }

    public sealed class OtherLinksContainer
    {
        public List<PortalApp> OtherLinks { get; set; } = new();
    }

    public sealed class DeploymentVersionViewModel
    {
        public string ApplicationName { get; set; } = "SELECT";
        public string EnvironmentName { get; set; } = "SELECT";
        public string LastVersion { get; set; } = string.Empty;
        public string NewVersion { get; set; } = string.Empty;
    }

    public class ApplicationNameModel
    {
        public string ApplicationName { get; set; } = string.Empty;
    }

    public class UpdateDeploymentVersionModel
    {
        public string ApplicationName { get; set; } = string.Empty;

        public string Environment { get; set; } = string.Empty;

        public string VersionNumber { get; set; } = string.Empty;

        public string? UpdatedBy { get; set; }

        public string? AppUpdateBase64 { get; set; }
    }

    public class EmployeeLoginValidationRequestModel
    {
        public string ApplicationName { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = string.Empty;

        public string LoginID { get; set; } = string.Empty;
    }

    public class EmployeeLoginValidationResultModel
    {
        public bool IsValid { get; set; }

        public string LoginId { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        public string EmailId { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class DeploymentSendOtpModel
    {
        public string ApplicationName { get; set; } = string.Empty;

        public string Environment { get; set; } = string.Empty;

        public string LoginId { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        public string EmailId { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;

        public string ExpiryInMinutes { get; set; } = string.Empty;

        public string NewVersion { get; set; } = string.Empty;
    }

    public class DeploymentVersionOtpStorageModel
    {
        public string LoginId { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        public string EmailId { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;

        public DateTime ExpiryUtc { get; set; }

        public string ApplicationName { get; set; } = string.Empty;

        public string Environment { get; set; } = string.Empty;

        public string NewVersion { get; set; } = string.Empty;
    }
}
