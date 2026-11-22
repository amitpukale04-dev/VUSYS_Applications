using System.Security.Cryptography;
using System.Text;
using CommonLib;

namespace VUSYSAppHub.DACHelper
{
    public static class DbConnectionHelper
    {
        /// <summary>
        /// This Method Is Used To Get DB Decrypted Connection String.
        /// </summary>
        /// <param name="EncryptedConnectionString"></param>
        /// <param name="EncryptDecryptKey"></param>
        /// <returns></returns>
        public static string GetDBDecryptedConnectionString(string EncryptedConnectionString, string EncryptDecryptKey)
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

                // Extract specific keys
                string EncryptedUid = dict.GetValueOrDefault("Uid");
                string EncryptedPwd = dict.GetValueOrDefault("Pwd");
                string EncryptedServer = dict.GetValueOrDefault("Server");
                string EncryptedHost = dict.GetValueOrDefault("Host");
                string EncryptedDatabase = dict.GetValueOrDefault("Database");

                // Decrypting the Values
                string DecryptedUid = AESEncrytDecry.DecryptAES256(EncryptedUid, EncryptDecryptKey);
                string DecryptedPwd = AESEncrytDecry.DecryptAES256(EncryptedPwd, EncryptDecryptKey);
                string DecryptedServer = AESEncrytDecry.DecryptAES256(EncryptedServer, EncryptDecryptKey);
                string DecryptedHost = AESEncrytDecry.DecryptAES256(EncryptedHost, EncryptDecryptKey);
                string DecryptedDatabase = AESEncrytDecry.DecryptAES256(EncryptedDatabase, EncryptDecryptKey);

                ReturnDBConnectionString = $"Driver={Driver};Uid={DecryptedUid};Pwd={DecryptedPwd};Server={DecryptedServer}; Host={DecryptedHost}; Database={DecryptedDatabase};";
            }
            catch (Exception ex)
            {
                ReturnDBConnectionString = string.Empty;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.DACHelper", "DbConnectionHelper.GetDBDecryptedConnectionString()", ex.Message, false);
            }
            return ReturnDBConnectionString;
        }
    }
}
