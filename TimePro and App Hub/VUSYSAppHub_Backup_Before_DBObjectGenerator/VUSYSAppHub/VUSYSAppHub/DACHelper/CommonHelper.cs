using System.Data;
using CommonLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.DACHelper
{
    public static class CommonHelper
    {
        /// <summary>
        /// Key Option
        /// </summary>
        public static MyApplicationKeysOptions KeyOption { get; set; } = new MyApplicationKeysOptions();

        /// <summary>
        /// SHARPPLY_ENCRYPT_DECRYPT_KEY
        /// </summary>
        public static readonly string SHARPPLY_ENCRYPT_DECRYPT_KEY = "Sharpply@123456$Key@191220192024";

        /// <summary>
        /// PROTECTRON_ENCRYPT_DECRYPT_KEY
        /// </summary>
        public static readonly string PROTECTRON_ENCRYPT_DECRYPT_KEY = "Protectron@123456$Key@1912201924";

        /// <summary>
        /// INSPIRE_MAGNETICS_ENCRYPT_DECRYPT_KEY
        /// </summary>
        public static readonly string INSPIREMAGNETICS_ENCRYPT_DECRYPT_KEY = "IMagnetics@123456$Key@1912201924";

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
                Helper.LogMessage(KeyOption.ErrorLog, "VUSYSAppHub.DACHelper", "CommonHelper.CheckDirectoryExists()", ex.Message, false);
                throw;
            }
            return IsExists;
        }


        /// <summary>
        /// This Method Is Used To Get JSON File Path.
        /// </summary>
        /// <param name="_IWebHostEnvironment"></param>
        /// <returns></returns>
        public static string GetJSONFilePath(IWebHostEnvironment _IWebHostEnvironment, string JSONFileName)
        {
            return Path.Combine(_IWebHostEnvironment.ContentRootPath, "Configurations", JSONFileName);
        }

        #region [Send Email]

        /// <summary>
        /// This Method Is Used To Send an Email.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="objSendEmailRequestParam"></param>
        /// <param name="FolderPath"></param>
        /// <param name="FileName"></param>
        /// <param name="IsMultipleFiles"></param>
        /// <param name="HasAttachment"></param>
        /// <returns></returns>
        public static DataSet SendEmailAPI(IWebHostEnvironment webHostEnvironment, SendEmailRequestParam objSendEmailRequestParam, string FolderPath, string FileName, bool IsMultipleFiles, bool HasAttachment)
        {
            string FromEmailID = string.Empty;
            string Password = string.Empty;
            string SmtpHost = string.Empty;
            string PortNo = string.Empty;
            DataSet finalDataSet = new DataSet();
            bool IsEmailSent = false;
            try
            {
                List<ConfigureEmail> lstConfigureEmail = new List<ConfigureEmail>();
                lstConfigureEmail = GetEmailConfigurationList(webHostEnvironment);

                if (lstConfigureEmail.Count > 0)
                {
                    FromEmailID = lstConfigureEmail[0].FromEmailID;
                    Password = lstConfigureEmail[0].Password;
                    SmtpHost = lstConfigureEmail[0].SmtpHost;
                    PortNo = lstConfigureEmail[0].PortNo;

                    IsEmailSent = Helper.SendEmail(objSendEmailRequestParam.EmailSubject, objSendEmailRequestParam.EmailBody, FromEmailID, Password, SmtpHost, PortNo, objSendEmailRequestParam.ToEmail, objSendEmailRequestParam.CCEmail, string.Empty, FolderPath, FileName, IsMultipleFiles, HasAttachment);
                    List<SendEmailResponseParam> lstSendEmailResponseParam = new List<SendEmailResponseParam>();
                    if (IsEmailSent == true)
                    {
                        lstSendEmailResponseParam.Add(new SendEmailResponseParam { EmailSent = true, Message = Helper.EmailSuccess });
                    }
                    else
                    {
                        lstSendEmailResponseParam.Add(new SendEmailResponseParam { EmailSent = false, Message = Helper.EmailFailed });
                    }
                    var dt1 = Helper.ConvertListToDataTable(lstSendEmailResponseParam);
                    if (dt1.Rows.Count > 0)
                    {
                        finalDataSet = Helper.ConvertDataTableToDataSet(dt1);
                        finalDataSet.Tables[0].TableName = "EMAIL_SENT_RESPONSE";
                    }
                }
            }
            catch (Exception ex)
            {
                finalDataSet = null;
                Helper.LogMessage(KeyOption.ErrorLog, "VUSYSAppHub.DACHelper", "CommonHelper.SendEmailAPI()", ex.Message, false);
            }
            return finalDataSet;
        }

        #endregion [Send Email]

        #region [Read Configuration Files]

        /// <summary>
        /// This Method is Used to Get Email Configuration List.
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <returns></returns>
        public static List<ConfigureEmail> GetEmailConfigurationList(IWebHostEnvironment webHostEnvironment)
        {
            List<ConfigureEmail> lstConfigureEmail = new List<ConfigureEmail>();
            try
            {
                string ConfigurationFilesPath = GetJSONFilePath(webHostEnvironment, "EmailConfiguration.json");

                //get the Json filepath
                if (File.Exists(ConfigurationFilesPath))
                {
                    //deserialize JSON from file
                    string JsonData = File.ReadAllText(ConfigurationFilesPath);
                    JObject objJsonResponseResult = JObject.Parse(JsonData);
                    string strEmailConfigurationDetails = string.Join(",", objJsonResponseResult["EmailConfigurationDetails"]);
                    if (strEmailConfigurationDetails != null && strEmailConfigurationDetails != "")
                    {
                        string strEmailConfigurationDetailsJsonResult = "[" + strEmailConfigurationDetails + "]";
                        if (strEmailConfigurationDetailsJsonResult != null && strEmailConfigurationDetailsJsonResult != "")
                        {
                            List<ConfigureEmail> lstEmailConfiguration = JsonConvert.DeserializeObject<List<ConfigureEmail>>(strEmailConfigurationDetailsJsonResult);
                            if (lstEmailConfiguration.Count > 0)
                            {
                                lstConfigureEmail = lstEmailConfiguration;
                            }
                            else
                            {
                                lstConfigureEmail = null;
                            }
                        }
                        else
                        {
                            lstConfigureEmail = null;
                        }
                    }
                    else
                    {
                        lstConfigureEmail = null;
                    }
                }
            }
            catch (Exception ex)
            {
                lstConfigureEmail = null;
                Helper.LogMessage(KeyOption.ErrorLog, "VUSYSAppHub.DACHelper", "CommonHelper.GetEmailConfigurationList()", ex.Message, false);
            }
            return lstConfigureEmail;
        }
    }

    #endregion [Read Configuration Files]
}
