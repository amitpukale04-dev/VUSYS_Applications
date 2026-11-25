using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using VUSYSAICodeReviewWebAPI.Api.Controllers.BaseControllers;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;
using VUSYSAICodeReviewWebAPI.Application.Features;
using VUSYSAICodeReviewWebAPI.Domain;

namespace VUSYSAICodeReviewWebAPI.Api.Controllers;

[Route("api/reviews")]
public sealed class ReviewsController(ReviewService reviews, IOptions<GitHubOptions> githubOptions) : BaseApiAuthorizationController
{
    [HttpGet]
    public Task<List<ReviewResult>> List([FromQuery] string? repository,
        [FromQuery, Range(1, int.MaxValue)] int? pullNumber,
        [FromQuery, RegularExpression("Critical|High|Medium|Low|Informational")] string? severity,
        [FromQuery, Range(1, 1000000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 100,
        [FromQuery] bool checkOutdated = false)
        => reviews.List(githubOptions.Value.AllowedRepositories, repository, pullNumber, severity, page,
            pageSize, checkOutdated, RequestCancellationToken);

    [HttpGet("{id:long}")]
    public Task<ReviewResult> Get(long id) => reviews.Get(id, RequestCancellationToken);

    [HttpPost("analyze")]
    [EnableRateLimiting("ai")]
    public Task<ReviewResult> Analyze(ReviewRequest request) => reviews.Analyze(request, RequestCancellationToken);

    [HttpPost("publish/preview")]
    public Task<object> Preview(PublishRequest request) => reviews.Preview(request, RequestCancellationToken);

    [HttpPost("manual/publish")]
    [RequestSizeLimit(200_000)]
    public async Task<IActionResult> PublishManual(ManualReviewRequest request)
    {
        var publication = await reviews.PublishManual(request, RequestCancellationToken);
        return Ok(publication);
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish(PublishRequest request)
    {
        var publication = await reviews.Publish(request, RequestCancellationToken);
        return Ok(publication);
    }
}
