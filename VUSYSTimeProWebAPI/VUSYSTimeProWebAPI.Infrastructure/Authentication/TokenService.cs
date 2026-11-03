using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommonLibCore10;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VUSYSTimeProWebAPI.Application.Abstractions.Authentication;
using VUSYSTimeProWebAPI.Application.Models.Authentication;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Authentication
{
    public sealed class TokenService : ITokenService
    {
        private const string TokenServiceLogClassName = "TokenService";
        private readonly string _SecretKey = string.Empty; // This should be stored in a secure place
        private readonly string _Issuer = string.Empty;
        public List<string> _Audiences { get; set; } = new();
        private readonly int _TokenExpirationInMinutes = 0;
        private readonly int _RefreshTokenExpirationInDays = 0;
        private readonly SymmetricSecurityKey _signingKey;
        public static JwtSettings objJWTSettings { get; set; } = new JwtSettings();

        /// <summary>
        /// Token Service Class Constructor.
        /// </summary>
        /// <param name="configuration"></param>
        public TokenService(IConfiguration configuration)
        {
            configuration.GetSection("JWTKeys").Bind(objJWTSettings);
            _SecretKey = objJWTSettings.IssuerSigningKey;
            _Issuer = objJWTSettings.ValidIssuer;
            _Audiences = objJWTSettings.ValidAudiences;
            _TokenExpirationInMinutes = objJWTSettings.TokenExpirationInMinutes;
            _RefreshTokenExpirationInDays = objJWTSettings.RefreshTokenExpirationInDays;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_SecretKey));
        }

        /// <summary>
        /// This Method Is Used To Generate Token.
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="RoleID"></param>
        /// <returns></returns>
        public string GenerateToken(string UserName, string EmailID, string EmployeeCode, string RoleID)
        {
            string accessToken = string.Empty;
            try
            {
                var claims = new List<Claim>
                {
                    new Claim("name", UserName),
                    new Claim("email_id", EmailID),
                    new Claim("employee_code", EmployeeCode),
                    new Claim("role_id", RoleID),
                    new Claim("token_type", "access")
                };

                var token = new JwtSecurityToken(
                    issuer: _Issuer,
                    audience: null,
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(_TokenExpirationInMinutes),
                    signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha512)
                );

                token.Payload.Remove(JwtRegisteredClaimNames.Aud);
                token.Payload[JwtRegisteredClaimNames.Aud] = _Audiences.Distinct().ToList();

                var tokenHandler = new JwtSecurityTokenHandler();
                accessToken = tokenHandler.WriteToken(token);
                return accessToken;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureAuthenticationLogNamespace, $"{TokenServiceLogClassName}.GenerateToken()", ex.Message, false);
                accessToken = string.Empty;
            }
            return accessToken;
        }

        /// <summary>
        /// This Method Is Used To Get Token Expiration In Minutes.
        /// </summary>
        /// <returns></returns>
        public int GetTokenExpirationInMinutes()
        {
            int TokenExpirationInMinutes = 0;
            try
            {
                TokenExpirationInMinutes = _TokenExpirationInMinutes;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureAuthenticationLogNamespace, $"{TokenServiceLogClassName}.GetTokenExpirationInMinutes()", ex.Message, false);
                TokenExpirationInMinutes = 0;
            }
            return TokenExpirationInMinutes;
        }

        /// <summary>
        /// This Method Is Used To Checked Whether Token Is Expired Or Not.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool IsTokenExpired(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// This Method Is Used To Get Token Details.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public ClaimsPrincipal? GetTokenDetails(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_SecretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true, // Check Expiry
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// This Method Is Used To Generate Refresh Token.
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="RoleID"></param>
        /// <returns></returns>
        public string GenerateRefreshToken(string UserName, string EmailID, string EmployeeCode, string RoleID)
        {
            string accessToken = string.Empty;
            try
            {
                var claims = new List<Claim>
                {
                    new Claim("name", UserName),
                    new Claim("email_id", EmailID),
                    new Claim("employee_code", EmployeeCode),
                    new Claim("role_id", RoleID),
                    new Claim("token_type", "refresh")
                };

                var token = new JwtSecurityToken(
                    issuer: _Issuer,
                    audience: null,
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(_RefreshTokenExpirationInDays),
                    signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha512)
                );

                token.Payload.Remove(JwtRegisteredClaimNames.Aud);
                token.Payload[JwtRegisteredClaimNames.Aud] = _Audiences.Distinct().ToList();

                var tokenHandler = new JwtSecurityTokenHandler();
                accessToken = tokenHandler.WriteToken(token);
                return accessToken;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureAuthenticationLogNamespace, $"{TokenServiceLogClassName}.GenerateRefreshToken()", ex.Message, false);
                accessToken = string.Empty;
            }
            return accessToken;
        }

        /// <summary>
        /// This Method Is Used To Get Refresh Token Expiration In Days.
        /// </summary>
        /// <returns></returns>
        public int GetRefreshTokenExpirationInDays() => _RefreshTokenExpirationInDays;

        /// <summary>
        /// This Method Is Used To Get Refresh To Store.
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="RoleID"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public RefreshToken GetRefreshTokenStore(string UserName, string EmailID, string EmployeeCode, string RoleID, string Token)
        {
            var refreshToken = new RefreshToken
            {
                Token = Token,
                UserName = UserName,
                EmailID = EmailID,
                EmployeeCode = EmployeeCode,
                RoleID = RoleID,
                ExpiresAt = DateTime.UtcNow.AddDays(_RefreshTokenExpirationInDays),
                IsRevoked = false
            };
            return refreshToken;
        }

        /// <summary>
        /// This Method Is Used To Validate Refresh Token.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
        {
            return GetTokenDetails(refreshToken); // Reuse the same validation
        }
    }
}