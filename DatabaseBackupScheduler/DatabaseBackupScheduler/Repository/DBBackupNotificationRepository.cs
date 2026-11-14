using System.Data;
using CommonLib;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
namespace DatabaseBackupScheduler.Repository
{
    public class DBBackupNotificationRepository : IDBBackupNotificationRepository
    {
        private readonly IConfiguration _configuration;

        public DBBackupNotificationRepository(IConfiguration configuration) : base()
        {
            _configuration = configuration;
        }

        /// <summary>
        /// This Method Is Used To Take the Backup of the Database.
        /// </summary>
        /// <returns></returns>
        public async Task<string> BackUpDatabase()
        {
            string ReturnMessage = string.Empty;
            DataTable dt = new DataTable();
            try
            {
                string DBName = CommonHelper.KeyOption.DatabaseName;
                string BackupFolderPath = CommonHelper.KeyOption.DBBackupFolderPath;
                string connectionString = _configuration.GetConnectionString(CommonHelper.KeyOption.ConnectionStringKey);
                if (!Directory.Exists(BackupFolderPath))
                {
                    Directory.CreateDirectory(BackupFolderPath);
                }

                string backupFileName = CommonHelper.KeyOption.DBBackupFileName;
                string backupFilePath = Path.Combine(BackupFolderPath, backupFileName);

                string tempOldBackupPath = Path.Combine(BackupFolderPath, $"{DBName}_old.bak");

                if (File.Exists(backupFilePath))
                {
                    if (File.Exists(tempOldBackupPath))
                        File.Delete(tempOldBackupPath); // Clean up any previous temp file

                    File.Move(backupFilePath, tempOldBackupPath);
                }

                string sql = $@"BACKUP DATABASE [{DBName}] TO DISK = N'{backupFilePath}' WITH FORMAT, INIT, NAME = N'{DBName} - Full Backup';";

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
                //ReturnMessage = $"{DBName}: Backup Successful: {backupFilePath} !!!";

                if (File.Exists(backupFilePath))
                {
                    if (File.Exists(tempOldBackupPath))
                        File.Delete(tempOldBackupPath);

                    ReturnMessage = $"{DBName}: ✅ Backup successful to {backupFilePath}";
                }
                else
                {
                    // File not created even though SQL ran
                    ReturnMessage = $"{DBName}: ❌ Backup failed — file was not created.";
                }
            }
            catch (Exception ex)
            {
                ReturnMessage = $"Backup Failed: {ex.Message} !!!";
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "DatabaseBackupScheduler.Repository", "LogTimesheetNotificationRepository.GetEmployeesTimesheetNotFilledListAndSendEmailNotification()", ex.Message, false);
                throw;
            }
            return ReturnMessage;
        }
    }
}
