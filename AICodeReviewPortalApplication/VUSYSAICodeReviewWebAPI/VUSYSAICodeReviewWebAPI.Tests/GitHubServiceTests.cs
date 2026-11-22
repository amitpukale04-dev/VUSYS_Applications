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

        var reviewService = new ReviewService(Service(http), null!, repository);
        var request = new ManualReviewRequest("acme", "portal", 42, head, baseSha, "General comment",
            [new("src/new.cs", line, "Inline comment")]);
        if (errorCode == 0)
        {
            Assert.Equal(808, await reviewService.PublishManual(request, TestContextToken));
            Assert.Equal(1, posted);
            Assert.Equal(808L, repository.GitHubId);
            var duplicate = await Assert.ThrowsAsync<PortalException>(() => reviewService.PublishManual(request, TestContextToken));
            Assert.Equal(409, duplicate.StatusCode);
            Assert.Equal(1, posted);
        }
        else
        {
            var error = await Assert.ThrowsAsync<PortalException>(() => reviewService.PublishManual(request, TestContextToken));
            Assert.Equal(errorCode, error.StatusCode);
            Assert.Equal(errorCode == 502 ? 1 : 0, posted);
            Assert.Equal(errorCode == 502, repository.Saved is not null);
            Assert.Null(repository.GitHubId);
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
        => new(http, Options.Create(new GitHubOptions { Token = "test-placeholder", AllowedRepositories = ["acme/portal"] }));

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private static HttpResponseMessage Blob(string text)
        => Json(new { type = "file", size = text.Length, encoding = "base64",
            content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text)) });

    private static object File(string name, string status, string? patch, string? previous, int additions)
        => new { filename = name, previous_filename = previous, status, patch, additions, deletions = 0, changes = additions, sha = "blob" };

    private static object Pull(int files)
        => new
        {
            number = 42, title = "Test PR", state = "open", html_url = "https://github.com/acme/portal/pull/42",
            created_at = DateTime.UnixEpoch, updated_at = DateTime.UnixEpoch, user = new { login = "dev" },
            head = new { sha = "head", @ref = "feature", repo = new { name = "portal", owner = new { login = "fork" } } },
            @base = new { sha = "base", @ref = "main", repo = new { full_name = "acme/portal" } }, changed_files = files
        };

    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
