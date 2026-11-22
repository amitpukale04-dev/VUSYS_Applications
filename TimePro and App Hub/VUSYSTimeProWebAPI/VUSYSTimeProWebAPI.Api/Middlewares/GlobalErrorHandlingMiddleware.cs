using System.Data.Common;
using System.Net;
using System.Security;
using System.Text;
using System.Text.Json;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Middlewares
{
    public class GlobalErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _isGlobalErrorLoggingEnabled;
        private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;
        private const string GlobalErrorHandlingMiddlewareLogClassName = "GlobalErrorHandlingMiddleware";
        private const string TableName = "GLOBAL_ERROR_RESPONSE";

        /// <summary>
        /// Global Error Handling Middleware Class Constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            try
            {
                _isGlobalErrorLoggingEnabled = Convert.ToBoolean(CommonHelper.KeyOption.GlobalErrorLoggingEnabled.ToString().ToLower());
            }
            catch
            {
                _isGlobalErrorLoggingEnabled = true;
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
                var origin = context.Request.Headers["Origin"].FirstOrDefault();
                if (!string.IsNullOrEmpty(origin))
                {
                    if (!string.IsNullOrEmpty(origin) && (!Helper.AllowAPIURL.Contains(origin)))
                    {
                        if (!string.IsNullOrEmpty(origin) && (!Helper.AllowCorsURL.Contains(origin)))
                        {
                            await WriteCorsErrorResponseAsync(context);
                            return;
                        }
                    }
                }
                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    await WriteUnauthorizedResponseAsync(context);
                    return;
                }
                if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    await WriteForbiddenResponseAsync(context);
                    return;
                }
                if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    await WriteNotFoundResponseAsync(context);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (_isGlobalErrorLoggingEnabled)
                {
                    await LogExceptionAsync(context, ex);
                }
                else
                {
                    _logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// This Method Is Used To Handle The Exception and Creates The Clean Problem Details JSON Response For The Client.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // If response already started, we can't change it – just log and rethrow.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The Response Has Already Started, Unable To Write The Global Error Logs.");
                throw exception;
            }

            var statusCode = (int)HttpStatusCode.InternalServerError;

            string CustomErrorDetail = "Please Check The Global Error Logs For More Details.";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Instance = context.Request.Path,
                Title = "An Unexpected Error Occurred.",
                Detail = "Please Contact Support If The Problem Persists."
            };

            // Specific handling for DB / ODBC errors
            switch (exception)
            {
                // SQLException / DATABASE ERRORS
                case SqlException sqlEx:
                    problem.Title = "Database Error";
                    problem.Status = StatusCodes.Status500InternalServerError;

                    // Try extracting DB object name
                    var dbObjectName = ODBCDBExceptionHelper.ExtractDbObjectName(sqlEx.Message);
                    if (!string.IsNullOrEmpty(dbObjectName))
                    {
                        problem.Detail = $"Database Object '{dbObjectName}' Is Missing or Invalid. {CustomErrorDetail}";
                    }
                    else if (sqlEx.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        problem.Detail = $"A Required Stored Procedure or Database Object Is Missing. {CustomErrorDetail}";
                    }
                    else
                    {
                        problem.Detail = $"A Database Error Occurred While Processing Your Request. {CustomErrorDetail}";
                    }
                    break;

                case DbException:
                    problem.Title = "Database Error";
                    problem.Detail = $"A Database Error Occurred While Processing Your Request. {CustomErrorDetail}";
                    problem.Status = StatusCodes.Status500InternalServerError;
                    break;



                // AUTHENTICATION / AUTHORIZATION
                case UnauthorizedAccessException:
                    problem.Title = "Unauthorized Access";
                    problem.Detail = "You Are Not Authorized To Access This Resource.";
                    problem.Status = StatusCodes.Status401Unauthorized;
                    break;

                case SecurityException:
                    problem.Title = "Security Biolation";
                    problem.Detail = "Access Denied Due To Security Restrictions.";
                    problem.Status = StatusCodes.Status403Forbidden;
                    break;



                // VALIDATION ERRORS (ModelState etc.)
                case ArgumentException argEx:
                    problem.Title = "Invalid Argument";
                    problem.Detail = argEx.Message;
                    problem.Status = StatusCodes.Status400BadRequest;
                    break;

                case FormatException:
                    problem.Title = "Invalid Data Format";
                    problem.Detail = "One or More Inputs Are Not In The Expected Format.";
                    problem.Status = StatusCodes.Status400BadRequest;
                    break;

                case JsonException:
                    problem.Title = "Invalid JSON";
                    problem.Detail = "The JSON Payload Is Malformed or Unreadable.";
                    problem.Status = StatusCodes.Status400BadRequest;
                    break;



                // COMMON RUNTIME ERRORS
                case NullReferenceException:
                    problem.Title = "Unexpected Error";
                    problem.Detail = "A Required Value Was null. Please Contact Support.";
                    problem.Status = StatusCodes.Status500InternalServerError;
                    break;

                case InvalidOperationException:
                    problem.Title = "Invalid Operation";
                    problem.Detail = exception.Message;
                    problem.Status = StatusCodes.Status400BadRequest;
                    break;



                // TIMEOUTS & TASK CANCELS
                case TaskCanceledException:
                    problem.Title = "Request Cancelled";
                    problem.Detail = "The Request Timed Out or was Cancelled.";
                    problem.Status = StatusCodes.Status408RequestTimeout;
                    break;

                case TimeoutException:
                    problem.Title = "Operation Timed Out";
                    problem.Detail = "The Operation Took Too Long To Complete.";
                    problem.Status = StatusCodes.Status408RequestTimeout;
                    break;



                // NOT FOUND
                case FileNotFoundException:
                    problem.Title = "File Not Found";
                    problem.Detail = exception.Message;
                    problem.Status = StatusCodes.Status404NotFound;
                    break;

                case DirectoryNotFoundException:
                    problem.Title = "Directory Not Found";
                    problem.Detail = exception.Message;
                    problem.Status = StatusCodes.Status404NotFound;
                    break;



                // NETWORK / IO ERRORS
                case IOException:
                    problem.Title = "I/O Error";
                    problem.Detail = exception.Message;
                    problem.Status = StatusCodes.Status500InternalServerError;
                    break;

                case HttpRequestException:
                    problem.Title = "Network Error";
                    problem.Detail = "Unable To Reach The Remote Server.";
                    problem.Status = StatusCodes.Status503ServiceUnavailable;
                    break;

                case Exception ex
                when context.Response.StatusCode == StatusCodes.Status403Forbidden &&
                !context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"):
                    problem.Title = "CORS Error";
                    problem.Detail = "The Request Was Blocked By CORS Policy Restrictions.";
                    problem.Status = StatusCodes.Status403Forbidden;
                    break;


                // DEFAULT ERROR HANDLER - catch all
                default:
                    problem.Title = "An Unexpected Error Occurred";
                    problem.Detail = "Please Contact Support If The Problem Persists.";
                    problem.Status = StatusCodes.Status500InternalServerError;
                    break;
            }

            problem.Extensions["exception"] = exception.ToString();

            context.Response.Clear();
            context.Response.StatusCode = problem.Status ?? statusCode;
            context.Response.ContentType = ContentTypeConstants.PROBLEM_JSON_CONTENTTYPE;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            List<string> columns = new List<string> { "Status", "APIName", "Title", "ErrorStatus", "ErrorDetail" };
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), problem.Instance, problem.Title, problem.Status.ToString(), problem.Detail }
            };

            string ReturnResult = Util.JSONOK(ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(columns, rows, TableName))); ;
            await context.Response.WriteAsync(ReturnResult);
        }

        /// <summary>
        /// This Method Is Used To Log The Exceptions
        /// Logs a rich error entry similar to RequestLoggingMiddleware.LogRequestBodyAsync,
        /// including user info, endpoint, exception, and request payload (if available).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private async Task LogExceptionAsync(HttpContext context, Exception exception)
        {
            try
            {
                var request = context.Request;

                if (request.ContentLength == null || request.ContentLength == 0 || !request.Body.CanRead)
                    return;

                if (context.RequestAborted.IsCancellationRequested)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.MiddlewareLogNamespace, $"{GlobalErrorHandlingMiddlewareLogClassName}.LogRequestBodyAsync()", "Client disconnected before reading body.", false);
                    return;
                }

                request.EnableBuffering();

                string body;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }
                // Format for pretty JSON if valid
                string formattedBody;
                if (string.IsNullOrWhiteSpace(body))
                {
                    formattedBody = "<no body>";
                }
                else
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(body);
                        formattedBody = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                    }
                    catch
                    {
                        formattedBody = body; // fallback if not valid JSON
                    }
                }

                var emailId = context.Items["EmailID"]?.ToString() ?? "UnknownUserID";
                var userName = context.Items["UserName"]?.ToString() ?? "UnknownUserName";

                var logEntry = new StringBuilder();
                logEntry.AppendLine("=====================================================================================================================");
                logEntry.AppendLine($"🕒 Time Stamp : {DateTime.Now.ToString(DateTimeFormatConstants.DDMMYYYY_HHMMSS_DATETIMEFORMAT)}");
                logEntry.AppendLine($"👤 User       : {userName} ({emailId})");
                logEntry.AppendLine($"🔗 End Point  : {request.Method} {request.Path}{request.QueryString}");
                logEntry.AppendLine($"❗ Exception  : {exception.GetType().FullName}");
                logEntry.AppendLine($"📌 Message    : {exception.Message}");

                if (exception.InnerException != null)
                {
                    logEntry.AppendLine($"🔍 Inner      : {exception.InnerException.GetType().FullName} - {exception.InnerException.Message}");
                }

                logEntry.AppendLine("📄 StackTrace :");
                logEntry.AppendLine(exception.StackTrace ?? "<no stack trace>");
                logEntry.AppendLine();
                logEntry.AppendLine("📦 Request Payload :");
                logEntry.AppendLine(formattedBody);   // <-- NEVER null now
                logEntry.AppendLine("=====================================================================================================================");
                logEntry.AppendLine();

                Helper.LogMessage(CommonHelper.KeyOption.APIGlobalErrorLog, LogNameSpaceConstants.MiddlewareLogNamespace, $"{GlobalErrorHandlingMiddlewareLogClassName}.LogExceptionAsync()", logEntry.ToString(), false);
                _logger.LogError(exception, "Global exception captured while processing {Path}", request.Path);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Error while logging global exception");
            }
        }

        /// <summary>
        /// This Method Is Used To Write UnAuthorized Response.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task WriteUnauthorizedResponseAsync(HttpContext context)
        {
            if (context.Response.HasStarted) return;

            context.Response.ContentType = ContentTypeConstants.JSON_CONTENTTYPE;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            var problem = new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = "Authentication Failed. Your Token Is Missing/Expired/Invalid.",
                Instance = context.Request.Path
            };

            List<string> columns = new List<string> { "Status", "APIName", "Title", "ErrorStatus", "ErrorDetail" };
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), problem.Instance, problem.Title, problem.Status.ToString(), problem.Detail }
            };

            string ReturnResult = Util.JSONOK(ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(columns, rows, TableName)));
            await context.Response.WriteAsync(ReturnResult);
        }

        /// <summary>
        /// This Method Is Used To Write Forbidden Response.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task WriteForbiddenResponseAsync(HttpContext context)
        {
            if (context.Response.HasStarted) return;

            context.Response.ContentType = ContentTypeConstants.JSON_CONTENTTYPE;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            var problem = new ProblemDetails
            {
                Status = 403,
                Title = "Forbidden",
                Detail = "You Do Not Have Permission To Access This Resource.",
                Instance = context.Request.Path
            };

            List<string> columns = new List<string> { "Status", "APIName", "Title", "ErrorStatus", "ErrorDetail" };
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), problem.Instance, problem.Title, problem.Status.ToString(), problem.Detail }
            };

            string ReturnResult = Util.JSONOK(ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(columns, rows, TableName)));
            await context.Response.WriteAsync(ReturnResult);
        }

        /// <summary>
        /// This Method Is Used To Write Cors Error Response.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task WriteCorsErrorResponseAsync(HttpContext context)
        {
            var clientOrigin = context.Request.Headers["Origin"].FirstOrDefault() ?? "Unknown Origin";
            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = ContentTypeConstants.JSON_CONTENTTYPE;
                context.Response.StatusCode = StatusCodes.Status403Forbidden;   
            }
            var problem = new ProblemDetails
            {
                Status = 403,
                Title = "CORS Error",
                Detail = $"The Request Was Blocked Due To CORS policy. Ensure The API Allows This '{clientOrigin}' Origin.",
                Instance = context.Request.Path
            };
            List<string> columns = new List<string> { "Status", "APIName", "Title", "ErrorStatus", "ErrorDetail" };
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), problem.Instance, problem.Title, problem.Status.ToString(), problem.Detail }
            };
            string ReturnResult = Util.JSONOK(ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(columns, rows, TableName))); ;
            await context.Response.WriteAsync(ReturnResult);
        }

        /// <summary>
        /// This Method Is Used To Write Not Found Response.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task WriteNotFoundResponseAsync(HttpContext context)
        {
            if (context.Response.HasStarted) return;
            context.Response.ContentType = ContentTypeConstants.JSON_CONTENTTYPE;
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            var problem = new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = "The Requested Resource Was Not Found.",
                Instance = context.Request.Path
            };

            List<string> columns = new List<string> { "Status", "APIName", "Title", "ErrorStatus", "ErrorDetail" };
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), problem.Instance, problem.Title, problem.Status.ToString(), problem.Detail }
            };
            string ReturnResult = Util.JSONOK(ObjectToJson.ConvertDataSetToJson(Helper.GenerateCommonMessage(columns, rows, TableName)));
            await context.Response.WriteAsync(ReturnResult);
        }
    }
}
