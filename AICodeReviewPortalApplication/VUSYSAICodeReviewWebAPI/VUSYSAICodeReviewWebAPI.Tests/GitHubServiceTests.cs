using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VUSYSAICodeReviewWebAPI.Application.Features;
using Microsoft.Extensions.Options;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;
using VUSYSAICodeReviewWebAPI.Infrastructure.Services;
using Xunit;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Persistence;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;
using VUSYSAICodeReviewWebAPI.Domain;

namespace VUSYSAICodeReviewWebAPI.Tests;

public sealed class GitHubServiceTests
{
    [Fact]
    public async Task FilesFollowPaginationAndDistinguishMissingPatches()
    {
        using var http = new HttpClient(new Stub(request =>
        {
            var uri = request.RequestUri!;
            Assert.Equal("api.github.com", uri.Host);
            if (!uri.AbsolutePath.EndsWith("/files", StringComparison.Ordinal))
                return Json(Pull(2));
            if (uri.Query.EndsWith("&page=1", StringComparison.Ordinal))
            {
                var response = Json(new[] { File("new.cs", "added", "@@ -0,0 +1 @@\n+new", null, 1) });
                response.Headers.Add("Link", "<https://example.invalid/>; rel=\"next\"");
                return response;
            }
            Assert.EndsWith("&page=2", uri.Query);
            return Json(new[] { File("new-name.cs", "renamed", null, "old-name.cs", 0) });
        }));

        var result = await Service(http).Files("acme", "portal", 42, TestContextToken);
        Assert.True(result.FileListComplete);
        Assert.Equal(2, result.ReturnedFileCount);
        Assert.Equal("Complete", result.Files[0].DiffCompleteness);
        Assert.Equal("old-name.cs", result.Files[1].PreviousFileName);
        Assert.Equal("Unavailable", result.Files[1].DiffCompleteness);
        Assert.Null(result.Files[1].IsBinary);
    }

    [Fact]
    public async Task ForkRenameUsesPreviousPathAtMergeBaseAndCurrentPathAtForkHead()
    {
        using var http = new HttpClient(new Stub(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            string query = request.RequestUri.Query;
            if (path.EndsWith("/files", StringComparison.Ordinal))
                return Json(new[] { File("src/new.cs", "renamed", "@@ -0,0 +1 @@\n+new", "src/old.cs", 1) });
            if (path.Contains("/compare/", StringComparison.Ordinal))
            {
                Assert.EndsWith("base...fork%3Ahead", path);
                return Json(new { merge_base_commit = new { sha = "merge-base" } });
            }
            if (path.EndsWith("/contents/src/old.cs", StringComparison.Ordinal))
            {
                Assert.Equal("?ref=merge-base", query);
                return Blob("old");
            }
            if (path == "/repos/acme/portal/contents/src/new.cs")
                return new(HttpStatusCode.NotFound);
            if (path == "/repos/fork/portal/contents/src/new.cs")
            {
                Assert.Equal("?ref=head", query);
                return Blob("new");
            }
            Assert.Equal("/repos/acme/portal/pulls/42", path);
            return Json(Pull(1));
        }));

        var result = await Service(http).Content("acme", "portal", 42, "src/new.cs", TestContextToken);
        Assert.Equal("src/old.cs", result.Before.Path);
        Assert.Equal("old", result.Before.Content);
        Assert.Equal("new", result.After.Content);
        Assert.Equal("head", result.After.CommitSha);
    }

    [Fact]
    public async Task RejectsRepositoriesOutsideAllowlistBeforeSendingHttp()
    {
        using var http = new HttpClient(new Stub(_ => throw new InvalidOperationException("Unexpected HTTP request.")));
        var error = await Assert.ThrowsAsync<PortalException>(() => Service(http).Pull("other", "portal", 42, TestContextToken));
        Assert.Equal(403, error.StatusCode);
    }

    [Theory]
    [InlineData("head", "base", 1, 0)]
    [InlineData("stale", "base", 1, 409)]
    [InlineData("head", "stale", 1, 409)]
    [InlineData("head", "base", 2, 422)]
    [InlineData("head", "base", 1, 502)]
    public async Task ManualPublicationValidatesSnapshotAndDiffLine(string head, string baseSha, int line, int errorCode)
    {
        int posted = 0;
        var repository = new ManualRepository();
        using var http = new HttpClient(new Stub(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                posted++;
                Assert.NotNull(repository.Saved);
                Assert.Equal("Manual", repository.Saved.ReviewType);
                Assert.Equal("General comment", repository.Saved.Summary);
                var storedComment = Assert.Single(repository.Saved.Findings);
                Assert.Equal("Inline comment", storedComment.Description);
                Assert.Equal("src/new.cs", storedComment.File);
                Assert.Equal(1, storedComment.Line);
                Assert.Null(repository.GitHubId);
                using var json = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert.Equal("COMMENT", json.RootElement.GetProperty("event").GetString());
                Assert.Equal("head", json.RootElement.GetProperty("commit_id").GetString());
                var comment = json.RootElement.GetProperty("comments")[0];
                Assert.Equal("src/new.cs", comment.GetProperty("path").GetString());
                Assert.Equal(1, comment.GetProperty("position").GetInt32());
                Assert.EndsWith("<!-- vusys-review:88 -->", json.RootElement.GetProperty("body").GetString());
                return errorCode == 502 ? new(HttpStatusCode.BadGateway) : Json(new { id = 808 });
            }

            return request.RequestUri!.AbsolutePath.EndsWith("/files", StringComparison.Ordinal)
                ? Json(new[] { File("src/new.cs", "added", "@@ -0,0 +1 @@\n+new", null, 1) })
                : Json(Pull(1));
        }));

        var email = new FailingEmail(repository);
        var reviewService = new ReviewService(Service(http), null!, repository, email);
        var request = new ManualReviewRequest("acme", "portal", 42, head, baseSha, "General comment",
            [new("src/new.cs", line, "Inline comment")]);
        if (errorCode == 0)
        {
            var publication = await reviewService.PublishManual(request, TestContextToken);
            Assert.True(publication.Published);
            Assert.Equal(808L, publication.GitHubReviewId);
            Assert.Equal("failed", publication.EmailStatus);
            Assert.Equal(1, posted);
            Assert.Equal(808L, repository.GitHubId);
            var duplicate = await Assert.ThrowsAsync<PortalException>(() => reviewService.PublishManual(request, TestContextToken));
            Assert.Equal(409, duplicate.StatusCode);
            var notification = Assert.Single(email.Notifications);
            Assert.Equal("acme/portal", notification.Repository);
            Assert.Equal("Test PR", notification.Title);
            Assert.Equal(808L, notification.ReviewId);
            Assert.Equal(808L, notification.RecordedId);
            Assert.Contains("Inline comment", notification.Body);
            Assert.Equal(1, posted);
        }
        else
        {
            var error = await Assert.ThrowsAsync<PortalException>(() => reviewService.PublishManual(request, TestContextToken));
            Assert.Equal(errorCode, error.StatusCode);
            Assert.Equal(errorCode == 502 ? 1 : 0, posted);
            Assert.Equal(errorCode == 502, repository.Saved is not null);
            Assert.Null(repository.GitHubId);
            Assert.Empty(email.Notifications);
        }
    }

    [Theory]
    [InlineData(false, "head", "main", 7, "reviewer_token", 0)]
    [InlineData(false, "stale", "main", 7, "reviewer_token", 409)]
    [InlineData(false, "head", "main", 9, "reviewer_token", 403)]
    [InlineData(false, "head", "main", 7, "test_placeholder", 403)]
    [InlineData(true, "head", "develop", 7, "reviewer_token", 409)]
    [InlineData(true, "head", "main", 7, "reviewer_token", 0)]
    [InlineData(false, "head", "main", 7, "reviewer_token", 409, false)]
    [InlineData(true, "head", "main", 7, "reviewer_token", 409, true, false)]
    public async Task PullActionsPinTheHeadAndRequireAnAllowedUser(bool merge, string head, string target,
        int userId, string token, int errorCode, bool commented = true, bool approved = true)
    {
        int writes = 0;
        using var http = new HttpClient(new Stub(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path == "/graphql")
                return Json(new { data = new { repository = new { pullRequest = new
                    { headRefOid = "head", baseRefName = target, reviewDecision = approved ? "APPROVED" : "REVIEW_REQUIRED" } } } });
            if (request.Method == HttpMethod.Get && path.EndsWith("/reviews", StringComparison.Ordinal))
                return Json(new[] {
                    new { state = commented ? "COMMENTED" : "PENDING", body = "<!-- vusys-review:10 -->", commit_id = "head", user = new { login = "bot" } },
                    new { state = approved ? "APPROVED" : "CHANGES_REQUESTED", body = "", commit_id = "head", user = new { login = "reviewer" } }
                });
            if (request.Method != HttpMethod.Get || path is "/user" or "/repos/acme/portal")
                Assert.Equal("reviewer_token", request.Headers.Authorization?.Parameter);
            if (request.Method == HttpMethod.Get)
                return path switch
                {
                    "/user" => Json(new { id = userId, login = "udaya-rao", type = "User" }),
                    "/repos/acme/portal" => Json(new { permissions = new { push = true } }),
                    "/repos/acme/portal/pulls/42" => Json(Pull(1, target)),
                    _ => throw new InvalidOperationException("Unexpected read.")
                };
            writes++;
            using var json = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            var body = json.RootElement;
            if (merge)
            {
                Assert.EndsWith("/merge-async", path);
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("head", body.GetProperty("sha").GetString());
                Assert.Equal("default", body.GetProperty("merge_action").GetString());
                Assert.False(body.GetProperty("bypass_rules").GetBoolean());
                return Json(new { status = "enqueued" });
            }
            Assert.EndsWith("/reviews", path);
            Assert.Equal("head", body.GetProperty("commit_id").GetString());
            Assert.Equal("APPROVE", body.GetProperty("event").GetString());
            return Json(new { state = "APPROVED", commit_id = "head" });
        }));
        var service = new GitHubService(http, Options.Create(new GitHubOptions
        {
            Token = "test_placeholder", AllowedRepositories = ["acme/portal"],
            PullActions = new() { ["acme/portal"] = new() { OwnerToken = token, ApproverIds = [7], MergerIds = [7] } }
        }));

        if (errorCode == 0)
        {
            var result = await service.Execute("acme", "portal", 42, head, merge, TestContextToken);
            Assert.Equal(merge ? "enqueued" : "approved", result.Status);
            Assert.Equal("udaya-rao", result.Actor);
        }
        else
        {
            var error = await Assert.ThrowsAsync<PortalException>(() =>
                service.Execute("acme", "portal", 42, head, merge, TestContextToken));
            Assert.Equal(errorCode, error.StatusCode);
        }
        Assert.Equal(errorCode == 0 ? 1 : 0, writes);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    public async Task ReviewThreadWritesUseOwnerCredentialAndVerifyScope(bool resolve, bool samePull, bool permitted)
    {
        int writes = 0;
        using var http = new HttpClient(new Stub(request =>
        {
            Assert.Equal("owner_token", request.Headers.Authorization?.Parameter);
            if (request.RequestUri!.AbsolutePath == "/user")
                return Json(new { id = 7, login = "udaya-rao", type = "User" });
            using var json = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            var payload = json.RootElement;
            if (request.RequestUri.AbsolutePath.EndsWith("/comments/5500000000/replies", StringComparison.Ordinal))
            {
                writes++;
                Assert.False(resolve);
                Assert.Equal("Fixed in latest commit.", payload.GetProperty("body").GetString());
                return Json(new { id = 5500000001L });
            }
            Assert.Equal("thread-1", payload.GetProperty("variables").GetProperty("threadId").GetString());
            string query = payload.GetProperty("query").GetString()!;
            if (query.StartsWith("query", StringComparison.Ordinal))
                return Json(new { data = new
                {
                    repository = new { pullRequest = new { id = "pull-42" } },
                    node = new { id = "thread-1", isResolved = false, viewerCanResolve = permitted, viewerCanReply = permitted,
                        root = new { nodes = new[] { new { fullDatabaseId = "5500000000", pullRequest = new { id = samePull ? "pull-42" : "pull-99" } } } } }
                } });
            writes++;
            if (resolve)
            {
                Assert.Contains("resolveReviewThread", query);
                return Json(new { data = new { resolveReviewThread = new { thread = new { id = "thread-1", isResolved = true } } } });
            }
            throw new InvalidOperationException("Unexpected GitHub write.");
        }));
        var service = new GitHubService(http, Options.Create(new GitHubOptions
        {
            Token = "test_placeholder", AllowedRepositories = ["acme/portal"],
            PullActions = new() { ["acme/portal"] = new() { OwnerToken = "owner_token", ApproverIds = [7] } }
        }));
        Task Update() => service.UpdateThread("acme", "portal", 42, "thread-1", resolve ? null : "Fixed in latest commit.", TestContextToken);
        if (samePull && permitted)
            await Update();
        else
            Assert.Equal(samePull ? 403 : 404, (await Assert.ThrowsAsync<PortalException>(Update)).StatusCode);
        Assert.Equal(samePull && permitted ? 1 : 0, writes);
    }

    private sealed class FailingEmail(ManualRepository repository) : IReviewEmail
    {
        public List<(string Repository, string Title, long ReviewId, long? RecordedId, string Body)> Notifications { get; } = [];

        public Task<(string Status, string Message)> Notify(Pull pull, string repositoryName, long reviewId,
            string body, CancellationToken ct)
        {
            Notifications.Add((repositoryName, pull.Title, reviewId, repository.GitHubId, body));
            throw new InvalidOperationException("Simulated SMTP failure.");
        }
    }

    private sealed class ManualRepository : IReviewRepository
    {
        public ReviewResult? Saved { get; private set; }
        public long? GitHubId { get; private set; }

        public Task<long> ClaimManual(ReviewResult review, byte[] fingerprint, CancellationToken ct)
        {
            Assert.Equal(32, fingerprint.Length);
            if (Saved is not null)
                throw new PortalException(409, "Duplicate manual publication.");
            Saved = review with { Id = 88, PublicationStatus = "PublishingUnconfirmed" };
            return Task.FromResult(88L);
        }

        public Task Complete(long id, long gitHubReviewId, CancellationToken ct)
        {
            Assert.Equal(Saved?.Id, id);
            GitHubId = gitHubReviewId;
            return Task.CompletedTask;
        }

        public Task<ReviewResult> Save(ReviewResult review, CancellationToken ct) => throw new NotSupportedException();
        public Task<ReviewResult?> Get(long id, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<ReviewResult>> List(string[] repositories, int? number, string? severity, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<bool> Reserve(long id, CancellationToken ct) => throw new NotSupportedException();
    }

    private static CancellationToken TestContextToken => CancellationToken.None;

    private static GitHubService Service(HttpClient http)
        => new(http, Options.Create(new GitHubOptions { Token = "test_placeholder", AllowedRepositories = ["acme/portal"] }));

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private static HttpResponseMessage Blob(string text)
        => Json(new { type = "file", size = text.Length, encoding = "base64",
            content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text)) });

    private static object File(string name, string status, string? patch, string? previous, int additions)
        => new { filename = name, previous_filename = previous, status, patch, additions, deletions = 0, changes = additions, sha = "blob" };

    private static object Pull(int files, string target = "main")
        => new
        {
            number = 42, title = "Test PR", state = "open", html_url = "https://github.com/acme/portal/pull/42",
            created_at = DateTime.UnixEpoch, updated_at = DateTime.UnixEpoch, user = new { login = "dev" },
            head = new { sha = "head", @ref = "feature", repo = new { name = "portal", owner = new { login = "fork" } } },
            @base = new { sha = "base", @ref = target, repo = new { full_name = "acme/portal" } }, changed_files = files
        };

    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
