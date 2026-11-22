using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VUSYSAICodeReviewWebAPI.Api.Controllers.BaseControllers;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;

namespace VUSYSAICodeReviewWebAPI.Api.Controllers;

[Route("api/repositories")]
public sealed class RepositoriesController(IGitHubService gitHub) : BaseApiAuthorizationController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, Range(1, 1000000)] int? page,
        [FromQuery, Range(1, 100)] int pageSize = 100)
        => Ok(await gitHub.Repositories(page, pageSize, RequestCancellationToken));

    [HttpGet("{owner}/{repo}")]
    public async Task<IActionResult> Get(string owner, string repo)
        => Ok(await gitHub.Repository(owner, repo, RequestCancellationToken));

    [HttpGet("{owner}/{repo}/branches")]
    public async Task<IActionResult> Branches(string owner, string repo, [FromQuery, Range(1, 1000000)] int? page,
        [FromQuery, Range(1, 100)] int pageSize = 100)
        => Ok(await gitHub.Branches(owner, repo, page, pageSize, RequestCancellationToken));

    [HttpGet("{owner}/{repo}/pulls")]
    public async Task<IActionResult> Pulls(string owner, string repo, [FromQuery] string state = "open",
        [FromQuery, Range(1, 1000000)] int? page = null, [FromQuery, Range(1, 100)] int pageSize = 100)
        => Ok(await gitHub.Pulls(owner, repo, state, page, pageSize, RequestCancellationToken));
}
