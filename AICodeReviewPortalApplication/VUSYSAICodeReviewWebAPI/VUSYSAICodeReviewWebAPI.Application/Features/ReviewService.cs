using System.Security.Cryptography;
using System.Text.Json;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Persistence;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Domain;

namespace VUSYSAICodeReviewWebAPI.Application.Features;

public sealed class ReviewService(IGitHubService gitHub, IGeminiService gemini, IReviewRepository repository)
{
    private const int ReviewInputLimit = 120_000;

    public async Task<ReviewResult> Analyze(ReviewRequest input, CancellationToken ct)
    {
        var snapshot = await gitHub.Files(input.Owner, input.Repo, input.Number, ct);
        var pull = snapshot.PullRequest;
        var available = snapshot.Files.Where(f => f.DiffCompleteness is "Complete" or "Partial").ToList();
        if (available.Count == 0)
            throw new PortalException(422, "No reviewable text patches are available for this pull request.");
        string diff = JsonSerializer.Serialize(available.Select(f => new { file = f.FileName, patch = f.Patch }));
        if (diff.Length > ReviewInputLimit)
            throw new PortalException(422, $"The {available.Count} reviewable patches exceed the configured review input limit. No files were sent to Gemini.");
        var (summary, modelFindings) = await gemini.Review(diff, ct);
        if (available.Select(file => file.FileName).Distinct(StringComparer.Ordinal).Count() != available.Count)
            throw new PortalException(409, "GitHub returned duplicate file paths. Retry the review.");
        var files = available.ToDictionary(file => file.FileName, StringComparer.Ordinal);
        if (modelFindings.Any(finding => !files.ContainsKey(finding.File)))
            throw new PortalException(502, "Gemini returned findings for files outside the reviewed patch set.");

        var findings = modelFindings.Select(finding =>
        {
            var file = files[finding.File];
            int? line = IsRightSideLine(file, finding.Line) ? finding.Line : null;
            int? endLine = line.HasValue && IsRightSideLine(file, finding.EndLine) ? finding.EndLine : null;
            return finding with { CommitSha = pull.HeadSha, Line = line, EndLine = endLine };
        }).ToList();
        var limitations = new List<string>();
        if (!snapshot.FileListComplete) limitations.Add("GitHub did not return the full changed-file list.");
        if (available.Count < snapshot.Files.Count) limitations.Add("Files without usable text patches were excluded.");
        int partial = available.Count(file => file.DiffCompleteness == "Partial");
        if (partial > 0) limitations.Add("Some reviewed files had partial patches.");
        var coverage = new Coverage(pull.ChangedFiles ?? snapshot.Files.Count, available.Count, partial, limitations.ToArray());
        var current = await gitHub.Pull(input.Owner, input.Repo, input.Number, ct);
        if (current.HeadSha != pull.HeadSha || current.BaseSha != pull.BaseSha)
            throw new PortalException(409, "The pull request changed during analysis. Run the review again.");
        string fullName = pull.Metadata.GetProperty("base").GetProperty("repo").GetProperty("full_name").GetString()!;
        return await repository.Save(new(0, fullName, input.Number, pull.Title, pull.HeadSha,
            DateTime.UtcNow, findings, summary, false, Coverage: coverage, IsOutdated: false), ct);
    }

    public async Task<ReviewResult> Get(long id, CancellationToken ct)
    {
        var result = await repository.Get(id, ct) ?? throw new PortalException(404, "Review was not found.");
        var parts = result.Repository.Split('/');
        gitHub.EnsureAllowed(parts[0], parts[1]);
        return await CheckHead(result, ct);
    }

    public async Task<List<ReviewResult>> List(string[] allowed, string? name, int? number, string? severity,
        int page, int pageSize, bool checkOutdated, CancellationToken ct)
    {
        if (name is not null)
        {
            var parts = name.Split('/');
            if (parts.Length != 2) throw new PortalException(400, "Use owner/repository for the repository filter.");
            gitHub.EnsureAllowed(parts[0], parts[1]);
            allowed = [name];
        }

        var reviews = await repository.List(allowed, number, severity, page, pageSize, ct);
        if (!checkOutdated) return reviews;
        var outdatedResults = new List<ReviewResult>();
        var heads = new Dictionary<(string, int), string?>();
        foreach (var review in reviews)
        {
            var key = (review.Repository.ToUpperInvariant(), review.PullNumber);
            if (!heads.TryGetValue(key, out string? current))
            {
                current = await ReadHead(review, ct);
                heads.Add(key, current);
            }
            outdatedResults.Add(review with { IsOutdated = current is null ? null : current != review.CommitSha });
        }
        return outdatedResults;
    }

    public async Task<long> Publish(PublishRequest input, CancellationToken ct)
    {
        var (review, body, comments) = await PreparePublication(input, ct);
        var current = await gitHub.Pull(input.Owner, input.Repo, input.Number, ct);
        if (current.HeadSha != review.CommitSha)
            throw new PortalException(409, "PR changed since analysis. Run the review again.");

        // The SQL claim persists across process failures. A failed or ambiguous remote write requires reconciliation.
        if (!await repository.Reserve(review.Id, ct))
            throw new PortalException(409, "A review already claims publication for this PR commit.");
        long gitHubId = await gitHub.Publish(input.Owner, input.Repo, input.Number, review.CommitSha,
            body + $"\n\n<!-- vusys-review:{review.Id} -->", comments, ct);
        using var finalizeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await repository.Complete(review.Id, gitHubId, finalizeTimeout.Token);
        return gitHubId;
    }

    public async Task<long> PublishManual(ManualReviewRequest input, CancellationToken ct)
    {
        gitHub.EnsureAllowed(input.Owner, input.Repo);
        var body = input.Body?.Trim();
        var requested = input.Comments ?? [];
        if (string.IsNullOrWhiteSpace(body) && requested.Length == 0)
            throw new PortalException(400, "Enter a review comment or add an inline comment.");

        var comments = new List<InlineComment>();
        if (requested.Length > 0)
        {
            var snapshot = await gitHub.Files(input.Owner, input.Repo, input.Number, ct);
            if (snapshot.PullRequest.HeadSha != input.HeadSha || snapshot.PullRequest.BaseSha != input.BaseSha)
                throw new PortalException(409, "The pull request changed. Refresh the files and check your comments.");
            foreach (var comment in requested)
            {
                if (comment is null || string.IsNullOrWhiteSpace(comment.Body))
                    throw new PortalException(400, "Inline comments require text.");
                var file = snapshot.Files.Find(file => file.FileName == comment.FileName);
                if (file is null || DiffPosition(file, comment.Line) is not int position)
                    throw new PortalException(422, "An inline comment refers to a line outside a complete PR patch.");
                comments.Add(new(comment.FileName, position, comment.Body.Trim()));
            }
        }

        var current = await gitHub.Pull(input.Owner, input.Repo, input.Number, ct);
        if (current.HeadSha != input.HeadSha || current.BaseSha != input.BaseSha)
            throw new PortalException(409, "The pull request changed. Refresh the files and check your comments.");
        string repositoryName = $"{input.Owner}/{input.Repo}";
        string summary = string.IsNullOrWhiteSpace(body) ? "Manual code review" : body;
        var findings = requested.Select(comment => new Finding("Informational", comment.FileName, comment.Line,
            "Inline comment", comment.Body.Trim(), "", Category: "Manual", CommitSha: current.HeadSha)).ToList();
        var fingerprint = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Repository = repositoryName.ToUpperInvariant(),
            input.Number,
            current.HeadSha,
            current.BaseSha,
            Summary = summary,
            InlineComments = findings.OrderBy(finding => finding.File, StringComparer.Ordinal)
                .ThenBy(finding => finding.Line).ThenBy(finding => finding.Description, StringComparer.Ordinal)
        }));
        var review = new ReviewResult(0, repositoryName, input.Number, current.Title, current.HeadSha,
            DateTime.UtcNow, findings, summary, false, ReviewType: "Manual");
        long id = await repository.ClaimManual(review, fingerprint, ct);
        long gitHubId = await gitHub.Publish(input.Owner, input.Repo, input.Number, current.HeadSha,
            summary + $"\n\n<!-- vusys-review:{id} -->", comments.ToArray(), ct);
        using var finalizeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await repository.Complete(id, gitHubId, finalizeTimeout.Token);
        return gitHubId;
    }

    public async Task<object> Preview(PublishRequest input, CancellationToken ct)
    {
        var (review, body, comments) = await PreparePublication(input, ct);
        return new { review, body, comments };
    }

    private async Task<(ReviewResult Review, string Body, InlineComment[] Comments)> PreparePublication(
        PublishRequest input, CancellationToken ct)
    {
        gitHub.EnsureAllowed(input.Owner, input.Repo);
        var review = await repository.Get(input.ReviewId, ct) ?? throw new PortalException(404, "Review was not found.");
        if (!review.Repository.Equals($"{input.Owner}/{input.Repo}", StringComparison.OrdinalIgnoreCase) || review.PullNumber != input.Number)
            throw new PortalException(400, "Review does not match PR.");
        if (review.ReviewType != "AI")
            throw new PortalException(409, "Use manual publication for manual reviews.");
        if (review.PublicationStatus != "Ready" || review.Published)
            throw new PortalException(409, "Review is published or has an unresolved publishing attempt.");

        var snapshot = await gitHub.Files(input.Owner, input.Repo, input.Number, ct);
        if (snapshot.PullRequest.HeadSha != review.CommitSha)
            throw new PortalException(409, "PR changed since analysis. Run the review again.");
        var files = snapshot.Files.GroupBy(file => file.FileName).ToDictionary(group => group.Key, group => group.First());
        var comments = new List<InlineComment>();
        foreach (var finding in review.Findings)
        {
            if (finding.Line is int line && files.TryGetValue(finding.File, out var file)
                && DiffPosition(file, line) is int position)
                comments.Add(new(finding.File, position, CommentText(finding)));
        }
        string body = "## VUSYS AI Code Review (Gemini)\n\n" + PlainText(review.Summary) + "\n\n"
            + string.Join("\n\n", review.Findings.Select(finding => PlainText(finding.File) + "\n" + CommentText(finding)));
        if (body.Length > 60_000)
            throw new PortalException(422, "Review summary exceeds the publishing size limit.");

        return (review with { IsOutdated = false }, body, comments.ToArray());
    }

    private static int? DiffPosition(ChangedFile file, int lineNumber)
    {
        if (file.DiffCompleteness != "Complete")
            return null;
        int position = -1;
        foreach (var hunk in file.Hunks)
        {
            position++;
            foreach (var line in hunk.Lines)
            {
                position++;
                if (line.NewLineNumber == lineNumber)
                    return position;
            }
        }
        return null;
    }

    private async Task<ReviewResult> CheckHead(ReviewResult review, CancellationToken ct)
    {
        string? head = await ReadHead(review, ct);
        return review with { IsOutdated = head is null ? null : head != review.CommitSha };
    }

    private async Task<string?> ReadHead(ReviewResult review, CancellationToken ct)
    {
        var parts = review.Repository.Split('/');
        try { return (await gitHub.Pull(parts[0], parts[1], review.PullNumber, ct)).HeadSha; }
        catch (PortalException ex) when (ex.StatusCode is 403 or 404) { return null; }
    }

    private static bool IsRightSideLine(ChangedFile file, int? line)
        => line.HasValue && file.DiffCompleteness == "Complete"
            && file.Hunks.Any(hunk => hunk.Lines.Any(item => item.NewLineNumber == line));

    private static string CommentText(Finding finding)
        => $"**[{finding.Severity}]** {PlainText(finding.Title)}\n\n{PlainText(finding.Description)}\n\n**Recommendation:** {PlainText(finding.Recommendation)}";

    private static string PlainText(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("@", "＠");
}
