using VUSYSAppHub.Models;

namespace VUSYSAppHub.Contracts
{
    public interface IGitHubService
    {
        Task<GitHubBillingUsageResultModel> GetPersonalBillingUsageAsync(string timeFrame);
    }
}
