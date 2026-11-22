using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VUSYSAICodeReviewWebAPI.Api.Controllers.BaseControllers;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Api.Controllers;

[Route("api/PullRequests/{owner}/{repository}/{pullRequestNumber:int}")]
[Route("api/repositories/{owner}/{repository}/pulls/{pullRequestNumber:int}")]
public sealed class PullRequestsController(IGitHubService gitHub) : BaseApiAuthorizationController
{
    [HttpGet]
    public Task<Pull> Get(string owner, string repository, int pullRequestNumber)
        => gitHub.Pull(owner, repository, pullRequestNumber, RequestCancellationToken);

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
