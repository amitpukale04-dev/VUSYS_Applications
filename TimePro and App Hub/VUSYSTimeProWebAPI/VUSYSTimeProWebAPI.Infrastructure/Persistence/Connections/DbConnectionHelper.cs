using CommonLibCore10;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using VUSYSTimeProWebAPI.Application.Abstractions.Persistence;
using VUSYSTimeProWebAPI.Application.Models.Configuration;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Persistence.Connections
{
    public sealed class DbConnectionHelper : IDbConnectionHelper
    {
        private readonly IHostEnvironment _hostEnvironment;
        private const string DbConnectionHelperLogClassName = "DbConnectionHelper";

        /// <summary>
        /// Database Connection Helper Constructor.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        public DbConnectionHelper(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        }

        /// <summary>
        /// This Method Is Used To Get Decrypted Connection String.
        /// </summary>
        /// <param name="EncryptedConnectionString"></param>
        /// <returns></returns>
        public string GetDBDecryptedConnectionString(string EncryptedConnectionString)
        {
            string ReturnDBConnectionString = string.Empty;
            try
            {
                var parts = EncryptedConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var dict = parts
                    .Select(p => p.Split('=', 2))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => p[0].Trim(), p => p[1].Trim());


                string Driver = dict.GetValueOrDefault("Driver");
                string CPOOL = dict.GetValueOrDefault("CPOOL");
                string CPTO = dict.GetValueOrDefault("CPTO");
                string MC = dict.GetValueOrDefault("MC");

                // Extract specific keys
                string EncryptedUid = dict.GetValueOrDefault("Uid");
                string EncryptedPwd = dict.GetValueOrDefault("Pwd");
                string EncryptedServer = dict.GetValueOrDefault("Server");
                string EncryptedHost = dict.GetValueOrDefault("Host");
                string EncryptedDatabase = dict.GetValueOrDefault("Database");

                // Decrypting the Values
                string DecryptedUid = AESEncrytDecry.DecryptAES256(EncryptedUid, Helper.EncryptDecryptKey);
                string DecryptedPwd = AESEncrytDecry.DecryptAES256(EncryptedPwd, Helper.EncryptDecryptKey);
                string DecryptedServer = AESEncrytDecry.DecryptAES256(EncryptedServer, Helper.EncryptDecryptKey);
                string DecryptedHost = AESEncrytDecry.DecryptAES256(EncryptedHost, Helper.EncryptDecryptKey);
                string DecryptedDatabase = AESEncrytDecry.DecryptAES256(EncryptedDatabase, Helper.EncryptDecryptKey);

                ReturnDBConnectionString = $"Driver={Driver};Uid={DecryptedUid};Pwd={DecryptedPwd};Server={DecryptedServer}; Host={DecryptedHost}; Database={DecryptedDatabase}; CPOOL={CPOOL}; CPTO={CPTO}; MC={MC};";
            }
            catch (Exception ex)
            {
                ReturnDBConnectionString = string.Empty;
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructurePersistenceConnectionsLogNamespace, $"{DbConnectionHelperLogClassName}.GetDBDecryptedConnectionString()", ex.Message, false);
            }
            return ReturnDBConnectionString;
        }

        /// <summary>
        /// This Method Is Used To Get DB Connection Details.
        /// </summary>
        /// <param name="DBName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetDBConnectionStringAsync(string DBName, CancellationToken cancellationToken = default)
        {
            string ReturnDBConnectionString = string.Empty;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(DBName))
                {
                    return ReturnDBConnectionString;
                }

                IReadOnlyCollection<ConfigureConnectionStrings> connectionDetails = await GetConnectionStringsListAsync(cancellationToken);

                ConfigureConnectionStrings? currentSelectedConnection = connectionDetails
                    .FirstOrDefault(connection => string.Equals(connection.Environment, DBName, StringComparison.OrdinalIgnoreCase));

                if (currentSelectedConnection == null)
                {
                    return ReturnDBConnectionString;
                }

                string connectionStringKey = CommonHelper.KeyOption.ConnectionStringKey;

                if (string.IsNullOrWhiteSpace(connectionStringKey))
                {
                    return ReturnDBConnectionString;
                }

                if (currentSelectedConnection.Connections == null)
                {
                    return ReturnDBConnectionString;
                }
                if (!currentSelectedConnection.Connections.TryGetValue(connectionStringKey, out string? connectionString))
                {
                    return ReturnDBConnectionString;
                }
                ReturnDBConnectionString = connectionString;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructurePersistenceConnectionsLogNamespace, $"{DbConnectionHelperLogClassName}.GetDBConnectionStringAsync()", ex.Message, false);
                ReturnDBConnectionString = string.Empty;
            }
            return ReturnDBConnectionString;
        }

        /// <summary>
        /// This Method Is Used To Get Connection String List.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IReadOnlyCollection<ConfigureConnectionStrings>> GetConnectionStringsListAsync(CancellationToken cancellationToken)
        {
            List<ConfigureConnectionStrings> connectionStrings = new();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string configurationFilesFolderPath =Path.Combine(_hostEnvironment.ContentRootPath,"ConfigurationFiles");

                if (!Directory.Exists(configurationFilesFolderPath))
                {
                    Directory.CreateDirectory(configurationFilesFolderPath);
                }

                string configurationFilePath =Path.Combine(configurationFilesFolderPath,"DBConnectionConfiguration.json");

                if (!File.Exists(configurationFilePath))
                {
                    return connectionStrings;
                }

                string jsonData = await File.ReadAllTextAsync(configurationFilePath,cancellationToken);

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    return connectionStrings;
                }

                ConnectionStringsRoot? rootObject = JsonConvert.DeserializeObject<ConnectionStringsRoot>(jsonData);

                if (rootObject?.ConnectionStrings != null)
                {
                    connectionStrings = rootObject.ConnectionStrings;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructurePersistenceConnectionsLogNamespace, $"{DbConnectionHelperLogClassName}.GetConnectionStringsListAsync()", ex.Message, false);
            }
            return connectionStrings;
        }

        /// <summary>
        /// Decrypts an individual connection string value.
        /// </summary>
        /// <param name="encryptedValue"></param>
        /// <returns></returns>
        private static string DecryptConnectionValue(string encryptedValue)
        {
            if (string.IsNullOrWhiteSpace(encryptedValue))
            {
                return string.Empty;
            }
            return AESEncrytDecry.DecryptAES256(encryptedValue, Helper.EncryptDecryptKey);
        }
    }
}