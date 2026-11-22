using Microsoft.Data.SqlClient;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Api.Middlewares;

public sealed class GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller disconnected.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, error) = ex switch
            {
                PortalException portal => (portal.StatusCode, portal.Message),
                SqlException => (503, "Review storage is temporarily unavailable."),
                OperationCanceledException => (504, "An upstream request timed out."),
                HttpRequestException or System.Text.Json.JsonException => (502, "An upstream service returned an invalid response."),
                _ => (500, "An unexpected error occurred.")
            };
            logger.Log(status >= 500 ? LogLevel.Error : LogLevel.Information,
                "Request {TraceId} failed with status {StatusCode} ({ExceptionType})",
                context.TraceIdentifier, status, ex.GetType().Name);
            context.Response.StatusCode = status;
            if (ex is PortalException { RetryAfter: { } retryAfter })
                context.Response.Headers.RetryAfter = retryAfter;
            await context.Response.WriteAsJsonAsync(new { error, traceId = context.TraceIdentifier }, context.RequestAborted);
        }
    }
}
