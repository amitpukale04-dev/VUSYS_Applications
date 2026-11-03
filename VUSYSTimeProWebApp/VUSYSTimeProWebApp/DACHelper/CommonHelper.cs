using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VUSYSTimeProWebApp.Models;
using HtmlAgilityPack;
using CommonLib;

namespace VUSYSTimeProWebApp.DACHelper
{
    public static class CommonHelper
    {
        static CommonHelper()
        {
            Helper.EncryptDecryptKey = "VUSYSTimePro@123456$Key@1912$@^!";   
            Helper.DefaultValue = "VUSYSTimeProWebApp@123456$";
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
        /// Key Option
        /// </summary>
        public static MyApplicationKeysOptions KeyOption { get; set; } = new MyApplicationKeysOptions();


        #region [Session Methods]

        /// <summary>
        /// This Method Is Used To Set Json Session Object.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="SessionKey"></param>
        /// <param name="value"></param>
        public static void SetJsonSessionObject(this ISession session, string SessionKey, object value)
        {
            session.SetString(SessionKey, JsonConvert.SerializeObject(value));
        }

        /// <summary>
        /// This Method Is Used To Get Object From Json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="SessionKey"></param>
        /// <returns></returns>
        public static T GetObjectFromJson<T>(this ISession session, string SessionKey)
        {
            var value = session.GetString(SessionKey);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// This Method Is Used To Clear and Remove Session Object.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="SessionKey"></param>
        public static void ClearAndRemoveSessionObject(this ISession session, string SessionKey)
        {
            session.Clear();
            session.Remove(SessionKey);
        }

        /// <summary>
        /// This Method Is Used To Remove Session Object.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="SessionKey"></param>
        public static void RemoveSessionObject(this ISession session, string SessionKey)
        {
            session.Remove(SessionKey);
        }


        #endregion [Session Methods]

        #region [Permission Methods]

        /// <summary>
        /// This Method Is Used To Check Page Permission.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ControllerName"></param>
        /// <param name="ActionName"></param>
        /// <returns></returns>
        public static bool CheckPagePermission(this ISession session, string ControllerName, string ActionName)
        {
            bool IsAuthorize = false;
            try
            {
                var SubMenuSessionDetails = GetObjectFromJson<List<MenusM>>(session, "SubMenuListSession");
                List<MenusM> AllSubMenuList = SubMenuSessionDetails.ToList();
                IsAuthorize = AllSubMenuList.Exists(c => c.Controller == "/" + ControllerName && c.Action == ActionName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.CheckPagePermission()", ex.Message, false);
                throw;
            }
            return IsAuthorize;
        }

        /// <summary>
        /// This Method Is Used To Check Page Access.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ControllerContext"></param>
        /// <returns></returns>
        public static bool CheckPageAccess(this ISession session, ControllerContext ControllerContext)
        {
            bool IsAuthorize = false;
            try
            {
                string actionName = ControllerContext.RouteData.Values["action"].ToString();
                string controllerName = ControllerContext.RouteData.Values["controller"].ToString();
                IsAuthorize = CheckPagePermission(session, controllerName, actionName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.CheckPageAccess()", ex.Message, false);
                throw;
            }
            return IsAuthorize;
        }

        #endregion [Permission Methods]

        #region [Custom Methods]

        /// <summary>
        /// This Method is Used to Get Base URL.
        /// </summary>
        /// <returns></returns>
        public static string GetBaseURL(IHttpContextAccessor httpContextAccessor)
        {
            string strBaseURL = string.Empty;
            try
            {
                var Request = httpContextAccessor.HttpContext.Request;
                strBaseURL = $"{Request.Scheme}://{Request.Host}";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.GetBaseURL()", ex.Message, false);
                throw;
            }
            return strBaseURL;
        }

        /// <summary>
        /// This Method Is Used To Check Whether Directory Exists or Not.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="FolderName"></param>
        /// <returns></returns>
        public static bool CheckDirectoryExists(IWebHostEnvironment webHostEnvironment, string FolderName)
        {
            bool IsExists = false;
            try
            {
                string strPath = Path.Combine(webHostEnvironment.ContentRootPath, FolderName);
                if (!Directory.Exists(strPath))
                {
                    Directory.CreateDirectory(strPath);
                    IsExists = true;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.CheckDirectoryExists()", ex.Message, false);
                throw;
            }
            return IsExists;
        }

        /// <summary>
        /// This Method Is Used To Convert Image To Bytes.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static byte[] ConvertToBytes(IFormFile image)
        {
            byte[] CoverImageBytes = null;
            BinaryReader reader = new BinaryReader(image.OpenReadStream());
            CoverImageBytes = reader.ReadBytes((int)image.Length);
            return CoverImageBytes;
        }

        /// <summary>
        /// This Method Is Used To Convert Image To Bytes From Image URL.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static async Task<byte[]> ConvertToBytesFromImageURL(Uri uri)
        {
            byte[] ConvertImageBytes;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                        delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                        {
                            return true;
                        };
                using (HttpClient client = new HttpClient())
                {
                    ConvertImageBytes = await client.GetByteArrayAsync(uri);
                }
            }
            catch (Exception ex)
            {
                ConvertImageBytes = null;
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.ConvertToBytesFromImageURL()", ex.Message, false);
            }
            return ConvertImageBytes;
        }

        /// <summary>
        /// This Method Is Used To Get Actual Path.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="FolderName"></param>
        /// <returns></returns>
        public static string GetActualPath(IWebHostEnvironment webHostEnvironment, string FolderName)
        {
            CheckDirectoryExists(webHostEnvironment, FolderName);
            return Path.Combine(webHostEnvironment.ContentRootPath, FolderName);
        }

        /// <summary>
        /// This Method Is Used To Generate Error Message.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Message"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public static DataSet GenerateErrorMessage(int ID, string Message, string TableName)
        {
            DataSet dataSet = new DataSet();
            try
            {
                DataTable dtMessage = new DataTable();
                dtMessage.Columns.Add("Id");
                dtMessage.Columns.Add("Message");
                DataRow drMessage = null;
                drMessage = dtMessage.NewRow();
                drMessage["Id"] = ID;
                drMessage["Message"] = Message;
                dtMessage.Rows.Add(drMessage);
                dataSet = Helper.ConvertDataTableToDataSet(dtMessage.Copy());
                dataSet.Tables["Table1"].TableName = TableName;
            }
            catch (Exception ex)
            {
                dataSet = null;
            }
            return dataSet;
        }

        /// <summary>
        /// This Method Is Used To Get Email Configuration Settings.
        /// </summary>
        /// <returns></returns>
        public static DataTable GetEmailConfigurationSettings(AppDbContext db)
        {
            DataTable dt = new DataTable();
            try
            {
                var EmailConfigurationList = (from c in db.ConfigureEmailList
                                              where c.IsDeleted == false
                                              select new { c.Id, c.FromEmail, Password = AESEncrytDecry.DecryptAES256(c.Password, Helper.EncryptDecryptKey), c.SmtpHost, c.PortNo }).ToList();

                if (EmailConfigurationList != null && EmailConfigurationList.Count > 0)
                {
                    dt = Helper.ConvertListToDataTable(EmailConfigurationList);
                }
            }
            catch (Exception ex)
            {
                dt = null;
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.GetEmailConfigurationSettings()", ex.Message, false);
            }
            return dt;
        }

        /// <summary>
        /// This Method Is Used To Generate Common Error.
        /// </summary>
        /// <param name="ImageName"></param>
        /// <param name="ControllerName"></param>
        /// <param name="ActionName"></param>
        /// <param name="LinkText"></param>
        /// <param name="ErrorMessage"></param>
        /// <param name="IsShowFooter"></param>
        /// <returns></returns>
        public static CommonError GenerateCommonError(string ImageName, string ControllerName, string ActionName, string LinkText, string ErrorMessage, bool IsShowFooter)
        {
            CommonError _objCommonError = new CommonError();
            try
            {
                _objCommonError.ImageName = ImageName;
                _objCommonError.ControllerName = ControllerName;
                _objCommonError.ActionName = ActionName;
                _objCommonError.LinkText = LinkText;
                _objCommonError.ErrorMessage = ErrorMessage;
                _objCommonError.IsShowFooter = IsShowFooter;
            }
            catch(Exception ex)
            {
                _objCommonError = null;
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.GenerateCommonError()", ex.Message, false);
            }
            return _objCommonError;
        }

        /// <summary>
        /// This Method Is Used To Get User Session Details.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static UsersM GetUserSessionDetails(ISession session)
        {
            UsersM _objUsersM = new UsersM();
            try
            {
                _objUsersM = GetObjectFromJson<UsersM>(session, "UserSession");
            }
            catch (Exception ex)
            {
                _objUsersM = null;
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.GetUserSessionDetails()", ex.Message, false);
            }
            return _objUsersM;
        }

        /// <summary>
        /// This Method Is Used To Get Formatted String From Raw HTML.
        /// </summary>
        /// <param name="Description"></param>
        /// <returns></returns>
        public static string GetFormattedStringFromRawHTML(string Description)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                // Load the HTML string using HtmlAgilityPack
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(Description);

                // Select all the list items (<li>) and display them with numbers
                int index = 1;
                foreach (var li in doc.DocumentNode.SelectNodes("//li"))
                {
                    string text = li.InnerText.Replace("&nbsp;", " ").Trim();  // Replace &nbsp; with a space and trim extra spaces
                    sb.AppendLine($"{index}. {text}" + Environment.NewLine);
                    index++;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.GetFormattedStringFromRawHTML()", ex.Message, false);
            }
            return sb.ToString();
        }

        /// <summary>
        /// This Method is used to process comma-separated values and add '&' between the last two values
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
            catch(Exception ex)
            {
                Helper.LogMessage(KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "CommonHelper.AddAmpersandToLast()", ex.Message, false);
            }
            return ReturnResult;
        }

        /// <summary>
        /// This Method is used to Get Custom Timesheet Error Message.
        /// </summary>
        /// <param name="Mode"></param>
        /// <param name="Source"></param>
        /// <param name="TaskDate"></param>
        /// <returns></returns>
        public static string GetCustomTimesheetErrorMessage(string Mode, string Source, DateTime? TaskDate = null)
        {
            string ReturnResult = string.Empty;
            try
            {
                DateTime effectiveDate = TaskDate ?? DateTime.Now;
                string TimesheetMonth = DateTimeHelper.GetMonthName(effectiveDate.Month);
                string TimesheetYear = Convert.ToString(effectiveDate.Year);
                if(Source == "Approved")
                {
                    if (Mode == "Edit")
                    {
                        ReturnResult = $"You Cannot Edit The Timesheet. Since Timesheet is Already Approved for the Month of {TimesheetMonth} - {TimesheetYear} !!!";
                    }
                    else
                    {
                        ReturnResult = $"You Cannot Add The Timesheet. Since Timesheet is Already Approved for the Month of {TimesheetMonth} - {TimesheetYear} !!!";
                    }
                }
                else
                {
                    if (Mode == "Edit")
                    {
                        ReturnResult = $"You Cannot Edit The Timesheet. Since Timesheet is Already Sent for Approval for the Month of {TimesheetMonth} - {TimesheetYear} !!!";
                    }
                    else
                    {
                        ReturnResult = $"You Cannot Add The Timesheet. Since Timesheet is Already Sent for Approval for the Month of {TimesheetMonth} - {TimesheetYear} !!!";
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "CommonHelper.GetCustomTimesheetErrorMessage()", ex.Message, false);
            }
            return ReturnResult;
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
        /// This Method Is Used To Get Environment and Version No.
        /// </summary>
        /// <returns></returns>
        public static string GetEnvironmentAndVersionNo()
        {
            string ReturnMessage = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(KeyOption.Environment) && !string.IsNullOrEmpty(KeyOption.Version))
                {
                    ReturnMessage = $"{KeyOption.Environment} Version: {KeyOption.Version}";
                }
            }
            catch (Exception ex)
            {
                ReturnMessage = string.Empty;
                Helper.LogMessage(KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "CommonHelper.GetCustomTimesheetErrorMessage()", ex.Message, false);
            }
            return ReturnMessage;
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
            catch(Exception ex)
            {
                ReturnMessage = string.Empty;
                Helper.LogMessage(KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "CommonHelper.ConvertNumberedTextToHtmlList()", ex.Message, false);
            }
            return ReturnMessage;
        }


        #endregion [Custom Methods]
    }
}
