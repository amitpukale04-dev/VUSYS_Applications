using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using VUSYSTimeProWebAPI.Application.Common.Options;
using static CommonLibCore10.CONST;
using ApplicationSendEmailRequestParam = VUSYSTimeProWebAPI.Application.Models.SendEmailRequestParam;

namespace VUSYSTimeProWebAPI.Infrastructure.Common
{
    public static class CommonHelper
    {
        private const string CommonHelperLogClassName = "CommonHelper";

        /// <summary>
        /// Application configuration values.
        /// </summary>
        public static MyApplicationKeysOptions KeyOption
        {
            get;
            private set;
        } = new();

        /// <summary>
        /// Gemini AI Key configuration values.
        /// </summary>
        public static GeminiAIKeysOptions GeminiAIKeyOption
        {
            get;
            private set;
        } = new();

        static CommonHelper()
        {
            Helper.EncryptDecryptKey = "VUSYSTimePro@123456$Key@1912$@^!";
            Helper.DefaultValue = "VUSYSTimeProWebApp@123456$";
            Helper.AllowAPIURL = new string[] { "https://localhost:44353", "https://timepro-webapi.vusys.in:9091", "https://timepro-dev-webapi.vusys.in:9092" };
            Helper.AllowCorsURL = new string[] { "http://localhost:5173", "https://timepro.vusys.in:9090", "https://dev-timepro.vusys.in:8089" };
        }

        /// <summary>
        /// EMAIL_DISPLAY_NAME
        /// </summary>
        public static readonly string EMAIL_DISPLAY_NAME = "Time-Pro Notification";

        /// <summary>
        /// TIMESHEET CLOSED
        /// </summary>
        public static string TIMESHEET_CLOSED = "You Cannot Add The Timesheet. Since Timesheet is Closed for This Month !!!";

        /// <summary>
        /// DROPDOWN_LIST_RESPONSE
        /// </summary>
        public static string DROPDOWN_LIST_RESPONSE = "DROPDOWN_LIST_RESPONSE";

        /// <summary>
		/// SMTP_EMAIL_SOURCE_PURPOSE
		/// </summary>
		public static readonly string[] SMTP_EMAIL_SOURCE_PURPOSE = { "General" };

        /// <summary>
        /// Initialises application configuration used by static helpers.
        /// </summary>
        public static void Initialise(MyApplicationKeysOptions applicationKeysOptions)
        {
            KeyOption = applicationKeysOptions ?? throw new ArgumentNullException(nameof(applicationKeysOptions));
        }

        /// <summary>
        /// Initialises Gemini AI Key configuration used by static helpers.
        /// </summary>
        public static void GeminiAIInitialise(GeminiAIKeysOptions applicationKeysOptions)
        {
            GeminiAIKeyOption = applicationKeysOptions ?? throw new ArgumentNullException(nameof(applicationKeysOptions));
        }

        /// <summary>
        /// This Method Is Used To Check Whether Directory Exists or Not.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <param name="FolderName"></param>
        /// <returns></returns>
        public static bool CheckDirectoryExists(IHostEnvironment hostEnvironment, string FolderName)
        {
            bool IsExists = false;
            try
            {
                string strPath = Path.Combine(hostEnvironment.ContentRootPath, FolderName);
                if (!Directory.Exists(strPath))
                {
                    Directory.CreateDirectory(strPath);
                    IsExists = true;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{CommonHelperLogClassName}.CheckDirectoryExists()", ex.Message, false);
                throw;
            }
            return IsExists;
        }

        /// <summary>
        /// This Method Is Used To Get Actual Path.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <param name="FolderName"></param>
        /// <returns></returns>
        public static string GetActualPath(IHostEnvironment hostEnvironment, string FolderName)
        {
            CheckDirectoryExists(hostEnvironment, FolderName);
            return Path.Combine(hostEnvironment.ContentRootPath, FolderName);
        }

        /// <summary>
        /// This Method Is Used To Read HTML Template Using StreamReader Async.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<string> ReadHTMLTemplateUsingStreamReaderAsync(IHostEnvironment hostEnvironment, string fileName)
        {
            string templateFolderPath = GetActualPath(hostEnvironment, "Templates");
            string templateFilePath = Path.Combine(templateFolderPath, fileName);

            using StreamReader rdrEmailBody = new StreamReader(templateFilePath);
            return await rdrEmailBody.ReadToEndAsync();
        }

        /// <summary>
        /// This Method Is Used To Create Send Email Request Parameters.
        /// </summary>
        /// <param name="FromDisplayName"></param>
        /// <param name="ToEmail"></param>
        /// <param name="CCEmail"></param>
        /// <param name="BCCEmail"></param>
        /// <param name="ReplyToEmails"></param>
        /// <param name="ReplyToDisplayNames"></param>
        /// <param name="EmailSubject"></param>
        /// <param name="EmailBody"></param>
        /// <returns></returns>
        public static ApplicationSendEmailRequestParam CreateSendEmailRequestParam(string FromDisplayName, string ToEmail, string CCEmail, string BCCEmail, string ReplyToEmails, string ReplyToDisplayNames, string EmailSubject, string EmailBody)
        {
            return new ApplicationSendEmailRequestParam
            {
                FromDisplayName = FromDisplayName,
                ToEmail = ToEmail,
                CCEmail = CCEmail,
                BCCEmail = BCCEmail,
                ReplyToEmails = ReplyToEmails,
                ReplyToDisplayNames = ReplyToDisplayNames,
                EmailSubject = EmailSubject,
                EmailBody = EmailBody
            };
        }

        /// <summary>
        /// This Method Is Used To Convert Image To Bytes From Image URL.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<byte[]?> ConvertToBytesFromImageURLAsync(HttpClient httpClient, Uri uri, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(httpClient);
                ArgumentNullException.ThrowIfNull(uri);
                return await httpClient.GetByteArrayAsync(uri, cancellationToken);
            }
            catch (OperationCanceledException)when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{CommonHelperLogClassName}.ConvertToBytesFromImageURLAsync()", ex.Message, false);
                return null;
            }
        }

        /// <summary>
        /// This Method Is Used To Generate Password.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateNewPassword(int length = 10)
        {
            string NewPassword = string.Empty;
            try
            {
                // Define the character sets for each requirement
                const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
                const string digits = "0123456789";
                const string specialChars = "@#$";

                // Ensure the password has at least one character from each set
                Random random = new Random();
                StringBuilder password = new StringBuilder();

                password.Append(upperCase[random.Next(upperCase.Length)]);
                password.Append(lowerCase[random.Next(lowerCase.Length)]);
                password.Append(digits[random.Next(digits.Length)]);
                password.Append(specialChars[random.Next(specialChars.Length)]);

                // Fill the rest of the password with random characters from all sets
                string allCharacters = upperCase + lowerCase + digits + specialChars;
                for (int i = password.Length; i < length; i++)
                {
                    password.Append(allCharacters[random.Next(allCharacters.Length)]);
                }

                // Shuffle the password to avoid predictable patterns
                NewPassword = new string(password.ToString().OrderBy(c => random.Next()).ToArray());
            }
            catch (Exception ex)
            {
                NewPassword = string.Empty;
                throw;
            }
            return NewPassword;
        }

        /// <summary>
        /// This Method is Used To Sanitize File Name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "file";
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
                else sb.Append('_');
            }
            var cleaned = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(cleaned) ? "file" : cleaned;
        }

        /// <summary>
        /// This Method Is Used To Save Save File To Folder.
        /// </summary>
        /// <param name="fileBase64"></param>
        /// <param name="fileType"></param>
        /// <param name="webHostEnvironment"></param>
        /// <returns></returns>
        public static string SaveFileToFolder(string fileBase64, FileTypes fileType, IWebHostEnvironment webHostEnvironment)
        {
            string ReturnFullPath = string.Empty;
            try
            {
                string tempFolderPath = GetActualPath(webHostEnvironment, "TempPDFFiles");
                string extension = FileHelper.GetFileExtension(fileType);
                string fileName = $"Merged_{DateTime.Now.ToString(DateTimeFormatConstants.YYYYMMDD_HHMMSS_COMPACT_DATETIMEFORMAT)}{extension}";
                var fullPath = tempFolderPath + "\\" + fileName;
                var bytes = Convert.FromBase64String(CleanBase64(fileBase64));
                File.WriteAllBytes(fullPath, bytes);
                ReturnFullPath = fullPath;
            }
            catch (Exception ex)
            {
                ReturnFullPath = string.Empty;
                Helper.LogMessage(KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{CommonHelperLogClassName}.SaveFileToFolder()", ex.Message, false);
                throw;
            }
            return ReturnFullPath;
        }


        /// <summary>
        /// This Method Is Used To Clean Base64.
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static string CleanBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return string.Empty;
            var idx = base64.IndexOf(',');
            return idx >= 0 ? base64[(idx + 1)..].Trim() : base64.Trim();
        }

        /// <summary>
        /// This Method Is Used To Encode HTML.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string HtmlEncode(string value)
        {
            return WebUtility.HtmlEncode(value ?? "");
        }

        /// <summary>
        /// This Method Is Used To Fetch Attachments From DataSet.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static Task<List<Attachment>> FetchAttachmentsFromDataSet(DataSet ds)
        {
            List<Attachment> fileAttachments = new List<Attachment>();
            try
            {
                if (ds == null || ds.Tables.Count == 0)
                    return Task.FromResult(new List<Attachment>());

                var base64Files = new List<FileHelper.Base64File>();

                foreach (DataTable dt in ds.Tables)
                {
                    if (!CheckDataTableHasColumn(dt, "Status") ||
                        !CheckDataTableHasColumn(dt, "FileName") ||
                        !CheckDataTableHasColumn(dt, "Base64"))
                    {
                        continue;
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        var status = row["Status"]?.ToString()?.Trim();
                        if (!string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var base64 = row["Base64"]?.ToString()?.Trim();
                        if (string.IsNullOrWhiteSpace(base64))
                            continue;

                        var fileName = row["FileName"]?.ToString()?.Trim() ?? "file";

                        // Remove ONLY the first underscore if present
                        var idx = fileName.IndexOf('_');
                        if (idx >= 0)
                            fileName = fileName.Remove(idx, 1);

                        base64Files.Add(
                            new FileHelper.Base64File(
                                base64,
                                SuggestedName: fileName
                            )
                        );
                    }
                }

                var attachments = FileHelper.BuildAttachmentsFromBase64(base64Files);
                fileAttachments = attachments;
            }
            catch (Exception ex)
            {
                fileAttachments = null;
                Helper.LogMessage(KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{CommonHelperLogClassName}.FetchAttachmentsFromDataSet()", ex.Message, false);
            }
            return Task.FromResult(fileAttachments);
        }

        /// <summary>
        /// This Method Is Used To Check DataTable Has Specific Column.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private static bool CheckDataTableHasColumn(DataTable table, string columnName)
        {
            return table.Columns
                .Cast<DataColumn>()
                .Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// This Method Is Used To Convert Numbered Text To HTML List.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="skipItems"></param>
        /// <returns></returns>
        public static string ConvertNumberedTextToHtmlList(string input, int skipItems = 0)
        {
            string ReturnMessage = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    ReturnMessage = "<ol></ol>";
                    return ReturnMessage;
                }
                else
                {
                    // Normalize format: replace commas before numbers with a separator
                    string normalized = input.Replace(",1.", "1.");

                    // Split on numbered patterns like "1. ", "2. ", etc.
                    var items = System.Text.RegularExpressions.Regex
                        .Split(normalized, @"(?=\d+\.\s)")
                        .Where(i => !string.IsNullOrWhiteSpace(i))
                        .Skip(skipItems) // Skip first N items if specified
                        .Select(i =>
                        {
                            int dotIndex = i.IndexOf('.');
                            string content = dotIndex >= 0 ? i.Substring(dotIndex + 1).Trim() : i.Trim();
                            return $"<li>&nbsp;{content}</li>";
                        });
                    ReturnMessage = "<ol>" + string.Join("", items) + "</ol>";
                }
            }
            catch (Exception ex)
            {
                ReturnMessage = string.Empty;
                Helper.LogMessage(KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{CommonHelperLogClassName}.ConvertNumberedTextToHtmlList()", ex.Message, false);
            }
            return ReturnMessage;
        }

        /// <summary>
        /// This Method is used to process comma-separated values and add ampersand between the last two values
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string AddAmpersandToLast(string input)
        {
            string ReturnResult = string.Empty;
            try
            {
                // Split the string by commas and trim any leading/trailing spaces
                var parts = input.Split(',')
                                 .Select(part => part.Trim())
                                 .Distinct()
                                 .ToList();

                // If there are more than one part, add an "&" between the last two parts
                if (parts.Count > 1)
                {
                    parts[parts.Count - 2] += " & " + parts.Last();  // Append "&" to the second-last part
                    parts.RemoveAt(parts.Count - 1);  // Remove the last part since it's now appended
                }
                ReturnResult = string.Join(",", parts);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureCommonLogNamespace, $"{CommonHelperLogClassName}.AddAmpersandToLast()", ex.Message, false);
            }
            return ReturnResult;
        }
    }
}