namespace VUSYSAICodeReviewWebAPI.Api.Middlewares;

public sealed class UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["User"] = context.User.Identity?.Name,
            ["TraceId"] = context.TraceIdentifier
        });
        await next(context);
    }
}
