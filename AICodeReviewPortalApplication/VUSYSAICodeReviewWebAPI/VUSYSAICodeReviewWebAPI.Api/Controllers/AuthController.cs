using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VUSYSAICodeReviewWebAPI.Api.Controllers.BaseControllers;
using VUSYSAICodeReviewWebAPI.Api.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(AuthenticationService authentication) : BaseApiController
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public LoginResponse Login(LoginRequest request) => authentication.Login(request);
}

[Route("api/health")]
public sealed class HealthController : BaseApiController
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
