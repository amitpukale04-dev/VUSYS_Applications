using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;
using VUSYSAICodeReviewWebAPI.Application.Features;

namespace VUSYSAICodeReviewWebAPI.Infrastructure.Services;

public sealed partial class GitHubService(HttpClient http, IOptions<GitHubOptions> configuration) : IGitHubService, IPullRequestActions
{
    private const int FileLimit = 3000;
    private const int ContentLimit = 512 * 1024;
    private readonly GitHubOptions options = configuration.Value;

    public void EnsureAllowed(string owner, string repo)
    {
        if (!ValidName(owner) || !ValidName(repo))
            throw new PortalException(400, "Invalid GitHub owner or repository name.");
        if (!IsAllowed($"{owner}/{repo}"))
            throw new PortalException(403, "Repository is outside the configured access scope.");
    }

    private bool IsAllowed(string fullName) => options.AllowedRepositories.Contains(fullName, StringComparer.OrdinalIgnoreCase);
    private static bool ValidName(string value) => value is not ("." or "..") && Regex.IsMatch(value, @"\A[A-Za-z0-9_.-]{1,100}\z");
    private string RepoRoute(string owner, string repo)
    {
        EnsureAllowed(owner, repo);
        return $"repos/{owner}/{repo}";
    }

    private string PullRoute(string owner, string repo, int number)
    {
        if (number < 1)
            throw new PortalException(400, "Pull request number must be positive.");
        return $"{RepoRoute(owner, repo)}/pulls/{number}";
    }

    public async Task<List<Repository>> Repositories(int? page, int pageSize, CancellationToken ct)
    {
        var all = await Pages("user/repos?sort=updated", null, 100, ct);
        var authorized = all.Select(MapRepository).Where(r => IsAllowed(r.FullName));
        return (page.HasValue ? authorized.Skip((page.Value - 1) * pageSize).Take(pageSize) : authorized).ToList();
    }

    public async Task<Repository> Repository(string owner, string repo, CancellationToken ct)
        => MapRepository((await Send(HttpMethod.Get, RepoRoute(owner, repo), null, ct)).Data);

    public Task<List<JsonElement>> Branches(string owner, string repo, int? page, int pageSize, CancellationToken ct)
        => Pages($"{RepoRoute(owner, repo)}/branches", page, pageSize, ct);

    public async Task<List<Pull>> Pulls(string owner, string repo, string state, int? page, int pageSize, CancellationToken ct)
    {
        if (state is not ("open" or "closed" or "all"))
            throw new PortalException(400, "State must be open, closed or all.");
        return (await Pages($"{RepoRoute(owner, repo)}/pulls?state={state}", page, pageSize, ct)).Select(MapPull).ToList();
    }

    public async Task<Pull> Pull(string owner, string repo, int number, CancellationToken ct)
        => MapPull((await Send(HttpMethod.Get, PullRoute(owner, repo, number), null, ct)).Data);

    public Task<List<JsonElement>> Commits(string owner, string repo, int number, int? page, int pageSize, CancellationToken ct)
        => Pages($"{PullRoute(owner, repo, number)}/commits", page, pageSize, ct);

    public async Task<PullFiles> Files(string owner, string repo, int number, CancellationToken ct)
    {
        var pull = await Pull(owner, repo, number, ct);
        var entries = await Pages($"{PullRoute(owner, repo, number)}/files", null, 100, ct, FileLimit);
        var files = entries.Select(MapFile).ToList();
        var current = await Pull(owner, repo, number, ct);
        if (pull.HeadSha != current.HeadSha || pull.BaseSha != current.BaseSha)
            throw new PortalException(409, "The PR changed while GitHub files were being retrieved. Retry the request.");
        bool complete = pull.ChangedFiles == files.Count
            && files.Select(f => f.FileName).Distinct(StringComparer.Ordinal).Count() == files.Count;
        return new(pull, files, files.Count, FileLimit, complete);
    }

    public async Task<FileComparison> Content(string owner, string repo, int number, string fileName, CancellationToken ct)
    {
        var snapshot = await Files(owner, repo, number, ct);
        var file = snapshot.Files.Find(f => f.FileName == fileName)
            ?? throw new PortalException(404, "File is outside the available GitHub changed-file list.");
        var pull = snapshot.PullRequest;
        string baseRoute = RepoRoute(owner, repo);
        string beforePath = file.PreviousFileName ?? fileName;
        FileVersion before;
        if (file.FileStatus is "added" or "copied")
            before = new("NotApplicable", beforePath, null);
        else
        {
            JsonElement headRepo = pull.Metadata.GetProperty("head").GetProperty("repo");
            string? headOwner = headRepo.ValueKind == JsonValueKind.Object
                ? Text(headRepo.GetProperty("owner"), "login") : null;
            string comparisonHead = headOwner is not null && !headOwner.Equals(owner, StringComparison.OrdinalIgnoreCase)
                ? $"{headOwner}:{pull.HeadSha}" : pull.HeadSha;
            string mergeBase;
            try
            {
                var result = await Send(HttpMethod.Get,
                    $"{baseRoute}/compare/{Uri.EscapeDataString(pull.BaseSha + "..." + comparisonHead)}?per_page=1", null, ct);
                mergeBase = result.Data.GetProperty("merge_base_commit").GetProperty("sha").GetString()!;
                before = await ReadVersion(baseRoute, beforePath, mergeBase, ct);
            }
            catch (PortalException ex) when (ex.StatusCode == 404)
            {
                before = new("MergeBaseUnavailable", beforePath, null);
            }
        }

        FileVersion after = file.FileStatus == "removed"
            ? new("NotApplicable", fileName, null)
            : await ReadVersion(baseRoute, fileName, pull.HeadSha, ct);
        if (after.Status == "Unavailable" && pull.Metadata.GetProperty("head").GetProperty("repo") is
            { ValueKind: JsonValueKind.Object } fork)
        {
            string? forkOwner = Text(fork.GetProperty("owner"), "login");
            string? forkName = Text(fork, "name");
            // Fork access is constrained to the selected PR's GitHub-reported source and immutable SHA.
            if (forkOwner is not null && forkName is not null && ValidName(forkOwner) && ValidName(forkName)
                && !string.Equals($"{forkOwner}/{forkName}", $"{owner}/{repo}", StringComparison.OrdinalIgnoreCase))
                after = await ReadVersion($"repos/{forkOwner}/{forkName}", fileName, pull.HeadSha, ct);
        }
        return new(fileName, before, after, pull.HeadSha, pull.BaseSha);
    }

    private async Task<FileVersion> ReadVersion(string route, string path, string sha, CancellationToken ct)
    {
        string[] segments = path.Split('/');
        if (segments.Any(segment => segment is "" or "." or ".." || segment.Contains('\\')))
            return new("Unavailable", path, sha);
        string encoded = string.Join("/", segments.Select(Uri.EscapeDataString));
        JsonElement data;
        try
        {
            data = (await Send(HttpMethod.Get, $"{route}/contents/{encoded}?ref={Uri.EscapeDataString(sha)}", null, ct,
                "application/vnd.github.object+json")).Data;
        }
        catch (PortalException ex) when (ex.StatusCode == 404)
        {
            return new("Unavailable", path, sha);
        }

        if (data.ValueKind != JsonValueKind.Object || Text(data, "type") != "file" || data.TryGetProperty("submodule_git_url", out _))
            return new("UnsupportedFileType", path, sha);
        long size = data.GetProperty("size").GetInt64();
        if (size > ContentLimit)
            return new("TooLarge", path, sha, Size: size);
        if (Text(data, "encoding") != "base64" || Text(data, "content") is not { } content)
            return new("Unavailable", path, sha, Size: size);
        try
        {
            byte[] bytes = Convert.FromBase64String(content);
            if (bytes.Length != size)
                return new("Unavailable", path, sha, Size: size);
            var (decoder, skip) = DetectEncoding(bytes);
            string decoded = decoder.GetString(bytes, skip, bytes.Length - skip);
            return decoded.Contains('\0') ? new("Binary", path, sha, Size: size)
                : new("Available", path, sha, decoder.WebName, decoded, size);
        }
        catch (DecoderFallbackException)
        {
            return new("BinaryOrUnsupportedEncoding", path, sha, Size: size);
        }
        catch (FormatException)
        {
            return new("Unavailable", path, sha, Size: size);
        }
    }

    private static (Encoding Decoder, int Skip) DetectEncoding(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xff, 0xfe, 0x00, 0x00 })) return (new UTF32Encoding(false, true, true), 4);
        if (bytes.StartsWith(new byte[] { 0, 0, 0xfe, 0xff })) return (new UTF32Encoding(true, true, true), 4);
        if (bytes.StartsWith(new byte[] { 0xff, 0xfe })) return (new UnicodeEncoding(false, true, true), 2);
        if (bytes.StartsWith(new byte[] { 0xfe, 0xff })) return (new UnicodeEncoding(true, true, true), 2);
        return (new UTF8Encoding(false, true), bytes.StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? 3 : 0);
    }

    public async Task<long> Publish(string owner, string repo, int number, string sha, string body,
        InlineComment[] comments, CancellationToken ct)
        => (await Send(HttpMethod.Post, $"{PullRoute(owner, repo, number)}/reviews",
            new { commit_id = sha, @event = "COMMENT", body, comments }, ct)).Data.GetProperty("id").GetInt64();

    private async Task<List<JsonElement>> Pages(string route, int? page, int pageSize, CancellationToken ct, int max = int.MaxValue)
    {
        var entries = new List<JsonElement>();
        int current = page ?? 1;
        bool next;
        do
        {
            string separator = route.Contains('?') ? "&" : "?";
            var response = await Send(HttpMethod.Get, $"{route}{separator}per_page={pageSize}&page={current++}", null, ct);
            entries.AddRange(response.Data.EnumerateArray());
            next = response.HasNext;
        } while (page is null && next && entries.Count < max);
        return entries.Take(max).ToList();
    }

    private async Task<(JsonElement Data, bool HasNext)> Send(HttpMethod method, string route, object? payload,
        CancellationToken ct, string accept = "application/vnd.github+json", string? actorToken = null, string? apiVersion = null)
    {
        if (string.IsNullOrWhiteSpace(actorToken ?? options.Token))
            throw new PortalException(503, "GitHub credentials are missing.");
        using var request = new HttpRequestMessage(method, new Uri("https://api.github.com/" + route));
        request.Headers.UserAgent.ParseAdd("VUSYS-CodeReview/1.0");
        request.Headers.Accept.ParseAdd(accept);
        request.Headers.Add("X-GitHub-Api-Version", apiVersion ?? (actorToken is null ? "2022-11-28" : "2026-03-10"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actorToken ?? options.Token);
        if (payload is not null)
            request.Content = JsonContent.Create(payload);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            string? retryAfter = response.Headers.RetryAfter?.ToString();
            bool limited = response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) && remaining.Contains("0");
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (response.StatusCode == HttpStatusCode.Forbidden && (limited || retryAfter is not null)))
            {
                if (retryAfter is null && response.Headers.TryGetValues("X-RateLimit-Reset", out var resets)
                    && long.TryParse(resets.FirstOrDefault(), out long reset))
                    retryAfter = Math.Max(1, reset - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
                throw new PortalException(429, "GitHub rate limit reached.", retryAfter);
            }
            throw response.StatusCode switch
            {
                HttpStatusCode.NotFound => new PortalException(404, "GitHub resource is unavailable or inaccessible."),
                HttpStatusCode.Unauthorized => new PortalException(502, "GitHub rejected server credentials."),
                HttpStatusCode.Forbidden => new PortalException(403, "GitHub denied the operation."),
                HttpStatusCode.Conflict => new PortalException(409, "GitHub reported a conflict. Refresh the PR and check its status."),
                HttpStatusCode.UnprocessableEntity => new PortalException(422, "GitHub rejected the requested operation."),
                _ => new PortalException(502, "GitHub could not complete the operation.")
            };
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        bool next = response.Headers.TryGetValues("Link", out var links) && links.Any(link => link.Contains("rel=\"next\"", StringComparison.Ordinal));
        return (doc.RootElement.Clone(), next);
    }

    private static string? Text(JsonElement json, string property)
        => json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int? Number(JsonElement json, string property)
        => json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;

    private static Repository MapRepository(JsonElement json)
    {
        bool isPrivate = json.GetProperty("private").GetBoolean();
        return new(Text(json, "name")!, Text(json, "full_name")!, isPrivate, Text(json, "description"),
            json.GetProperty("owner"), Text(json, "visibility") ?? (isPrivate ? "private" : "public"),
            Text(json, "default_branch")!, Text(json, "language"), json.GetProperty("created_at").GetDateTime(),
            json.GetProperty("updated_at").GetDateTime(), Text(json, "html_url")!, json);
    }

    private static Pull MapPull(JsonElement json)
    {
        var head = json.GetProperty("head");
        var @base = json.GetProperty("base");
        return new(Number(json, "number")!.Value, Text(json, "title")!, Text(json, "state")!, Text(json, "html_url")!,
            Text(head, "sha")!, json.GetProperty("created_at").GetDateTime(), Text(json, "body"), json.GetProperty("user"),
            Text(head, "ref")!, Text(@base, "ref")!, Text(@base, "sha")!, json.GetProperty("updated_at").GetDateTime(),
            json.TryGetProperty("merged", out var merged) && merged.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? merged.GetBoolean() : null,
            Number(json, "commits"), Number(json, "changed_files"), Number(json, "additions"), Number(json, "deletions"), json);
    }

    private static ChangedFile MapFile(JsonElement json)
    {
        string? patch = Text(json, "patch");
        int additions = Number(json, "additions")!.Value;
        int deletions = Number(json, "deletions")!.Value;
        bool binaryMarker = patch?.StartsWith("Binary files ", StringComparison.Ordinal) == true
            || patch?.StartsWith("GIT binary patch", StringComparison.Ordinal) == true;
        var parsed = UnifiedDiffParser.Parse(binaryMarker ? null : patch, additions, deletions);
        return new(Text(json, "filename")!, Text(json, "previous_filename"), Text(json, "status")!, additions, deletions,
            Number(json, "changes")!.Value, Text(json, "sha")!, patch, Text(json, "blob_url"), Text(json, "raw_url"),
            Text(json, "contents_url"), binaryMarker ? true : parsed.Hunks.Count > 0 ? false : null,
            binaryMarker ? "Binary" : string.IsNullOrEmpty(patch) ? "Unavailable" : "Available", parsed.Completeness, parsed.Hunks);
    }
}
