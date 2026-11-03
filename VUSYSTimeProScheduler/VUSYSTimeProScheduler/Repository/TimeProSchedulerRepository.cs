using CommonLib;
using VUSYSTimeProScheduler.Service;

namespace VUSYSTimeProScheduler.Repository
{
    public class TimeProSchedulerRepository: ITimeProSchedulerRepository
    {
        string TimesheetReminderJobName = CommonHelper.EMPLOLYEE_TIMESHEET_REMINDER_SCHEDULER_NOTIFICATION_JOB;
        string EmployeeLeaveHolidayTimesheetSyncJobName = CommonHelper.EMPLOLYEE_LEAVE_AND_HOLIDAY_TIMESHEET_SYNC_SCHEDULER_JOB;


        /// <summary>
        /// This Method Is Used To Get Employee Timesheet Not Filled List and Send Email Notification.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetEmployeesTimesheetNotFilledListAndSendEmailNotification()
        {
            string ReturnMessage = string.Empty;
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri(CommonHelper.KeyOption.ApiBaseUrl) };
                http.Timeout = Timeout.InfiniteTimeSpan;
                var AccessTokenInfo = await TimeProAPIService.TimeProAPIAuthenticationAsync(http, CommonHelper.KeyOption.LoginID, AESEncrytDecry.DecryptAES256(CommonHelper.KeyOption.Password, Helper.EncryptDecryptKey));
                if (!string.IsNullOrWhiteSpace(AccessTokenInfo.Token))
                {
                   
                    var Details = await TimeProAPIService.GetEmployeesTimesheetNotFilledListAndSendEmailNotification(http, AccessTokenInfo.Token, perRequestTimeout: TimeSpan.FromMinutes(60));
                    ReturnMessage = Details;
                }
                else
                {
                    ReturnMessage = $"Failed To Authenticate TimePro API Service !!!";
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteErrorLog(TimesheetReminderJobName, "VUSYSTimeProScheduler.Repository", "LogTimesheetNotificationRepository.GetEmployeesTimesheetNotFilledListAndSendEmailNotification()", ex.Message);
                throw;
            }
            return ReturnMessage;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Leave Holiday and Sync To Project Timesheet.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet()
        {
            string ReturnMessage = string.Empty;
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri(CommonHelper.KeyOption.ApiBaseUrl) };
                http.Timeout = Timeout.InfiniteTimeSpan;
                var AccessTokenInfo = await TimeProAPIService.TimeProAPIAuthenticationAsync(http, CommonHelper.KeyOption.LoginID, AESEncrytDecry.DecryptAES256(CommonHelper.KeyOption.Password, Helper.EncryptDecryptKey));
                if (!string.IsNullOrWhiteSpace(AccessTokenInfo.Token))
                {

                    var Details = await TimeProAPIService.GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet(http, AccessTokenInfo.Token, perRequestTimeout: TimeSpan.FromMinutes(60));
                    ReturnMessage = Details;
                }
                else
                {
                    ReturnMessage = $"Failed To Authenticate TimePro API Service !!!";
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteErrorLog(EmployeeLeaveHolidayTimesheetSyncJobName, "VUSYSTimeProScheduler.Repository", "LogTimesheetNotificationRepository.GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet()", ex.Message);
                throw;
            }
            return ReturnMessage;
        }
    }
}
