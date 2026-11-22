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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Configuration.GetSection("MyApplicationKeys").Bind(CommonHelper.KeyOption);

builder.Services.AddScoped<ODBCHelper>();
builder.Services.AddScoped<IPortalAppService, PortalAppService>();
builder.Services.AddScoped<IUpdateDeploymentVersionService, UpdateDeploymentVersionService>();

builder.Services.AddScoped<IDatabaseObjectGeneratorService, DatabaseObjectGeneratorService>();
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
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();