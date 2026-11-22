using System.Diagnostics;

namespace VUSYSAICodeReviewWebAPI.Api.Middlewares;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        long started = Stopwatch.GetTimestamp();
        context.Response.OnCompleted(() =>
        {
            logger.LogInformation("{Method} returned {Status} in {ElapsedMs} ms ({TraceId})",
                context.Request.Method, context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                context.TraceIdentifier);
            return Task.CompletedTask;
        });
        await next(context);
    }
}
