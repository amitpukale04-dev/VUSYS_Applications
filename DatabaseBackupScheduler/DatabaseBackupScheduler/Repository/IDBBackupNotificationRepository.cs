namespace DatabaseBackupScheduler.Repository
{
    public interface IDBBackupNotificationRepository
    {
        /// <summary>
        /// This Method Is Used To Take the Backup of the Database.
        /// </summary>
        /// <returns></returns>
        Task<string> BackUpDatabase();
    }
}
