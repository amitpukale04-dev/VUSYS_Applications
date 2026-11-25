namespace VUSYSAICodeReviewWebAPI.Application.Common.Options;

public sealed class GitHubOptions
{
    public string Token { get; init; } = "";
    public string[] AllowedRepositories { get; init; } = [];
    public Dictionary<string, RepositoryActionOptions> PullActions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RepositoryActionOptions
{
    public string OwnerToken { get; init; } = "";
    public long[] ApproverIds { get; init; } = [];
    public long[] MergerIds { get; init; } = [];
}

public sealed class ReviewEmailOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string From { get; init; } = "";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public Dictionary<string, string> VerifiedAuthorEmails { get; init; } = new();
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
