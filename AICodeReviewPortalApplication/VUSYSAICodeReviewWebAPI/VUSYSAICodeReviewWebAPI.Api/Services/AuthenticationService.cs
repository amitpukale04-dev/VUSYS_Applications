using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;

namespace VUSYSAICodeReviewWebAPI.Api.Services;

public sealed class AuthenticationService(AuthOptions options)
{
    public LoginResponse Login(LoginRequest request)
    {
        bool userMatches = Matches(request.Username, options.AdminUser);
        bool passwordMatches = Matches(request.Password, options.AdminPassword);
        if (!userMatches || !passwordMatches)
            throw new PortalException(401, "Invalid username or password.");
        var expires = DateTime.UtcNow.AddHours(4);
        var token = new JwtSecurityToken(AuthOptions.Issuer, AuthOptions.Audience,
            [new Claim(ClaimTypes.Name, options.AdminUser)], expires: expires,
            signingCredentials: new(SigningKey(options), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), options.AdminUser, expires);
    }

    internal static SymmetricSecurityKey SigningKey(AuthOptions options) => new(Encoding.UTF8.GetBytes(options.JwtKey));
    private static bool Matches(string actual, string expected)
        => CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(actual)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
