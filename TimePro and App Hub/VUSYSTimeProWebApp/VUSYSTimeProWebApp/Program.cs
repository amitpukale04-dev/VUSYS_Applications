using System.Net.Http.Headers;
using CommonLib;
using CommonLib.Constants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.Repositories;
using VUSYSTimeProWebApp.Security;
using WebEssentials.AspNetCore.Pwa;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddControllersWithViews();

builder.Services.AddControllersWithViews()
        .AddViewOptions(options =>
        {
            options.HtmlHelperOptions.ClientValidationEnabled = true;
        });

builder.Services.AddMvcCore().AddDataAnnotations();

// Add PWA support
builder.Services.AddProgressiveWebApp(new PwaOptions
{
    RegisterServiceWorker = false,
    RegisterWebmanifest = true,
    OfflineRoute = "/Offline"
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.LoginPath = "/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});

HttpClientHandler clientHandler = new HttpClientHandler();
clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

HttpClient client = new HttpClient(clientHandler);
//string BaseURL = builder.Configuration.GetValue<string>("MyApplicationKeys:BaseURL");
//client.BaseAddress = new Uri(BaseURL);
var contentType = new MediaTypeWithQualityHeaderValue(ContentTypeConstants.JSON_CONTENTTYPE);
client.DefaultRequestHeaders.Accept.Add(contentType);

CookieOptions cookieOptions = new CookieOptions();

#region [Services]

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<HttpClient>(client);
builder.Services.AddSingleton<CookieOptions>(cookieOptions);
builder.Services.AddSingleton<DataProtectionPurposeString>();
builder.Services.AddScoped<DAC>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;

    string UserEmailID = "UnknownUser";

    if (httpContext != null)
    {
        var userSession = httpContext.Session.GetObjectFromJson<UsersM>("UserSession");

        if (userSession != null)
        {
            UserEmailID = userSession.UserEmailID;
        }
    }
    var connectionString = builder.Configuration.GetConnectionString(CommonHelper.KeyOption.ConnectionStringKey);
    return new DAC(connectionString, CommonHelper.KeyOption.DBType, UserEmailID, Convert.ToBoolean(CommonHelper.KeyOption.RequestDBScriptLoggingEnabled), CommonHelper.KeyOption.RequestDBScriptLog);
});

builder.Services.AddScoped<ILogin, ILoginResp>();
builder.Services.AddScoped<IUser, IUserResp>();
builder.Services.AddScoped<IRole, IRoleMasterResp>();
builder.Services.AddScoped<IRoleMenuAccess, IRoleMenuAccessResp>();
builder.Services.AddScoped<IEmailConfiguration, IEmailConfigurationResp>();
builder.Services.AddScoped<IEmployee, IEmployeeResp>();
builder.Services.AddScoped<ITimeSheet, ITimeSheetResp>();
builder.Services.AddScoped<IProject, IProjectResp>();
builder.Services.AddScoped<IGender, IGenderResp>();
builder.Services.AddScoped<ICountry, ICountryResp>();
builder.Services.AddScoped<IState, IStateResp>();

#endregion [Services]

#region [Connection String and Configuration Keys]

builder.Configuration.GetSection("MyApplicationKeys").Bind(CommonHelper.KeyOption);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString(CommonHelper.KeyOption.ConnectionStringKey));
});

#endregion [Connection String and Configuration Keys]

#region [Session]

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true; 
    options.Cookie.IsEssential = true;
});

#endregion [Session]

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 404 || context.Response.StatusCode == 500)
    {
        var newPath = new PathString("/Error/Error" + context.Response.StatusCode);
        var originalPath = context.Request.Path;
        var strOriginalPath = originalPath.ToString();
        context.Request.Path = newPath;
        await next();
    }

});

app.UseHttpsRedirection();                       // 1: Redirect HTTP to HTTPS
app.UseStaticFiles();                            // 2: Serve static files
app.UseRouting();                                // 3: Enables routing
app.UseSession();                                // 4: Session management
//app.UseStatusCodePagesWithReExecute("/Error/{0}"); // 5: Error handling (e.g., 404, 500)
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
