using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.OTPService;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Users;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using ApplicationSendEmailRequestParam = VUSYSTimeProWebAPI.Application.Models.SendEmailRequestParam;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.OTPService
{
    public sealed class OTPService : IOTPService
    {
        private const string OTPServiceLogClassName = "OTPService";
        private readonly IUserSharedService _UserSharedSvc;
        private readonly IEmailService _EmailSvc;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// OTP Service Class Constructor
        /// </summary>
        /// <param name="UserSharedSvc"></param>
        /// <param name="EmailSvc"></param>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OTPService(IUserSharedService UserSharedSvc, IEmailService EmailSvc, IWebHostEnvironment webHostEnvironment)
        {
            _UserSharedSvc = UserSharedSvc ?? throw new ArgumentNullException(nameof(UserSharedSvc));
            _EmailSvc = EmailSvc ?? throw new ArgumentNullException(nameof(EmailSvc));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        #region [OTP]

        /// <summary>
        /// This Method Is Used To Generate Or Resend OTP.
        /// </summary>
        /// <param name="_objOTP"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GenerateOrResendOTP(OTP _objOTP, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                finalDataSet = await SendOTPEmail(_objOTP.EmailID, string.Empty, cancellationToken);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureOTPServiceLogNamespace, $"{OTPServiceLogClassName}.GenerateOrResendOTP()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Send an OTP to Email ID.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <param name="FullName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SendOTPEmail(string EmailID, string FullName, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new DataSet();
            string TableName = "OTP_SENT_RESPONSE";
            List<string> columns = new List<string> { "ID", "otp", "Status", "Message" };
            string ErrorMessage = "Failed To Send OTP !!!";
            try
            {
                string strEmailID = string.Empty;
                string strFullName = string.Empty;
                if (!string.IsNullOrEmpty(EmailID) && !string.IsNullOrEmpty(FullName))
                {
                    strEmailID = EmailID;
                    strFullName = FullName;
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var ds = await _UserSharedSvc.GetUserDetails(EmailID, "CheckUserExist", cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (Helper.CheckDataSetIsNullOrEmpty(ds))
                    {
                        string DBStatus = Helper.ToProperCase(Convert.ToString(ds.Tables[0].Rows[0]["Status"].ToString()));
                        if (DBStatus == Helper.ToProperCase(MessageTypeConstants.Success))
                        {
                            strEmailID = Convert.ToString(ds.Tables[0].Rows[0]["EmailID"].ToString());
                            strFullName = Convert.ToString(ds.Tables[0].Rows[0]["FullName"].ToString());
                        }
                        else
                        {
                            List<List<string>> rows = new List<List<string>>
                            {
                                new List<string> { EmailID, string.Empty, Helper.ToProperCase(MessageTypeConstants.Error), ErrorMessage }
                            };
                            finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                            return finalDataSet;
                        }
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { EmailID, string.Empty, Helper.ToProperCase(MessageTypeConstants.Error), ErrorMessage }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                        return finalDataSet;
                    }
                }

                string OTP = Helper.GenerateOTP(4);
                string EncodedOTP = Helper.EncodeStringToBase64(OTP);

                string EmailBodyTemplate = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(_webHostEnvironment, "OTPAuthenticationEmailTemplate.html");
                EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", strFullName)
                                                     .Replace("#OTP#", OTP)
                                                     .Replace("#OTPExpiryInMinutes#", CommonHelper.KeyOption.OTPExpiryInMinutes)
                                                     .Replace("#CurrentYear#", DateTimeHelper.CurrentYear());
                string MailSubject = $"Time-Pro - OTP Authentication";
                var sendReq = CommonHelper.CreateSendEmailRequestParam(string.Empty, strEmailID, string.Empty, string.Empty, string.Empty, string.Empty, MailSubject, EmailBodyTemplate);
                var emailReturnResult = await _EmailSvc.SendEmailAPI(sendReq, CommonHelper.SMTP_EMAIL_SOURCE_PURPOSE[0], string.Empty, string.Empty, false, false, cancellationToken);
                string returnResult = ObjectToJson.ConvertDataSetToJson(emailReturnResult);
                JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(returnResult));
                string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                string EmailSentResponseReturnResult = Helper.Email_Failed;
                if (resultjObjectEmail == "ok")
                {
                    var EmailResponseData = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>();
                    var EmailResponse = EmailResponseData?.FirstOrDefault();
                    if (EmailResponse?.Message == Helper.EmailSuccess)
                    {
                        EmailSentResponseReturnResult = "OTP Has Been Sent To Your Email ID !!!";
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { EmailID, EncodedOTP, Helper.ToProperCase(MessageTypeConstants.Success), EmailSentResponseReturnResult }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                    else
                    {
                        List<List<string>> rows = new List<List<string>>
                        {
                            new List<string> { EmailID, string.Empty, Helper.ToProperCase(MessageTypeConstants.Error), ErrorMessage }
                        };
                        finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                    }
                }
                else
                {
                    List<List<string>> rows = new List<List<string>>
                    {
                        new List<string> { EmailID, string.Empty, Helper.ToProperCase(MessageTypeConstants.Error), ErrorMessage }
                    };
                    finalDataSet = Helper.GenerateCommonMessage(columns, rows, TableName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureOTPServiceLogNamespace, $"{OTPServiceLogClassName}.SendOTPEmail()", ex.Message, false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [OTP]
    }
}
