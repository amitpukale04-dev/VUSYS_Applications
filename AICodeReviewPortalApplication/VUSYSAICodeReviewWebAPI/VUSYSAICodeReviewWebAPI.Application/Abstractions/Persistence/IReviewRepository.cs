using VUSYSAICodeReviewWebAPI.Domain;

namespace VUSYSAICodeReviewWebAPI.Application.Abstractions.Persistence;

public interface IReviewRepository
{
    Task<ReviewResult> Save(ReviewResult review, CancellationToken ct);
    Task<long> ClaimManual(ReviewResult review, byte[] fingerprint, CancellationToken ct);
    Task<ReviewResult?> Get(long id, CancellationToken ct);
    Task<List<ReviewResult>> List(string[] repositories, int? number, string? severity, int page, int pageSize, CancellationToken ct);
    Task<bool> Reserve(long id, CancellationToken ct);
    Task Complete(long id, long gitHubReviewId, CancellationToken ct);
}
