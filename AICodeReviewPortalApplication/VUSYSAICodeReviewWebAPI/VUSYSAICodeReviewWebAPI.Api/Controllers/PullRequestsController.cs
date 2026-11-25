using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VUSYSAICodeReviewWebAPI.Api.Controllers.BaseControllers;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Api.Controllers;

[Route("api/PullRequests/{owner}/{repository}/{pullRequestNumber:int}")]
[Route("api/repositories/{owner}/{repository}/pulls/{pullRequestNumber:int}")]
public sealed class PullRequestsController(IGitHubService gitHub, IPullRequestActions actions) : BaseApiAuthorizationController
{
    [HttpGet]
    public Task<Pull> Get(string owner, string repository, int pullRequestNumber)
        => gitHub.Pull(owner, repository, pullRequestNumber, RequestCancellationToken);

    [HttpGet("ActionStatus")]
    public Task<PullActionStatus> ActionStatus(string owner, string repository, int pullRequestNumber)
        => actions.Status(owner, repository, pullRequestNumber, RequestCancellationToken);

    [HttpPost("Approve")]
    [RequestSizeLimit(2048)]
    public Task<PullActionResult> Approve(string owner, string repository, int pullRequestNumber,
        PullActionRequest request)
        => actions.Execute(owner, repository, pullRequestNumber, request.HeadSha, false, RequestCancellationToken);

    [HttpPost("Merge")]
    [RequestSizeLimit(2048)]
    public Task<PullActionResult> Merge(string owner, string repository, int pullRequestNumber,
        PullActionRequest request)
        => actions.Execute(owner, repository, pullRequestNumber, request.HeadSha, true, RequestCancellationToken);

    [HttpGet("ReviewThreads")]
    public Task<System.Text.Json.JsonElement> ReviewThreads(string owner, string repository, int pullRequestNumber,
        [FromQuery, StringLength(512)] string? cursor)
        => actions.Threads(owner, repository, pullRequestNumber, cursor, RequestCancellationToken);

    [HttpGet("ReviewThreadComments")]
    public Task<System.Text.Json.JsonElement> ReviewThreadComments(string owner, string repository, int pullRequestNumber,
        [FromQuery, Required, StringLength(256)] string threadId, [FromQuery, StringLength(512)] string? cursor)
        => actions.ThreadComments(owner, repository, pullRequestNumber, threadId, cursor, RequestCancellationToken);

    [HttpPost("ReviewThreads/Reply")]
    public async Task<IActionResult> Reply(string owner, string repository, int pullRequestNumber, ReviewReplyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = "Enter a reply." });
        await actions.UpdateThread(owner, repository, pullRequestNumber, request.ThreadId, request.Body.Trim(), RequestCancellationToken);
        return NoContent();
    }

    [HttpPost("ReviewThreads/Resolve")]
    public async Task<IActionResult> Resolve(string owner, string repository, int pullRequestNumber, ReviewThreadRequest request)
    {
        await actions.UpdateThread(owner, repository, pullRequestNumber, request.ThreadId, null, RequestCancellationToken);
        return NoContent();
    }

    [HttpGet("Commits")]
    public async Task<IActionResult> Commits(string owner, string repository, int pullRequestNumber,
        [FromQuery, Range(1, 1000000)] int? page, [FromQuery, Range(1, 100)] int pageSize = 100)
        => Ok(await gitHub.Commits(owner, repository, pullRequestNumber, page, pageSize, RequestCancellationToken));

    /// <summary>Returns GitHub changed files and parsed unified diff hunks for the current PR comparison.</summary>
    [HttpGet("Files")]
    public Task<PullFiles> Files(string owner, string repository, int pullRequestNumber)
        => gitHub.Files(owner, repository, pullRequestNumber, RequestCancellationToken);

    [HttpGet("FileContent")]
    public Task<FileComparison> FileContent(string owner, string repository, int pullRequestNumber,
        [FromQuery, Required, StringLength(4096)] string fileName)
        => gitHub.Content(owner, repository, pullRequestNumber, fileName, RequestCancellationToken);
}
