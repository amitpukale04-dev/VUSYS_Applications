namespace VUSYSTimeProWebAPI.Application.Abstractions.Persistence
{
    public interface IDbConnectionHelper
    {
        /// <summary>
        /// This Method Is Used To Get DB Connection String.
        /// </summary>
        /// <param name="DBName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetDBConnectionStringAsync(string DBName, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Decrypted Connection String.
        /// </summary>
        /// <param name="EncryptedConnectionString"></param>
        /// <returns></returns>
        string GetDBDecryptedConnectionString(string EncryptedConnectionString);
    }
}