using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VUSYSAICodeReviewWebAPI.Api.Filters;

namespace VUSYSAICodeReviewWebAPI.Api.Controllers.BaseControllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected CancellationToken RequestCancellationToken => HttpContext.RequestAborted;
}

[Authorize]
[ServiceFilter(typeof(JwtAuthorizationFilter))]
public abstract class BaseApiAuthorizationController : BaseApiController { }
