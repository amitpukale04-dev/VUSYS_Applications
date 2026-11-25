using System.Text.Json;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Infrastructure.Services;

public sealed partial class GitHubService
{
    public async Task<JsonElement> Threads(string owner, string repo, int number, string? cursor, CancellationToken ct)
    {
        PullRoute(owner, repo, number);
        const string query = """
            query($owner: String!, $repo: String!, $number: Int!, $cursor: String) {
              repository(owner: $owner, name: $repo) {
                pullRequest(number: $number) {
                  reviewThreads(first: 25, after: $cursor) {
                    nodes {
                      id path line originalLine isResolved isOutdated
                      comments(first: 10) {
                        nodes { id body createdAt author { login } }
                        pageInfo { endCursor hasNextPage }
                      }
                    }
                    pageInfo { endCursor hasNextPage }
                  }
                }
              }
            }
            """;
        var data = await ReviewGraph(query, new { owner, repo, number, cursor }, ct);
        if (ObjectField(ObjectField(ObjectField(data, "repository"), "pullRequest"), "reviewThreads") is not { } threads)
            throw new PortalException(404, "GitHub review threads are unavailable for this PR.");
        return threads;
    }

    public async Task<JsonElement> ThreadComments(string owner, string repo, int number, string threadId,
        string? cursor, CancellationToken ct)
    {
        var thread = await ReadThread(owner, repo, number, threadId, cursor, null, ct);
        return thread.GetProperty("comments");
    }

    public async Task UpdateThread(string owner, string repo, int number, string threadId, string? reply, CancellationToken ct)
    {
        PullRoute(owner, repo, number);
        var settings = ActionSettings(owner, repo);
        var (token, _) = await OwnerCredential(settings.OwnerToken, [..settings.ApproverIds, ..settings.MergerIds], ct);
        var existing = await ReadThread(owner, repo, number, threadId, null, token, ct);
        if (reply is null && existing.GetProperty("isResolved").GetBoolean())
            return;

        string permission = reply is null ? "viewerCanResolve" : "viewerCanReply";
        if (!existing.GetProperty(permission).GetBoolean())
            throw new PortalException(403, "GitHub does not permit the configured owner to perform this thread action.");

        if (reply is not null)
        {
            var root = existing.GetProperty("root").GetProperty("nodes")[0];
            if (!root.TryGetProperty("fullDatabaseId", out var id) || !long.TryParse(id.ToString(), out long commentId) || commentId <= 0)
                throw new PortalException(502, "GitHub did not return the root review comment ID.");
            var response = (await Send(HttpMethod.Post, $"{PullRoute(owner, repo, number)}/comments/{commentId}/replies",
                new { body = reply }, ct, actorToken: token)).Data;
            if (!response.TryGetProperty("id", out var replyId) || !replyId.TryGetInt64(out long confirmedId) || confirmedId <= 0)
                throw new PortalException(502, "GitHub did not confirm the reply. Refresh the thread before retrying.");
            return;
        }

        const string resolve = """
            mutation($threadId: ID!) {
              resolveReviewThread(input: {threadId: $threadId}) { thread { id isResolved } }
            }
            """;
        var result = await ReviewGraph(resolve, new { threadId }, ct, token);
        if (ObjectField(ObjectField(result, "resolveReviewThread"), "thread") is not { } resolved
            || Text(resolved, "id") != threadId || !resolved.GetProperty("isResolved").GetBoolean())
            throw new PortalException(502, "GitHub did not confirm that the conversation was resolved.");
    }

    private async Task<JsonElement> ReadThread(string owner, string repo, int number, string threadId,
        string? cursor, string? token, CancellationToken ct)
    {
        PullRoute(owner, repo, number);
        const string query = """
            query($owner: String!, $repo: String!, $number: Int!, $threadId: ID!, $cursor: String) {
              repository(owner: $owner, name: $repo) { pullRequest(number: $number) { id } }
              node(id: $threadId) {
                ... on PullRequestReviewThread {
                  id isResolved viewerCanReply viewerCanResolve
                  root: comments(first: 1) { nodes { fullDatabaseId pullRequest { id } } }
                  comments(first: 25, after: $cursor) {
                    nodes { id body createdAt author { login } }
                    pageInfo { endCursor hasNextPage }
                  }
                }
              }
            }
            """;
        var data = await ReviewGraph(query, new { owner, repo, number, threadId, cursor }, ct, token);
        var pull = ObjectField(ObjectField(data, "repository"), "pullRequest");
        var thread = ObjectField(data, "node");
        if (pull is not { } requested || thread is not { } found || Text(found, "id") != threadId
            || ObjectField(found, "root") is not { } root || root.GetProperty("nodes").GetArrayLength() == 0
            || ObjectField(root.GetProperty("nodes")[0], "pullRequest") is not { } actual
            || Text(requested, "id") is not { } requestedId || Text(actual, "id") != requestedId)
            throw new PortalException(404, "The review thread was not found on this PR.");
        return found;
    }

    private async Task<JsonElement> ReviewGraph(string query, object variables, CancellationToken ct, string? token = null)
    {
        var response = (await Send(HttpMethod.Post, "graphql", new { query, variables }, ct, actorToken: token)).Data;
        if (response.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array
            && errors.GetArrayLength() > 0)
        {
            if (errors.EnumerateArray().Any(error => Text(error, "type") == "FORBIDDEN"))
                throw new PortalException(403, "GitHub denied access to the requested review conversation operation.");
            throw new PortalException(502, "GitHub could not complete the review conversation request.");
        }
        return ObjectField(response, "data")
            ?? throw new PortalException(502, "GitHub returned an incomplete review conversation response.");
    }

    private static JsonElement? ObjectField(JsonElement? value, string name)
        => value is { ValueKind: JsonValueKind.Object } obj && obj.TryGetProperty(name, out var field)
            && field.ValueKind == JsonValueKind.Object ? field : null;
}
