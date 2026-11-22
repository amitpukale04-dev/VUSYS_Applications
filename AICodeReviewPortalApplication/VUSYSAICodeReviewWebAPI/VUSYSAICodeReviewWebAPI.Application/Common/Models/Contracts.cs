using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace VUSYSAICodeReviewWebAPI.Application.Common.Models;

public record LoginRequest([Required, StringLength(200)] string Username, [Required, StringLength(1024)] string Password);
public record LoginResponse(string Token, string Username, DateTime ExpiresUtc);
public record ReviewRequest([Required] string Owner, [Required] string Repo, [Range(1, int.MaxValue)] int Number);
public record PublishRequest([Required] string Owner, [Required] string Repo, [Range(1, int.MaxValue)] int Number,
    [Range(1, long.MaxValue)] long ReviewId);

public record ManualReviewComment([Required, StringLength(4096)] string FileName,
    [Range(1, int.MaxValue)] int Line, [Required, StringLength(2000)] string Body);
public record ManualReviewRequest([Required] string Owner, [Required] string Repo, [Range(1, int.MaxValue)] int Number,
    [Required, RegularExpression("[a-fA-F0-9]{40}")] string HeadSha,
    [Required, RegularExpression("[a-fA-F0-9]{40}")] string BaseSha, [StringLength(10000)] string? Body,
    [MaxLength(20)] ManualReviewComment[]? Comments);

public record Repository(string Name, string FullName, bool Private, string? Description, JsonElement Owner,
    string Visibility, string DefaultBranch, string? Language, DateTime CreatedAt, DateTime UpdatedAt, string Url, JsonElement Metadata);

public record Pull(int Number, string Title, string State, string Url, string HeadSha, DateTime CreatedAt,
    string? Description, JsonElement Author, string SourceBranch, string TargetBranch, string BaseSha,
    DateTime UpdatedAt, bool? Merged, int? Commits, int? ChangedFiles, int? Additions, int? Deletions, JsonElement Metadata);

public record DiffLine(string LineType, int? OldLineNumber, int? NewLineNumber, string Content);
public record DiffHunk(string Header, int OldStartLine, int OldLineCount, int NewStartLine, int NewLineCount, List<DiffLine> Lines);
public record DiffResult(string Completeness, List<DiffHunk> Hunks);
public record ChangedFile(string FileName, string? PreviousFileName, string FileStatus, int Additions, int Deletions,
    int Changes, string Sha, string? Patch, string? BlobUrl, string? RawUrl, string? ContentsUrl,
    bool? IsBinary, string PatchAvailability, string DiffCompleteness, List<DiffHunk> Hunks);
public record PullFiles(Pull PullRequest, List<ChangedFile> Files, int ReturnedFileCount, int UpstreamFileLimit, bool FileListComplete);
public record FileVersion(string Status, string Path, string? CommitSha, string? Encoding = null, string? Content = null, long? Size = null);
public record FileComparison(string FileName, FileVersion Before, FileVersion After, string? HeadSha = null, string? BaseSha = null);
public record InlineComment(string path, int position, string body);

public sealed class PortalException(int statusCode, string message, string? retryAfter = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string? RetryAfter { get; } = retryAfter;
}
