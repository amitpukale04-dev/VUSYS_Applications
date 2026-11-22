namespace VUSYSTimeProWebAPI.Application.Abstractions.Persistence
{
    public interface IDatabaseWarmUpService
    {
        /// <summary>
        /// This Method Is Used To Warm Up Database Connection.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WarmUpDatabaseConnectionAsync(CancellationToken cancellationToken = default);
    }
}
