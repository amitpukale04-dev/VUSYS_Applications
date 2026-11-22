using System.Reflection;
using log4net;
using log4net.Config;
using VUSYSTimeProWebAPI.Api.Logging;
using VUSYSTimeProWebAPI.Application.Common.Options;

namespace VUSYSTimeProWebAPI.Api.Extensions;

public static class Log4NetExtensions
{
    /// <summary>
    /// Configures log4net and initialises the custom
    /// per-user request log directory.
    /// </summary>
    public static WebApplication ConfigureLog4Net(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        MyApplicationKeysOptions applicationKeys = app.Services.GetRequiredService<MyApplicationKeysOptions>();
        if (string.IsNullOrWhiteSpace(applicationKeys.APIRequestBodyLog))
        {
            throw new InvalidOperationException("MyApplicationKeys:APIRequestBodyLog " + "configuration value is missing.");
        }
        Log4NetFileAppender.LogDirectory = applicationKeys.APIRequestBodyLog;
        Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Unable to determine the API entry assembly.");
        var logRepository = LogManager.GetRepository(entryAssembly);

        string configurationPath = Path.Combine(app.Environment.ContentRootPath, "log4net.config");

        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException("The log4net configuration file was not found.", configurationPath);
        }
        XmlConfigurator.Configure(logRepository, new FileInfo(configurationPath));
        return app;
    }
}