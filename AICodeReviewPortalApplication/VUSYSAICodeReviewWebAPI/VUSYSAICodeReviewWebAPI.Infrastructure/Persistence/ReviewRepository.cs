using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Persistence;
using VUSYSAICodeReviewWebAPI.Domain;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Infrastructure.Persistence.Connections;

namespace VUSYSAICodeReviewWebAPI.Infrastructure.Persistence;

public sealed class ReviewRepository(DbConnectionHelper connections) : IReviewRepository
{
    public async Task<ReviewResult> Save(ReviewResult review, CancellationToken ct)
        => review with { Id = await Insert(review, null, ct) };

    public async Task<long> ClaimManual(ReviewResult review, byte[] fingerprint, CancellationToken ct)
    {
        try
        {
            return await Insert(review, fingerprint, ct);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new PortalException(409,
                "This manual review already has a publication record. Check the review history and GitHub before resubmitting.");
        }
    }

    private async Task<long> Insert(ReviewResult review, byte[]? fingerprint, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = Procedure(connection, "Insert");
        Add(command, "@Repository", SqlDbType.NVarChar, review.Repository, 201);
        Add(command, "@PullNumber", SqlDbType.Int, review.PullNumber);
        Add(command, "@Title", SqlDbType.NVarChar, review.Title, -1);
        Add(command, "@CommitSha", SqlDbType.VarChar, review.CommitSha, 64);
        Add(command, "@CreatedUtc", SqlDbType.DateTime2, review.CreatedUtc);
        Add(command, "@Summary", SqlDbType.NVarChar, review.Summary, -1);
        using var findings = new DataTable();
        findings.Columns.Add("FindingOrder", typeof(int));
        findings.Columns.Add("Severity", typeof(string));
        findings.Columns.Add("FindingJson", typeof(string));
        for (int index = 0; index < review.Findings.Count; index++)
        {
            var finding = review.Findings[index];
            findings.Rows.Add(index, finding.Severity, JsonSerializer.Serialize(finding));
        }
        AddTable(command, "@Findings", "dbo.VUSYS_CodeReviewFindingRows", findings);
        Add(command, "@Coverage", SqlDbType.NVarChar, JsonSerializer.Serialize(review.Coverage), -1);
        if (fingerprint is not null)
            Add(command, "@ManualFingerprint", SqlDbType.Binary, fingerprint, 32);
        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<ReviewResult?> Get(long id, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = Procedure(connection, "Get");
        Add(command, "@Id", SqlDbType.BigInt, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<List<ReviewResult>> List(string[] repositories, int? number, string? severity, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = Procedure(connection, "List");
        using var names = new DataTable();
        names.Columns.Add("Repository", typeof(string));
        foreach (string repository in repositories)
            names.Rows.Add(repository);
        AddTable(command, "@Repositories", "dbo.VUSYS_CodeReviewRepositoryNames", names);
        Add(command, "@PullNumber", SqlDbType.Int, number);
        Add(command, "@Severity", SqlDbType.NVarChar, severity, 20);
        Add(command, "@Page", SqlDbType.Int, page);
        Add(command, "@PageSize", SqlDbType.Int, pageSize);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var reviews = new List<ReviewResult>();
        while (await reader.ReadAsync(ct))
            reviews.Add(Map(reader));
        return reviews;
    }

    public async Task<bool> Reserve(long id, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = Procedure(connection, "ReservePublication");
        Add(command, "@Id", SqlDbType.BigInt, id);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    public async Task Complete(long id, long gitHubReviewId, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = Procedure(connection, "CompletePublication");
        Add(command, "@Id", SqlDbType.BigInt, id);
        Add(command, "@GitHubReviewId", SqlDbType.BigInt, gitHubReviewId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static SqlCommand Procedure(SqlConnection connection, string operation)
        => new($"dbo.usp_VUSYS_CodeReview_{operation}", connection) { CommandType = CommandType.StoredProcedure };

    private static void Add(SqlCommand command, string name, SqlDbType type, object? value, int size = 0)
    {
        var parameter = size == 0 ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size);
        parameter.Value = value ?? DBNull.Value;
    }

    private static void AddTable(SqlCommand command, string name, string typeName, DataTable table)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Structured);
        parameter.TypeName = typeName;
        parameter.Value = table;
    }

    private static ReviewResult Map(SqlDataReader reader)
    {
        string status = reader.GetString(7);
        return new(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3), reader.GetString(4),
            DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
            JsonSerializer.Deserialize<List<Finding>>(reader.GetString(6)) ?? [], reader.GetString(8),
            status == "Published", status, reader.IsDBNull(9) ? null : reader.GetInt64(9),
            reader.IsDBNull(10) ? null : JsonSerializer.Deserialize<Coverage>(reader.GetString(10)),
            ReviewType: reader.GetString(11));
    }
}
