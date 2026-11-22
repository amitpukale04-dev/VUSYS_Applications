using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Infrastructure.Persistence.Connections;

public sealed class DbConnectionHelper(IConfiguration configuration)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        string? connectionString = configuration.GetConnectionString("CodeReview");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new PortalException(503, "SQL Server connection is missing.");
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
