using CommonLib;
using VUSYSAppHub.Components;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Configuration.GetSection("MyApplicationKeys").Bind(CommonHelper.KeyOption);
builder.Services.AddScoped<ODBCHelper>();
builder.Services.AddScoped<IPortalAppService, PortalAppService>();
builder.Services.AddScoped<IUpdateDeploymentVersionService, UpdateDeploymentVersionService>();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("Configurations/DBConnection.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
