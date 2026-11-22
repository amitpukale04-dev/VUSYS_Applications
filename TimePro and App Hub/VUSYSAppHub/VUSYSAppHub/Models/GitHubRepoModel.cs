namespace VUSYSAppHub.Models
{
    public class GitHubRepoModel
    {
    }

    public class GitHubBillingUsageResultModel
    {
        public decimal CurrentMeteredUsage { get; set; }
        public decimal CurrentIncludedUsage { get; set; }

        public decimal ActionsMinutesUsed { get; set; }
        public decimal ActionsMinutesIncluded { get; set; } = 2000;

        public decimal ActionsStorageUsed { get; set; }
        public decimal ActionsStorageIncluded { get; set; } = 0.5m;

        public int DaysRemainingInMonth { get; set; }

        public int TotalRepositories { get; set; }
        public int TotalWorkflowRuns { get; set; }
        public int TotalArtifacts { get; set; }

        public string UsageDateText { get; set; } = string.Empty;
        public string SummaryRawJson { get; set; } = string.Empty;
        public string UsageRawJson { get; set; } = string.Empty;

        public List<GitHubBillingUsageItemModel> UsageItems { get; set; } = new();
        public List<GitHubWorkflowUsageModel> WorkflowUsageItems { get; set; } = new();
    }

    public class GitHubBillingUsageItemModel
    {
        public string Product { get; set; } = string.Empty;
        public string DisplayProduct { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Quantity { get; set; }
        public string UnitType { get; set; } = string.Empty;
    }

    public class GitHubRepositoryUsageModel
    {
        public string RepositoryName { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class GitHubWorkflowUsageModel
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public int TotalRuns { get; set; }
        public int SuccessRuns { get; set; }
        public int FailedRuns { get; set; }
        public decimal RuntimeMinutes { get; set; }
    }

    public class GitHubProductTabModel
    {
        public GitHubProductTabModel()
        {
        }

        public GitHubProductTabModel(string key, string displayName, string icon)
        {
            Key = key;
            DisplayName = displayName;
            Icon = icon;
        }

        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public sealed class GitHubRepositoryInternalModel
    {
        public string Name { get; set; } = string.Empty;
        public string OwnerLogin { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool Private { get; set; }
    }

    public sealed class GitHubWorkflowRunInternalModel
    {
        public long RunId { get; set; }
        public string RepositoryName { get; set; } = string.Empty;
        public string RepositoryFullName { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Conclusion { get; set; } = string.Empty;
        public string RunnerType { get; set; } = string.Empty;
        public decimal RuntimeMinutes { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class GitHubWorkflowJobInternalModel
    {
        public long JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string RunnerType { get; set; } = string.Empty;
        public decimal RuntimeMinutes { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public sealed class GitHubArtifactInternalModel
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string RepositoryFullName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Expired { get; set; }
    }
}
