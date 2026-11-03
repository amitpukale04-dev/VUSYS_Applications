using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using CommonLibCore10;
using CommonLibCore10.Constants;
using log4net;
using Microsoft.AspNetCore.Connections;
using Microsoft.IdentityModel.Tokens;
using VUSYSTimeProWebAPI.Application.Abstractions.Authentication;
using VUSYSTimeProWebAPI.Application.Models.Authentication;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _isLoggingEnabled;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly JwtSettings _jwtSettings;
        private readonly string _issuer;
        public List<string> _Audiences { get; set; } = new();
        private readonly bool _validateissuer;
        private readonly bool _validateaudience;
        private readonly bool _validateissuersigningkey;
        private readonly SymmetricSecurityKey _signingKey;
        private static readonly ILog _log = LogManager.GetLogger(typeof(RequestLoggingMiddleware));
        private const string RequestLoggingMiddlewareLogClassName = "RequestLoggingMiddleware";

        /// <summary>
        /// Request Logging Middleware Class Constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public RequestLoggingMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _jwtSettings = new JwtSettings();
            configuration.GetSection("JWTKeys").Bind(_jwtSettings);
            _issuer = _jwtSettings.ValidIssuer;
            //_audience = _jwtSettings.ValidAudience;
            _Audiences = _jwtSettings.ValidAudiences;
            _validateissuer = _jwtSettings.ValidateIssuer;
            _validateaudience = _jwtSettings.ValidateAudience;
            _validateissuersigningkey = _jwtSettings.ValidateIssuerSigningKey;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.IssuerSigningKey));
            _isLoggingEnabled = Convert.ToBoolean(CommonHelper.KeyOption.RequestLoggingEnabled.ToString().ToLower());
        }



        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            if (path != null &&
                (path.Contains("/api/auth/login") || path.Contains("/api/auth/schedulerlogin") || path.Contains("/api/auth/refreshtoken") || path.Contains("/api/auth/logout") || path.Contains("/api/auth/encrypttext") || path.Contains("/api/auth/decrypttext") || path.StartsWith("/api/auth/checkforpasswordlinkavailable") || path.StartsWith("/api/auth/forgotpasswordrequest") || path.StartsWith("/api/auth/resetpassword") || path.StartsWith("/api/otp/generateorresendotp")))
            {
                await _next(context);
                return;
            }
            if (_isLoggingEnabled)
            {
                var isOk = await LogRequestBodyAsync(context);
                if (!isOk)
                {
                    List<string> columns = new List<string> { "Status", "ErrorStatus", "ErrorDetail" };
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), StatusCodes.Status401Unauthorized.ToString(), "Token Expired or Invalid Token or You Have Logged-Out. Please Try Login Again !!!" }
                    };
                    var dataSet = Helper.GenerateCommonMessage(columns, rows, "GLOBAL_ERROR_RESPONSE");
                    string json = Util.JSONOK(ObjectToJson.ConvertDataSetToJson(dataSet));
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = ContentTypeConstants.JSON_CONTENTTYPE;
                    await context.Response.WriteAsync(json);
                    return;
                }
            }
            await _next(context);
        }

        /// <summary>
        /// This Method Is Used To Log API End Point Request Body.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<bool> LogRequestBodyAsync(HttpContext context)
        {
            try
            {
                var request = context.Request;
                var token = request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var isTokenValid = await ValidateToken(token, context);
                if (!isTokenValid)
                {
                    _log.Error("Token Expired or You Have Logged-Out. Please Try Login Again !!!");
                    return false;
                }

                if (context.RequestAborted.IsCancellationRequested)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.MiddlewareLogNamespace, $"{RequestLoggingMiddlewareLogClassName}.LogRequestBodyAsync()", "Client disconnected before reading body.", false);
                    return false;
                }

                // Only try reading body for methods that typically contain payload
                var canHaveBody = HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsPatch(request.Method);

                string body = string.Empty;

                if (canHaveBody && request.Body != null && request.Body.CanRead)
                {
                    request.EnableBuffering();

                    using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                    {
                        body = await reader.ReadToEndAsync();
                        request.Body.Position = 0;
                    }
                }

                // Format JSON (if any)
                string formattedBody = body;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(body);
                        formattedBody = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                    }
                    catch
                    {
                        formattedBody = body;
                    }
                }

                if (!Directory.Exists(CommonHelper.KeyOption.APIRequestBodyLog))
                {
                    Directory.CreateDirectory(CommonHelper.KeyOption.APIRequestBodyLog);
                }
                var emailID = context.Items["EmailID"]?.ToString() ?? "UnknownUserID";
                var userName = context.Items["UserName"]?.ToString() ?? "UnknownUserName";

                // Set context for PerUserFileAppender to use
                ThreadContext.Properties["EmailID"] = emailID;

                var logEntry = new StringBuilder();
                logEntry.AppendLine("=====================================================================================================================");
                logEntry.AppendLine($"🕒 Time Stamp : {DateTime.Now.ToString(DateTimeFormatConstants.DDMMYYYY_HHMMSS_DATETIMEFORMAT)}");
                logEntry.AppendLine($"👤 User      : {userName} ({emailID})");
                logEntry.AppendLine($"🔗 End Point  : {request.Method} {request.Path}{request.QueryString}");
                logEntry.AppendLine("📦 Payload   :");
                logEntry.AppendLine();
                logEntry.AppendLine(formattedBody);
                logEntry.AppendLine("=====================================================================================================================");
                logEntry.AppendLine();
                _log.Info(logEntry.ToString());
                return true;
            }
            catch (ConnectionResetException cre)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.MiddlewareLogNamespace, $"{RequestLoggingMiddlewareLogClassName}.LogRequestBodyAsync()", "Client disconnected during request body read: " + cre.Message, false);
                _log.Error("Client disconnected before request body could be read.", cre);
                return false;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.MiddlewareLogNamespace, $"{RequestLoggingMiddlewareLogClassName}.LogRequestBodyAsync()", ex.Message, false);
                _log.Error("Error logging request body", ex);
                return false;
            }
        }

        /// <summary>
        /// This Method Is Used To Validate The Token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<bool> ValidateToken(string token, HttpContext context)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var TokenStoreService = context.RequestServices.GetRequiredService<ITokenStoreService>();
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = _validateissuer,
                    ValidateAudience = _validateaudience,
                    ValidateIssuerSigningKey = _validateissuersigningkey,
                    ValidIssuer = _issuer,
                    //ValidAudience = _audience,
                    ValidAudiences = _Audiences,
                    IssuerSigningKey = _signingKey,
                    ClockSkew = TimeSpan.Zero // Allow no time skew
                };

                // Validate token and get claims
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

                // Optionally, you can access the validated token and extract claims here:
                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken != null)
                {
                    // Access claims (e.g., username)
                    var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                    var email_id = jwtToken.Claims.FirstOrDefault(c => c.Type == "email_id")?.Value;
                    var employee_code = jwtToken.Claims.FirstOrDefault(c => c.Type == "employee_code")?.Value;
                    var role_id = jwtToken.Claims.FirstOrDefault(c => c.Type == "role_id")?.Value;

                    if (username == null || email_id == null || employee_code == null || role_id == null)
                    {
                        _logger.LogError("Username claim not found in token.");
                        return false;
                    }
                    else
                    {
                        var TokenDetails = await TokenStoreService.GetTokenInfo(email_id);
                        if (TokenDetails != null)
                        {
                            if (TokenDetails.UserName == username && TokenDetails.EmailID == email_id && TokenDetails.EmployeeCode == employee_code && TokenDetails.RoleID == role_id)
                            {
                                context.Items["UserName"] = username;
                                context.Items["EmailID"] = email_id;
                                context.Items["EmployeeCode"] = employee_code;
                                context.Items["RoleID"] = role_id;
                            }
                            else
                            {
                                _logger.LogError("Token is invalid.");
                                return false;
                            }
                        }
                        else
                        {
                            _logger.LogError("Token Expired or You Have Logged-Out. Please Try Login Again !!!");
                            return false;
                        }
                    }
                }

                return true; // Token is valid
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogError("Token is expired.");
                return false; // Token expired
            }
            catch (SecurityTokenException)
            {
                _logger.LogError("Token is invalid.");
                return false; // Invalid token
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating token: {ex.Message}");
                return false;
            }
        }
    }
}
