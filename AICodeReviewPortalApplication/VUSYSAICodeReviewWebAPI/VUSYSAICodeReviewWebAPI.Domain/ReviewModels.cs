namespace VUSYSAICodeReviewWebAPI.Domain;

public record Finding(string Severity, string File, int? Line, string Title, string Description, string Recommendation,
    string? Category = null, string? SuggestedCode = null, string? CommitSha = null, int? EndLine = null)
{
    public string FileName => File;
    public int? LineNumber => Line;
}

public record Coverage(int TotalFiles, int ReviewedFiles, int PartialFiles, string[] Limitations);

public record ReviewResult(long Id, string Repository, int PullNumber, string Title, string CommitSha,
    DateTime CreatedUtc, List<Finding> Findings, string Summary, bool Published,
    string PublicationStatus = "Ready", long? GitHubReviewId = null, Coverage? Coverage = null, bool? IsOutdated = null, string ReviewType = "AI");
