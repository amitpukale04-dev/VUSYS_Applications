using System.Globalization;
using CommonLibCore10.Constants;
using log4net.Appender;
using log4net.Core;

namespace VUSYSTimeProWebAPI.Api.Logging;

/// <summary>
/// Creates separate request log files for each user and date.
/// </summary>
public sealed class Log4NetFileAppender : RollingFileAppender
{
    public static string LogDirectory = string.Empty;
    protected override void Append(LoggingEvent loggingEvent)
    {
        string emailId = loggingEvent.LookupProperty("EmailID")?.ToString() ?? "UnknownUser";
        string date = DateTime.Now.ToString(DateTimeFormatConstants.DDMMMYYYY_COMPACT_DATEFORMAT, CultureInfo.InvariantCulture);

        string logFileName = $"{emailId}_{date}_log.txt";
        string fullPath = Path.Combine(LogDirectory, logFileName);

        if (File != fullPath)
        {
            File = fullPath;
            ActivateOptions(); // re-apply file settings
        }

        base.Append(loggingEvent);
    }
}