using System.Text.Json;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Infrastructure.Services;

public sealed partial class GitHubService
{
    public async Task<PullActionStatus> Status(string owner, string repo, int number, CancellationToken ct)
    {
        string route = PullRoute(owner, repo, number);
        var pull = MapPull((await Send(HttpMethod.Get, route, null, ct, apiVersion: "2026-03-10")).Data);
        var blockers = new List<string>();
        if (pull.State != "open") blockers.Add("The pull request is closed.");
        if (pull.TargetBranch != "main") blockers.Add("The target branch is not main.");
        if (IsDraft(pull)) blockers.Add("The pull request is a draft.");
        if (IsStacked(pull)) blockers.Add("This PR belongs to a stack. Review the stack's merge scope on GitHub.");

        string? decision = null;
        string? checks = null;
        bool queued = false;
        bool queueEnabled = false;
        bool statusMatched = false;
        string? mergeState = Text(pull.Metadata, "mergeable_state")?.ToUpperInvariant();
        if (pull.State == "open")
        {
            const string query = """
                query($owner: String!, $repo: String!, $number: Int!) {
                  repository(owner: $owner, name: $repo) {
                    pullRequest(number: $number) {
                      headRefOid baseRefName mergeStateStatus reviewDecision isInMergeQueue isMergeQueueEnabled
                      commits(last: 1) { nodes { commit { statusCheckRollup { state } } } }
                    }
                  }
                }
                """;
            var result = (await Send(HttpMethod.Post, "graphql",
                new { query, variables = new { owner, repo, number } }, ct)).Data;
            if (result.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("repository", out var repository) && repository.ValueKind == JsonValueKind.Object
                && repository.TryGetProperty("pullRequest", out var status) && status.ValueKind == JsonValueKind.Object
                && Text(status, "headRefOid") == pull.HeadSha && Text(status, "baseRefName") == pull.TargetBranch)
            {
                decision = Text(status, "reviewDecision");
                statusMatched = true;
                mergeState = Text(status, "mergeStateStatus") ?? mergeState;
                queued = status.TryGetProperty("isInMergeQueue", out var inQueue) && inQueue.ValueKind == JsonValueKind.True;
                queueEnabled = status.TryGetProperty("isMergeQueueEnabled", out var enabled) && enabled.ValueKind == JsonValueKind.True;
                if (status.TryGetProperty("commits", out var commits) && commits.ValueKind == JsonValueKind.Object
                    && commits.TryGetProperty("nodes", out var nodes) && nodes.ValueKind == JsonValueKind.Array
                    && nodes.GetArrayLength() == 1 && nodes[0].TryGetProperty("commit", out var commit)
                    && commit.TryGetProperty("statusCheckRollup", out var rollup) && rollup.ValueKind == JsonValueKind.Object)
                    checks = Text(rollup, "state");
            }
        }

        if (queued) blockers.Add("The pull request is already in the merge queue.");
        if (decision == "REVIEW_REQUIRED") blockers.Add("GitHub requires additional approvals.");
        if (decision == "CHANGES_REQUESTED") blockers.Add("GitHub reports requested changes.");
        if (checks is "PENDING" or "EXPECTED") blockers.Add("GitHub reports checks are pending or expected.");
        if (checks is "FAILURE" or "ERROR") blockers.Add("GitHub reports failed checks.");
        string? mergeBlocker = mergeState switch
        {
            "BLOCKED" => "GitHub reports a branch rule is blocking merge. Check required reviews and checks on GitHub.",
            "BEHIND" => "The branch is behind its target.",
            "DIRTY" => "The pull request has merge conflicts.",
            "UNSTABLE" => "Some checks have not passed. GitHub determines whether they are required.",
            "HAS_HOOKS" => "GitHub will evaluate repository hooks during merge.",
            "CLEAN" or "DRAFT" => null,
            _ => "GitHub has not established mergeability yet."
        };
        if (pull.State == "open" && mergeBlocker is not null) blockers.Add(mergeBlocker);
        bool commentPublished = false;
        bool approved = false;
        if (pull.State == "open")
        {
            var reviews = await Pages($"{route}/reviews", null, 100, ct);
            commentPublished = reviews.Any(review => Text(review, "state") == "COMMENTED"
                && (Text(review, "body")?.Contains("<!-- vusys-review:", StringComparison.Ordinal) ?? false));
            var latestDecisions = reviews
                .Where(review => Text(review, "state") is "APPROVED" or "CHANGES_REQUESTED" or "DISMISSED")
                .Where(review => review.TryGetProperty("user", out var user) && user.ValueKind == JsonValueKind.Object)
                .GroupBy(review => Text(review.GetProperty("user"), "login"), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last());
            approved = statusMatched && (decision is null or "APPROVED") && latestDecisions.Any(review =>
                Text(review, "state") == "APPROVED" && Text(review, "commit_id") == pull.HeadSha);
        }
        return new(pull.Merged == true ? "merged" : pull.State, pull.TargetBranch, pull.HeadSha, decision, queued, queueEnabled, IsDraft(pull), IsStacked(pull), blockers.ToArray(), commentPublished, approved);
    }

    public async Task<PullActionResult> Execute(string owner, string repo, int number, string sha,
        bool merge, CancellationToken ct)
    {
        string route = PullRoute(owner, repo, number);
        var settings = ActionSettings(owner, repo);
        var (token, login) = await OwnerCredential(settings.OwnerToken,
            merge ? settings.MergerIds : settings.ApproverIds, ct);

        var repository = await ActorGet(RepoRoute(owner, repo), token, ct);
        if (!repository.TryGetProperty("permissions", out var permissions)
            || !permissions.TryGetProperty("push", out var push) || push.ValueKind != JsonValueKind.True)
            throw new PortalException(403, "GitHub write access to the repository is required.");

        var statusBeforeWrite = await Status(owner, repo, number, ct);
        if (!statusBeforeWrite.CommentPublished)
            throw new PortalException(409, "Publish a COMMENT review in the portal before proceeding.");
        if (merge && !statusBeforeWrite.Approved)
            throw new PortalException(409, "The current PR head requires an approval before merge.");
        if (merge && statusBeforeWrite.InMergeQueue)
            throw new PortalException(409, "This PR is already in the merge queue.");
        if (!string.Equals(statusBeforeWrite.HeadSha, sha, StringComparison.OrdinalIgnoreCase))
            throw new PortalException(409, "The PR head changed. Reload and review the latest changes.");

        var pull = MapPull(await ActorGet(route, token, ct));
        RequireHead(pull, sha);
        if (pull.State != "open" || IsDraft(pull))
            throw new PortalException(409, "The PR must be open and ready for review.");

        if (!merge)
        {
            if (string.Equals(Text(pull.Author, "login"), login, StringComparison.OrdinalIgnoreCase))
                throw new PortalException(403, "GitHub does not allow self-approval.");
            var review = (await Send(HttpMethod.Post, $"{route}/reviews",
                new { commit_id = sha, @event = "APPROVE" }, ct, actorToken: token)).Data;
            if (Text(review, "state") != "APPROVED" || !string.Equals(Text(review, "commit_id"), sha, StringComparison.OrdinalIgnoreCase))
                throw new PortalException(502, "GitHub did not confirm approval of the inspected commit. Check the PR before retrying.");
            try
            {
                var latest = MapPull(await ActorGet(route, token, ct));
                if (!string.Equals(latest.HeadSha, sha, StringComparison.OrdinalIgnoreCase))
                    return new("outdated", "GitHub recorded approval on the inspected commit. Reload and review the new head.", login);
            }
            catch (Exception exception) when (exception is PortalException or HttpRequestException or OperationCanceledException or JsonException)
            {
                return new("approved", "GitHub recorded approval. Refresh the PR to confirm the current head.", login);
            }
            return new("approved", "GitHub recorded approval on the inspected commit.", login);
        }

        if (pull.TargetBranch != "main")
            throw new PortalException(409, "This action requires a PR targeting main.");
        if (IsStacked(pull))
            throw new PortalException(409, "This PR belongs to a stack. Review the stack's merge scope on GitHub.");

        var result = (await Send(HttpMethod.Put, $"{route}/merge-async",
            new { sha, merge_action = "default", bypass_rules = false },
            ct, actorToken: token)).Data;
        string? status = Text(result, "status");
        for (int attempt = 0; attempt < 5 && status == "pending"; attempt++)
        {
            if (!result.TryGetProperty("details", out var details) || !Guid.TryParse(Text(details, "uuid"), out var idValue))
                break;
            try
            {
                using var poll = CancellationTokenSource.CreateLinkedTokenSource(ct);
                poll.CancelAfter(TimeSpan.FromSeconds(3));
                await Task.Delay(TimeSpan.FromSeconds(1), poll.Token);
                result = await ActorGet($"{route}/merge-async/{idValue:D}", token, poll.Token);
                status = Text(result, "status");
            }
            catch (Exception exception) when (exception is PortalException or HttpRequestException or OperationCanceledException or JsonException)
            {
                break;
            }
        }
        return status switch
        {
            "merged" => new("merged", "GitHub confirmed the merge.", login),
            "enqueued" => new("enqueued", "GitHub added the PR to the merge queue.", login),
            "failed" => new("failed", MergeFailure(result), login),
            _ => new("pending", "GitHub accepted the merge request. Check the PR on GitHub for the final outcome.", login)
        };
    }

    private VUSYSAICodeReviewWebAPI.Application.Common.Options.RepositoryActionOptions ActionSettings(string owner, string repo)
        => options.PullActions.FirstOrDefault(entry =>
            entry.Key.Equals($"{owner}/{repo}", StringComparison.OrdinalIgnoreCase)).Value
            ?? throw new PortalException(403, "Owner actions are not enabled for this repository.");

    private async Task<(string Token, string Login)> OwnerCredential(string token, long[] allowed, CancellationToken ct)
    {
        if (allowed.Length == 0 || string.IsNullOrWhiteSpace(token) || token.Length > 512
            || token.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_')
            || token.Equals(options.Token, StringComparison.Ordinal))
            throw new PortalException(403, "Configure a separate GitHub owner credential and authorized user IDs for this action.");

        var actor = await ActorGet("user", token, ct);
        string? login = Text(actor, "login");
        if (Text(actor, "type") != "User" || string.IsNullOrEmpty(login)
            || !actor.TryGetProperty("id", out var id) || !id.TryGetInt64(out long actorId) || !allowed.Contains(actorId))
            throw new PortalException(403, "The configured GitHub user is not authorized for this action.");
        return (token, login);
    }

    private async Task<JsonElement> ActorGet(string route, string token, CancellationToken ct)
        => (await Send(HttpMethod.Get, route, null, ct, actorToken: token)).Data;

    private static bool IsStacked(Pull pull)
        => pull.Metadata.TryGetProperty("stack", out var stack) && stack.ValueKind == JsonValueKind.Object;

    private static bool IsDraft(Pull pull)
        => pull.Metadata.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True;

    private static void RequireHead(Pull pull, string sha)
    {
        if (!string.Equals(pull.HeadSha, sha, StringComparison.OrdinalIgnoreCase))
            throw new PortalException(409, "The PR head changed. Reload and review the latest changes.");
    }

    private static string MergeFailure(JsonElement result)
        => result.TryGetProperty("details", out var details) && Text(details, "message") is { Length: > 0 } message
            ? $"GitHub did not merge the PR. {message}"
            : "GitHub did not merge the PR. Check the current requirements on GitHub.";
}
