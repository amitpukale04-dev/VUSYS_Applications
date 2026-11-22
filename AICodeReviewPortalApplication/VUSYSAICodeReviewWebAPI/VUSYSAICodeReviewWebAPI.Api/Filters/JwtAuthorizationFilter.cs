using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;

namespace VUSYSAICodeReviewWebAPI.Api.Filters;

public sealed class JwtAuthorizationFilter(AuthOptions options) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!string.Equals(context.HttpContext.User.Identity?.Name, options.AdminUser, StringComparison.Ordinal))
            context.Result = new ObjectResult(new { error = "Access denied." }) { StatusCode = 403 };
    }
}
