using CommonLibCore10;
using VUSYSTimeProWebAPI.Application.Abstractions.Persistence;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Persistence.Connections;

public sealed class DatabaseWarmUpService : IDatabaseWarmUpService
{
    private readonly IDbConnectionHelper _dbConnectionHelper;
   
    /// <summary>
    /// Database WarmUp Service Constructor.
    /// </summary>
    /// <param name="dbConnectionHelper"></param>
    public DatabaseWarmUpService(IDbConnectionHelper dbConnectionHelper)
    {
        _dbConnectionHelper = dbConnectionHelper ?? throw new ArgumentNullException(nameof(dbConnectionHelper));
    }

    /// <summary>
    /// This Method Is Used To Warm Up Database Connection.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task WarmUpDatabaseConnectionAsync(CancellationToken cancellationToken = default)
    {
        string decryptedConnectionString = await _dbConnectionHelper.GetDBConnectionStringAsync("DevOrProd");

        if (string.IsNullOrWhiteSpace(decryptedConnectionString))
        {
            throw new InvalidOperationException("The database connection string could not be loaded or decrypted.");
        }
        await DatabaseWarmUp.WarmUpSQLServerDatabaseConnectionAsync(decryptedConnectionString, Convert.ToBoolean(CommonHelper.KeyOption.IsDataBaseWarmUpLogEnabled), CommonHelper.KeyOption.DataBaseWarmUpLog, cancellationToken);
    }
}