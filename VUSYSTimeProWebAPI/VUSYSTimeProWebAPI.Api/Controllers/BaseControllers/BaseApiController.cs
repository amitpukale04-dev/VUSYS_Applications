using Microsoft.AspNetCore.Mvc;

namespace VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;

[ApiController]
[Route("api/[controller]/[action]")]
public abstract class BaseApiController : ControllerBase
{
    protected CancellationToken RequestCancellationToken => HttpContext.RequestAborted;
    protected const string ControllerLogNamespace = "VUSYSTimeProWebAPI.Api.Controllers";
}