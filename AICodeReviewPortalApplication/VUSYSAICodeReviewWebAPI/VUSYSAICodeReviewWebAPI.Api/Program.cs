using VUSYSAICodeReviewWebAPI.Api;
using VUSYSAICodeReviewWebAPI.Api.Middlewares;
using VUSYSAICodeReviewWebAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPortalApi(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<GlobalErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("frontend");
app.UseAuthentication();
app.UseMiddleware<UserContextMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();

public partial class Program { }
