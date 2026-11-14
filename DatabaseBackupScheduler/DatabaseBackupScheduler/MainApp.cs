using CommonLib;
using DatabaseBackupScheduler.Repository;

namespace DatabaseBackupScheduler
{
    public class MainApp
    {
        private readonly IDBBackupNotificationRepository _dbBackupRepo;

        public MainApp(IDBBackupNotificationRepository DBBackupRepo)
        {
            _dbBackupRepo = DBBackupRepo;
        }

        public async Task Run()
        {
            CommonHelper.SchedulerWriteLog(CommonHelper.KeyOption.NotificationLog, CommonHelper.KeyOption.LogFileName, "DB Backup Scheduler", "Program.Main()", "- Database Backup Scheduler Service Started");
            // Call method in repository
            string ReturnMessage = await _dbBackupRepo.BackUpDatabase();
            CommonHelper.SchedulerWriteLog(CommonHelper.KeyOption.NotificationLog, CommonHelper.KeyOption.LogFileName, "DB Backup Scheduler", "Program.Main()", ReturnMessage);
            CommonHelper.SchedulerWriteLog(CommonHelper.KeyOption.NotificationLog, CommonHelper.KeyOption.LogFileName, "DB Backup Scheduler", "Program.Main()", "- Database Backup Scheduler Service Ended");
        }
    }
}
