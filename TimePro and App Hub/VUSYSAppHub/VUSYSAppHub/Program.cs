using System.Net.Http.Headers;
using CommonLib;
using VUSYSAppHub.Components;
using VUSYSAppHub.Contracts;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("Configurations/DBConnection.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Configuration.GetSection("GitHubCredentials").Bind(CommonHelper.GitHubKeyOption);
builder.Configuration.GetSection("MyApplicationKeys").Bind(CommonHelper.KeyOption);

builder.Services.AddHttpClient("GitHubBillingClient", client =>
{
    client.BaseAddress = new Uri(CommonHelper.GitHubKeyOption.BaseUrl);

    client.DefaultRequestHeaders.UserAgent.ParseAdd("VUSYS-AppHub-GitHub-Billing");

    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            CommonHelper.GitHubKeyOption.Token);

    client.DefaultRequestHeaders.Remove("X-GitHub-Api-Version");

    client.DefaultRequestHeaders.Add(
        "X-GitHub-Api-Version",
        "2026-03-10");
});

builder.Services.AddScoped<ODBCHelper>();
builder.Services.AddScoped<IPortalAppService, PortalAppService>();
builder.Services.AddScoped<IUpdateDeploymentVersionService, UpdateDeploymentVersionService>();

builder.Services.AddScoped<IDatabaseObjectGeneratorService, DatabaseObjectGeneratorService>();
builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddSingleton<IScriptHistoryStore, ScriptHistoryStore>();

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/404");

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        string path = ctx.Context.Request.Path.Value ?? string.Empty;

        if (
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/service-worker.js", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/js/service-worker-register.js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("blazor.web.js", StringComparison.OrdinalIgnoreCase)
        )
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();