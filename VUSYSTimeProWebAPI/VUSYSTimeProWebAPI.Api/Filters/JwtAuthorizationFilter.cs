using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using VUSYSTimeProWebAPI.Application.Abstractions.Authentication;
using VUSYSTimeProWebAPI.Application.Models.Authentication;

namespace VUSYSTimeProWebAPI.Api.Filters
{
    public class JwtAuthorizationFilter : IAsyncActionFilter
    {
        private readonly string _issuer;
        private readonly List<string> _audiences;
        private readonly bool _validateIssuer;
        private readonly bool _validateAudience;
        private readonly bool _validateIssuerSigningKey;
        private readonly ILogger<JwtAuthorizationFilter> _logger;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly ITokenStoreService _tokenStoreService;
        private readonly ITokenService _tokenService;

        /// <summary>
        /// JWT Authorization Filter
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="tokenStoreService"></param>
        /// <param name="tokenService"></param>
        public JwtAuthorizationFilter(IConfiguration configuration, ILogger<JwtAuthorizationFilter> logger, ITokenStoreService tokenStoreService, ITokenService tokenService)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenStoreService = tokenStoreService ?? throw new ArgumentNullException(nameof(tokenStoreService));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            JwtSettings jwtSettings = new();
            configuration.GetSection("JWTKeys").Bind(jwtSettings);
            if (string.IsNullOrWhiteSpace(jwtSettings.IssuerSigningKey))
            {
                throw new InvalidOperationException("JWT IssuerSigningKey is not configured.");
            }
            _issuer = jwtSettings.ValidIssuer;
            _audiences = jwtSettings.ValidAudiences ?? new List<string>();
            _validateIssuer = jwtSettings.ValidateIssuer;
            _validateAudience = jwtSettings.ValidateAudience;
            _validateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.IssuerSigningKey));
        }

        /// <summary>
        /// This Method Is Used To Execute When We Call Any API.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            List<string> columns =
            [
                "Status",
                "ErrorStatus",
                "ErrorDetail"
            ];

            string? authorizationHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
            string? token = ExtractBearerToken(authorizationHeader);

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Authorization header is missing.");
                await WriteUnauthorizedResponseAsync(context.HttpContext, columns, "Authorization header is missing.", cancellationToken);
                return;
            }
            if (_tokenStoreService.IsTokenBlacklisted(token))
            {
                _logger.LogError("Token has been revoked.");
                await WriteUnauthorizedResponseAsync(context.HttpContext, columns, "Token has been revoked.", cancellationToken);
                return;
            }
            if (_tokenService.IsTokenExpired(token))
            {
                _logger.LogError("Token has expired.");
                await WriteUnauthorizedResponseAsync(context.HttpContext, columns, "Token has expired.", cancellationToken);
                return;
            }
            bool isValidToken = await ValidateTokenAsync(token, context.HttpContext, cancellationToken);
            if (!isValidToken)
            {
                const string message = "Token Expired or You Have Logged-Out. Please Try Login Again !!!";
                _logger.LogError(message);
                await WriteUnauthorizedResponseAsync(context.HttpContext, columns, message, cancellationToken);
                return;
            }
            var principal = _tokenService.GetTokenDetails(token);
            if (principal is null)
            {
                _logger.LogError("Invalid token.");
                await WriteUnauthorizedResponseAsync(context.HttpContext, columns, "Invalid token.", cancellationToken);
                return;
            }
            string? tokenType = principal.FindFirst("token_type")?.Value;
            if (string.Equals(tokenType, "refresh", StringComparison.OrdinalIgnoreCase))
            {
                const string message = "Refresh token cannot be used to access protected APIs.";
                _logger.LogError(message);
                await WriteUnauthorizedResponseAsync(context.HttpContext, columns, message, cancellationToken);
                return;
            }
            await next();
        }

        /// <summary>
        /// This Method Is Used To Validate The Token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> ValidateTokenAsync(string token, HttpContext context, CancellationToken cancellationToken)
        {
            JwtSecurityTokenHandler tokenHandler = new();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                TokenValidationParameters validationParameters =
                    new()
                    {
                        ValidateIssuer = _validateIssuer,
                        ValidateAudience = _validateAudience,
                        ValidateIssuerSigningKey = _validateIssuerSigningKey,
                        ValidateLifetime = true,
                        ValidIssuer = _issuer,
                        ValidAudiences = _audiences,
                        IssuerSigningKey = _signingKey,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    _logger.LogError("Validated token is not a JWT token.");
                    return false;
                }
                string? algorithm = jwtToken.Header.Alg;

                if (!string.Equals(algorithm, SecurityAlgorithms.HmacSha512, StringComparison.OrdinalIgnoreCase) && !string.Equals(algorithm, SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Invalid JWT signing algorithm.");
                    return false;
                }
                string? userName = principal.FindFirst("name")?.Value;
                string? emailId = principal.FindFirst("email_id")?.Value;
                string? employeeCode = principal.FindFirst("employee_code")?.Value;
                string? roleId = principal.FindFirst("role_id")?.Value;
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(emailId) || string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrWhiteSpace(roleId))
                {
                    _logger.LogError("Required user claims were not found in token.");
                    return false;
                }
                cancellationToken.ThrowIfCancellationRequested();
                var tokenDetails = await _tokenStoreService.GetTokenInfo(emailId);
                cancellationToken.ThrowIfCancellationRequested();

                if (tokenDetails is null)
                {
                    _logger.LogError("Token Expired or You Have Logged-Out. " + "Please Try Login Again !!!");
                    return false;
                }
                bool tokenMatches = string.Equals(tokenDetails.UserName, userName, StringComparison.Ordinal) && string.Equals(tokenDetails.EmailID, emailId, StringComparison.Ordinal) && string.Equals(tokenDetails.EmployeeCode, employeeCode, StringComparison.Ordinal) && string.Equals(tokenDetails.RoleID, roleId, StringComparison.Ordinal);
                if (!tokenMatches)
                {
                    _logger.LogError("Token is invalid.");
                    return false;
                }
                context.Items["UserName"] = userName;
                context.Items["EmailID"] = emailId;
                context.Items["EmployeeCode"] = employeeCode;
                context.Items["RoleID"] = roleId;
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogError("Token is expired.");
                return false;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex,"Token is invalid.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token.");
                return false;
            }
        }

        /// <summary>
        /// This Method Is Used To Extracts the JWT token from the Authorization header.
        /// </summary>
        /// <param name="authorizationHeader"></param>
        /// <returns></returns>
        private static string? ExtractBearerToken(string? authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                return null;
            }
            const string bearerPrefix = "Bearer ";
            if (authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return authorizationHeader[bearerPrefix.Length..].Trim();
            }
            return authorizationHeader.Trim();
        }

        /// <summary>
        /// This Method Is Used To Writes the existing common unauthorized response format.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="columns"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task WriteUnauthorizedResponseAsync(HttpContext httpContext, List<string> columns, string message, CancellationToken cancellationToken)
        {
            List<List<string>> rows =
            [
                [
                    Helper.ToProperCase(MessageTypeConstants.Error),
                    StatusCodes.Status401Unauthorized.ToString(),
                    message
                ]
            ];

            var dataSet = Helper.GenerateCommonMessage(columns, rows, "GLOBAL_ERROR_RESPONSE");
            string json = Util.JSONOK(ObjectToJson.ConvertDataSetToJson(dataSet));
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            httpContext.Response.ContentType = ContentTypeConstants.JSON_CONTENTTYPE;
            await httpContext.Response.WriteAsync(json, cancellationToken);
        }
    }
}
