namespace VUSYSAICodeReviewWebAPI.Application.Common.Options;

public sealed class GitHubOptions
{
    public string Token { get; init; } = "";
    public string[] AllowedRepositories { get; init; } = [];
}

public sealed class GeminiOptions
{
    public string ApiKey { get; init; } = "";
    public string Model { get; init; } = "";
}

public sealed class AuthOptions
{
    public const string Issuer = "VUSYS-CodeReview";
    public const string Audience = "VUSYS-CodeReview-Portal";
    public string JwtKey { get; init; } = "";
    public string AdminUser { get; init; } = "admin";
    public string AdminPassword { get; init; } = "";
}
