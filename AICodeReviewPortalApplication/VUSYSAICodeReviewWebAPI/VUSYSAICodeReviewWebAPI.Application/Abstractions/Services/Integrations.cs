using System.Text.Json;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Domain;

namespace VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;

public interface IGitHubService
{
    void EnsureAllowed(string owner, string repo);
    Task<List<Repository>> Repositories(int? page, int pageSize, CancellationToken ct);
    Task<Repository> Repository(string owner, string repo, CancellationToken ct);
    Task<List<JsonElement>> Branches(string owner, string repo, int? page, int pageSize, CancellationToken ct);
    Task<List<Pull>> Pulls(string owner, string repo, string state, int? page, int pageSize, CancellationToken ct);
    Task<Pull> Pull(string owner, string repo, int number, CancellationToken ct);
    Task<List<JsonElement>> Commits(string owner, string repo, int number, int? page, int pageSize, CancellationToken ct);
    Task<PullFiles> Files(string owner, string repo, int number, CancellationToken ct);
    Task<FileComparison> Content(string owner, string repo, int number, string fileName, CancellationToken ct);
    Task<long> Publish(string owner, string repo, int number, string sha, string body, InlineComment[] comments, CancellationToken ct);
}

public interface IPullRequestActions
{
    Task<PullActionStatus> Status(string owner, string repo, int number, CancellationToken ct);
    Task<JsonElement> Threads(string owner, string repo, int number, string? cursor, CancellationToken ct);
    Task<JsonElement> ThreadComments(string owner, string repo, int number, string threadId, string? cursor, CancellationToken ct);
    Task UpdateThread(string owner, string repo, int number, string threadId, string? reply, CancellationToken ct);
    Task<PullActionResult> Execute(string owner, string repo, int number, string sha, bool merge, CancellationToken ct);
}

public interface IReviewEmail
{
    Task<(string Status, string Message)> Notify(Pull pull, string repository, long reviewId, string body, CancellationToken ct);
}

public interface IGeminiService
{
    Task<(string Summary, List<Finding> Findings)> Review(string diff, CancellationToken ct);
}
