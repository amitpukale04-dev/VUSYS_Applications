using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VUSYSTimeProScheduler.Models;

namespace VUSYSTimeProScheduler.Service
{
    public static class TimeProAPIService
    {
        /// <summary>
        /// This Method Is Used For TimePro API Authentication.
        /// </summary>
        /// <param name="http"></param>
        /// <param name="LoginID"></param>
        /// <param name="Password"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<(string Token, DateTimeOffset? ExpiresAt)> TimeProAPIAuthenticationAsync(HttpClient http, string LoginID, string Password, CancellationToken ct = default)
        {
            var req = new UserLogin { loginID = LoginID, password = Password };

            using var resp = await http.PostAsJsonAsync("/api/Auth/SchedulerLogin", req, ct);
            resp.EnsureSuccessStatusCode();

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var env = await resp.Content.ReadFromJsonAsync<UserLoginResponse>(opts, ct);

            var item = env?.data?.USER_LOGIN_DETAILS?.FirstOrDefault();
            var token = item?.accessToken;

            if (string.IsNullOrWhiteSpace(token))
                return (null, null);

            var expiresAt = item?.TokenExpiryDateTime ?? DateTimeOffset.UtcNow.AddMinutes(item!.TokenExpirationInMinutes);

            return (token, expiresAt);
        }

        /// <summary>
        /// This Method Is Used To Get Employee Timesheet Not Filled List and Send Email Notification.
        /// </summary>
        /// <param name="http"></param>
        /// <param name="accessToken"></param>
        /// <param name="perRequestTimeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> GetEmployeesTimesheetNotFilledListAndSendEmailNotification(HttpClient http, string accessToken, TimeSpan perRequestTimeout, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(perRequestTimeout);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Timesheet/GetEmployeesTimesheetNotFilledListAndSendEmailNotification");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var resp = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            resp.EnsureSuccessStatusCode();

            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = await resp.Content.ReadFromJsonAsync<NotFilledTimesheetResponse>(opts, cts.Token);

            var message = result?.data?.TIMESHEET_NOT_FILLED_RESPONSE?.FirstOrDefault()?.Message;

            return message ?? "No response message found.";
        }

        /// <summary>
        /// This Method Is Used To Get Employee Leave Holiday and Sync To Project Timesheet.
        /// </summary>
        /// <param name="http"></param>
        /// <param name="accessToken"></param>
        /// <param name="perRequestTimeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet(HttpClient http, string accessToken, TimeSpan perRequestTimeout, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(perRequestTimeout);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Timesheet/GetEmployeesLeaveHolidayAndSyncToProjectTimeSheet");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var resp = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            resp.EnsureSuccessStatusCode();

            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = await resp.Content.ReadFromJsonAsync<EmployeeLeaveAndHolidayTimesheetSyncResponse>(opts, cts.Token);

            var message = result?.data?.EMPLOYEE_LEAVE_HOLIDAY_SYNC_PROJECT_TIMESHEET_RESPONSE?.FirstOrDefault()?.Message;

            return message ?? "No response message found.";
        }
    }
}
