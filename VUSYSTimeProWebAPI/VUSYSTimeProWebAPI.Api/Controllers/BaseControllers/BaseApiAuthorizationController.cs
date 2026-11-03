using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Filters;

namespace VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;

[Route("api/[controller]/[action]")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(JwtAuthorizationFilter))]
public abstract class BaseApiAuthorizationController : ControllerBase
{
    protected CancellationToken RequestCancellationToken => HttpContext.RequestAborted;

    protected const string ControllerLogNamespace = "VUSYSTimeProWebAPI.Api.Controllers";
}