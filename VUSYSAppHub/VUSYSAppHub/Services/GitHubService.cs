using System.Globalization;
using System.Net;
using System.Text.Json;
using CommonLib;
using VUSYSAppHub.Contracts;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly HttpClient _httpClient;

        private const decimal LinuxRunnerRate = 0.008m;
        private const decimal WindowsRunnerRate = 0.016m;
        private const decimal MacosRunnerRate = 0.08m;

        public GitHubService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("GitHubBillingClient");
        }

        public async Task<GitHubBillingUsageResultModel> GetPersonalBillingUsageAsync(string timeFrame)
        {
            try
            {
                var billingPeriod = GetBillingPeriod(timeFrame);

                var username = CommonHelper.GitHubKeyOption.Username;
                var token = CommonHelper.GitHubKeyOption.Token;

                if (string.IsNullOrWhiteSpace(username))
                    throw new Exception("GitHubBilling:Username is missing in appsettings.json.");

                if (string.IsNullOrWhiteSpace(token))
                    throw new Exception("GitHubBilling:Token is missing in appsettings.json.");

                var result = new GitHubBillingUsageResultModel
                {
                    UsageDateText = $"{billingPeriod.Start:MMMM d} - {billingPeriod.End:MMMM d, yyyy}",
                    DaysRemainingInMonth = Math.Max(0, (billingPeriod.End.Date - DateTime.Today).Days),
                    UsageItems = new List<GitHubBillingUsageItemModel>(),
                    WorkflowUsageItems = new List<GitHubWorkflowUsageModel>()
                };

                var repositories = await GetRepositoriesAsync();

                result.TotalRepositories = repositories.Count;

                var allRuns = new List<GitHubWorkflowRunInternalModel>();
                var allArtifacts = new List<GitHubArtifactInternalModel>();

                foreach (var repo in repositories)
                {
                    var runs = await GetWorkflowRunsAsync(
                        repo.OwnerLogin,
                        repo.Name,
                        billingPeriod.Start,
                        billingPeriod.End);

                    foreach (var run in runs)
                    {
                        run.RuntimeMinutes = Math.Round(run.RuntimeMinutes, 2);
                        run.EstimatedCost = Math.Round(run.RuntimeMinutes * WindowsRunnerRate, 2);
                        run.RunnerType = "windows";
                    }

                    var artifacts = await GetArtifactsAsync(
                        repo.OwnerLogin,
                        repo.Name,
                        billingPeriod.Start,
                        billingPeriod.End);

                    allRuns.AddRange(runs);
                    allArtifacts.AddRange(artifacts);
                }

                result.TotalWorkflowRuns = allRuns.Count;
                result.TotalArtifacts = allArtifacts.Count;

                result.ActionsMinutesUsed = Math.Round(allRuns.Sum(x => x.RuntimeMinutes), 2);
                result.ActionsStorageUsed = Math.Round(ConvertBytesToGb(allArtifacts.Sum(x => x.SizeInBytes)), 4);

                result.CurrentMeteredUsage = Math.Round(allRuns.Sum(x => x.EstimatedCost), 2);
                result.CurrentIncludedUsage = result.CurrentMeteredUsage;

                BuildRepositoryUsageItems(result, allRuns, allArtifacts);
                BuildWorkflowUsageItems(result, allRuns);

                result.SummaryRawJson = CreateSummaryRawJson(result);
                result.UsageRawJson = result.SummaryRawJson;

                return result;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetPersonalBillingUsageAsync()", ex.Message, false);
                throw new Exception($"Failed to fetch GitHub Actions usage. {ex.Message}");
            }
        }

        private async Task<List<GitHubRepositoryInternalModel>> GetRepositoriesAsync()
        {
            try
            {
                var repositories = new List<GitHubRepositoryInternalModel>();
                var page = 1;

                while (true)
                {
                    var url = $"/user/repos?visibility=all&affiliation=owner&sort=updated&per_page=100&page={page}";
                    var json = await SendGitHubRequestAsync(_httpClient, url);

                    if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
                        break;

                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        break;

                    var currentPageCount = 0;

                    foreach (var repoElement in doc.RootElement.EnumerateArray())
                    {
                        var repoName = GetString(repoElement, "name");
                        var ownerLogin = string.Empty;

                        if (TryGetPropertyIgnoreCase(repoElement, "owner", out var ownerElement))
                            ownerLogin = GetString(ownerElement, "login") ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(repoName) && !string.IsNullOrWhiteSpace(ownerLogin))
                        {
                            repositories.Add(new GitHubRepositoryInternalModel
                            {
                                Name = repoName,
                                OwnerLogin = ownerLogin,
                                FullName = GetString(repoElement, "full_name") ?? $"{ownerLogin}/{repoName}",
                                Private = GetBool(repoElement, "private")
                            });

                            currentPageCount++;
                        }
                    }

                    if (currentPageCount < 100)
                        break;

                    page++;
                }

                return repositories;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetRepositoriesAsync()", ex.Message, false);
                throw new Exception($"Failed to load GitHub repositories. {ex.Message}");
            }
        }

        private async Task<List<GitHubWorkflowRunInternalModel>> GetWorkflowRunsAsync(string owner, string repo, DateTime startDate, DateTime endDate)
        {
            try
            {
                var runs = new List<GitHubWorkflowRunInternalModel>();
                var page = 1;

                while (true)
                {
                    var createdFilter = $"{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}";
                    var url = $"/repos/{owner}/{repo}/actions/runs?created={Uri.EscapeDataString(createdFilter)}&per_page=100&page={page}";

                    var json = await SendGitHubOptionalRequestAsync(_httpClient, url);

                    if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
                        break;

                    using var doc = JsonDocument.Parse(json);

                    if (!TryGetPropertyIgnoreCase(doc.RootElement, "workflow_runs", out var runsElement) ||
                        runsElement.ValueKind != JsonValueKind.Array)
                        break;

                    var currentPageCount = 0;

                    foreach (var runElement in runsElement.EnumerateArray())
                    {
                        var createdAt = GetDateTime(runElement, "created_at");
                        var updatedAt = GetDateTime(runElement, "updated_at");

                        decimal runtimeMinutes = 0;

                        if (createdAt.HasValue && updatedAt.HasValue && updatedAt.Value > createdAt.Value)
                            runtimeMinutes = Convert.ToDecimal((updatedAt.Value - createdAt.Value).TotalMinutes);

                        runs.Add(new GitHubWorkflowRunInternalModel
                        {
                            RunId = GetLong(runElement, "id"),
                            RepositoryName = repo,
                            RepositoryFullName = $"{owner}/{repo}",
                            WorkflowName = GetString(runElement, "name") ?? "Unknown Workflow",
                            Status = GetString(runElement, "status") ?? string.Empty,
                            Conclusion = GetString(runElement, "conclusion") ?? string.Empty,
                            RuntimeMinutes = Math.Round(runtimeMinutes, 2),
                            EstimatedCost = Math.Round(runtimeMinutes * WindowsRunnerRate, 2),
                            RunnerType = "windows",
                            CreatedAt = createdAt,
                            UpdatedAt = updatedAt
                        });

                        currentPageCount++;
                    }

                    if (currentPageCount < 100)
                        break;

                    page++;
                }

                return runs;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetWorkflowRunsAsync()", ex.Message, false);
                throw new Exception($"Failed to load workflow runs for repository {owner}/{repo}. {ex.Message}");
            }
        }

        private async Task<List<GitHubWorkflowJobInternalModel>> GetWorkflowRunJobsAsync(string owner, string repo, long runId)
        {
            try
            {
                var jobs = new List<GitHubWorkflowJobInternalModel>();

                if (runId <= 0)
                    return jobs;

                var page = 1;

                while (true)
                {
                    var url = $"/repos/{owner}/{repo}/actions/runs/{runId}/jobs?per_page=100&page={page}";
                    var json = await SendGitHubOptionalRequestAsync(_httpClient, url);

                    if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
                        break;

                    using var doc = JsonDocument.Parse(json);

                    if (!TryGetPropertyIgnoreCase(doc.RootElement, "jobs", out var jobsElement) ||
                        jobsElement.ValueKind != JsonValueKind.Array)
                        break;

                    var currentPageCount = 0;

                    foreach (var jobElement in jobsElement.EnumerateArray())
                    {
                        var startedAt = GetDateTime(jobElement, "started_at");
                        var completedAt = GetDateTime(jobElement, "completed_at");

                        decimal runtimeMinutes = 0;

                        if (startedAt.HasValue && completedAt.HasValue && completedAt.Value > startedAt.Value)
                            runtimeMinutes = Convert.ToDecimal((completedAt.Value - startedAt.Value).TotalMinutes);

                        var labels = GetStringArray(jobElement, "labels");
                        var runnerType = GetRunnerType(labels);
                        var rate = GetRunnerRate(runnerType);
                        var estimatedCost = Math.Round(runtimeMinutes * rate, 2);

                        jobs.Add(new GitHubWorkflowJobInternalModel
                        {
                            JobId = GetLong(jobElement, "id"),
                            JobName = GetString(jobElement, "name") ?? "Unknown Job",
                            RunnerType = runnerType,
                            RuntimeMinutes = Math.Round(runtimeMinutes, 2),
                            EstimatedCost = estimatedCost,
                            StartedAt = startedAt,
                            CompletedAt = completedAt
                        });

                        currentPageCount++;
                    }

                    if (currentPageCount < 100)
                        break;

                    page++;
                }

                return jobs;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetWorkflowRunJobsAsync()", ex.Message, false);
                return new List<GitHubWorkflowJobInternalModel>();
            }
        }

        private async Task<List<GitHubArtifactInternalModel>> GetArtifactsAsync(string owner, string repo, DateTime startDate, DateTime endDate)
        {
            try
            {
                var artifacts = new List<GitHubArtifactInternalModel>();
                var page = 1;

                while (true)
                {
                    var url = $"/repos/{owner}/{repo}/actions/artifacts?per_page=100&page={page}";
                    var json = await SendGitHubOptionalRequestAsync(_httpClient, url);

                    if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
                        break;

                    using var doc = JsonDocument.Parse(json);

                    if (!TryGetPropertyIgnoreCase(doc.RootElement, "artifacts", out var artifactsElement) ||
                        artifactsElement.ValueKind != JsonValueKind.Array)
                        break;

                    var currentPageCount = 0;

                    foreach (var artifactElement in artifactsElement.EnumerateArray())
                    {
                        var createdAt = GetDateTime(artifactElement, "created_at");

                        if (createdAt.HasValue &&
                            createdAt.Value.Date >= startDate.Date &&
                            createdAt.Value.Date <= endDate.Date)
                        {
                            artifacts.Add(new GitHubArtifactInternalModel
                            {
                                RepositoryName = repo,
                                RepositoryFullName = $"{owner}/{repo}",
                                Name = GetString(artifactElement, "name") ?? "Unknown Artifact",
                                SizeInBytes = GetLong(artifactElement, "size_in_bytes"),
                                CreatedAt = createdAt,
                                Expired = GetBool(artifactElement, "expired")
                            });
                        }

                        currentPageCount++;
                    }

                    if (currentPageCount < 100)
                        break;

                    page++;
                }

                return artifacts;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetArtifactsAsync()", ex.Message, false);
                throw new Exception($"Failed to load artifacts for repository {owner}/{repo}. {ex.Message}");
            }
        }

        private static async Task<string> SendGitHubRequestAsync(HttpClient client, string url)
        {
            try
            {
                using var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"GitHub API failed: {(int)response.StatusCode} {response.ReasonPhrase}. {json}");

                return json;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.SendGitHubRequestAsync()", ex.Message, false);
                throw new Exception($"Failed to send GitHub API request. {ex.Message}");
            }
        }

        private static async Task<string> SendGitHubOptionalRequestAsync(HttpClient client, string url)
        {
            try
            {
                using var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.SendGitHubOptionalRequestAsync()", $"Optional GitHub API skipped: {(int)response.StatusCode} {response.ReasonPhrase}. {json}", false);
                    return "{}";
                }

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"GitHub API failed: {(int)response.StatusCode} {response.ReasonPhrase}. {json}");

                return json;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.SendGitHubOptionalRequestAsync()", ex.Message, false);
                return "{}";
            }
        }

        private static void BuildRepositoryUsageItems(
            GitHubBillingUsageResultModel result,
            List<GitHubWorkflowRunInternalModel> allRuns,
            List<GitHubArtifactInternalModel> allArtifacts)
        {
            try
            {
                result.UsageItems.Clear();

                var repoRuntimeItems = allRuns
                    .GroupBy(x => x.RepositoryFullName)
                    .Select(g => new GitHubBillingUsageItemModel
                    {
                        Product = "actions",
                        DisplayProduct = "Actions",
                        Sku = "Actions Runtime",
                        RepositoryName = g.Key,
                        GrossAmount = Math.Round(g.Sum(x => x.EstimatedCost), 2),
                        NetAmount = 0,
                        Quantity = Math.Round(g.Sum(x => x.RuntimeMinutes), 2),
                        UnitType = "USD"
                    })
                    .OrderByDescending(x => x.GrossAmount)
                    .ToList();

                var repoArtifactItems = allArtifacts
                    .GroupBy(x => x.RepositoryFullName)
                    .Select(g => new GitHubBillingUsageItemModel
                    {
                        Product = "actions",
                        DisplayProduct = "Artifact Storage",
                        Sku = "Artifact Storage GB",
                        RepositoryName = g.Key,
                        GrossAmount = 0,
                        NetAmount = 0,
                        Quantity = Math.Round(ConvertBytesToGb(g.Sum(x => x.SizeInBytes)), 4),
                        UnitType = "GB"
                    })
                    .ToList();

                result.UsageItems.AddRange(repoRuntimeItems);
                result.UsageItems.AddRange(repoArtifactItems);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.BuildRepositoryUsageItems()", ex.Message, false);
                throw new Exception($"Failed to build GitHub repository usage items. {ex.Message}");
            }
        }

        private static void BuildWorkflowUsageItems(GitHubBillingUsageResultModel result, List<GitHubWorkflowRunInternalModel> allRuns)
        {
            try
            {
                result.WorkflowUsageItems = allRuns
                    .GroupBy(x => new { x.RepositoryFullName, x.WorkflowName })
                    .Select(g => new GitHubWorkflowUsageModel
                    {
                        RepositoryName = g.Key.RepositoryFullName,
                        WorkflowName = g.Key.WorkflowName,
                        TotalRuns = g.Count(),
                        SuccessRuns = g.Count(x => string.Equals(x.Conclusion, "success", StringComparison.OrdinalIgnoreCase)),
                        FailedRuns = g.Count(x =>
                            string.Equals(x.Conclusion, "failure", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x.Conclusion, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x.Conclusion, "timed_out", StringComparison.OrdinalIgnoreCase)),
                        RuntimeMinutes = Math.Round(g.Sum(x => x.RuntimeMinutes), 2)
                    })
                    .OrderByDescending(x => x.RuntimeMinutes)
                    .ToList();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.BuildWorkflowUsageItems()", ex.Message, false);
                throw new Exception($"Failed to build GitHub workflow usage items. {ex.Message}");
            }
        }

        private static string CreateSummaryRawJson(GitHubBillingUsageResultModel result)
        {
            try
            {
                var rawObject = new
                {
                    totalRepositories = result.TotalRepositories,
                    totalWorkflowRuns = result.TotalWorkflowRuns,
                    totalArtifacts = result.TotalArtifacts,
                    actionsRuntimeMinutes = result.ActionsMinutesUsed,
                    actionsStorageGb = result.ActionsStorageUsed,
                    estimatedGrossAmount = result.CurrentMeteredUsage,
                    estimatedDiscountAmount = result.CurrentIncludedUsage,
                    estimatedBillableAmount = 0,
                    usageDateText = result.UsageDateText,
                    repositoryUsage = result.UsageItems,
                    workflowUsage = result.WorkflowUsageItems
                };

                return JsonSerializer.Serialize(rawObject, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.CreateSummaryRawJson()", ex.Message, false);
                throw new Exception($"Failed to create GitHub Actions usage raw JSON. {ex.Message}");
            }
        }

        private static (int Year, int Month, DateTime Start, DateTime End) GetBillingPeriod(string timeFrame)
        {
            try
            {
                var today = DateTime.Today;

                var targetDate = string.Equals(timeFrame, "previous", StringComparison.OrdinalIgnoreCase)
                    ? today.AddMonths(-1)
                    : today;

                var start = new DateTime(targetDate.Year, targetDate.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);

                return (targetDate.Year, targetDate.Month, start, end);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetBillingPeriod()", ex.Message, false);
                throw new Exception($"Failed to prepare GitHub usage period. {ex.Message}");
            }
        }

        private static string GetRunnerType(List<string> labels)
        {
            try
            {
                if (labels == null || labels.Count == 0)
                    return "windows";

                var labelText = string.Join(" ", labels).ToLowerInvariant();

                if (labelText.Contains("self-hosted"))
                    return "self-hosted";

                if (labelText.Contains("windows"))
                    return "windows";

                if (labelText.Contains("macos") || labelText.Contains("mac"))
                    return "macos";

                if (labelText.Contains("ubuntu") || labelText.Contains("linux"))
                    return "linux";

                return "windows";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetRunnerType()", ex.Message, false);
                return "windows";
            }
        }

        private static decimal GetRunnerRate(string runnerType)
        {
            try
            {
                return runnerType?.ToLowerInvariant() switch
                {
                    "linux" => LinuxRunnerRate,
                    "windows" => WindowsRunnerRate,
                    "macos" => MacosRunnerRate,
                    "self-hosted" => 0,
                    _ => WindowsRunnerRate
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetRunnerRate()", ex.Message, false);
                return WindowsRunnerRate;
            }
        }

        private static decimal ConvertBytesToGb(long bytes)
        {
            try
            {
                if (bytes <= 0)
                    return 0;

                return Convert.ToDecimal(bytes) / 1024 / 1024 / 1024;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.ConvertBytesToGb()", ex.Message, false);
                throw new Exception($"Failed to convert artifact bytes to GB. {ex.Message}");
            }
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            try
            {
                if (!TryGetPropertyIgnoreCase(element, propertyName, out var property))
                    return null;

                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.GetRawText(),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetString()", ex.Message, false);
                throw new Exception($"Failed to read string value from GitHub response. {ex.Message}");
            }
        }

        private static List<string> GetStringArray(JsonElement element, string propertyName)
        {
            try
            {
                var values = new List<string>();

                if (!TryGetPropertyIgnoreCase(element, propertyName, out var property))
                    return values;

                if (property.ValueKind != JsonValueKind.Array)
                    return values;

                foreach (var item in property.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var value = item.GetString();

                        if (!string.IsNullOrWhiteSpace(value))
                            values.Add(value);
                    }
                }

                return values;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetStringArray()", ex.Message, false);
                return new List<string>();
            }
        }

        private static bool GetBool(JsonElement element, string propertyName)
        {
            try
            {
                if (!TryGetPropertyIgnoreCase(element, propertyName, out var property))
                    return false;

                if (property.ValueKind == JsonValueKind.True)
                    return true;

                if (property.ValueKind == JsonValueKind.False)
                    return false;

                if (property.ValueKind == JsonValueKind.String &&
                    bool.TryParse(property.GetString(), out var boolValue))
                    return boolValue;

                return false;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetBool()", ex.Message, false);
                throw new Exception($"Failed to read boolean value from GitHub response. {ex.Message}");
            }
        }

        private static long GetLong(JsonElement element, string propertyName)
        {
            try
            {
                if (!TryGetPropertyIgnoreCase(element, propertyName, out var property))
                    return 0;

                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var longValue))
                    return longValue;

                if (property.ValueKind == JsonValueKind.String &&
                    long.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var stringValue))
                    return stringValue;

                return 0;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetLong()", ex.Message, false);
                throw new Exception($"Failed to read long value from GitHub response. {ex.Message}");
            }
        }

        private static DateTime? GetDateTime(JsonElement element, string propertyName)
        {
            try
            {
                if (!TryGetPropertyIgnoreCase(element, propertyName, out var property))
                    return null;

                if (property.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(property.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dateTimeValue))
                    return dateTimeValue;

                return null;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.GetDateTime()", ex.Message, false);
                throw new Exception($"Failed to read datetime value from GitHub response. {ex.Message}");
            }
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            try
            {
                value = default;

                if (element.ValueKind != JsonValueKind.Object)
                    return false;

                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                value = default;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "GitHubService.TryGetPropertyIgnoreCase()", ex.Message, false);
                throw new Exception($"Failed to find property in GitHub response. {ex.Message}");
            }
        }
    }
}