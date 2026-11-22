using System.Data;
using System.Net.Mail;
using AutoMapper;
using CommonLibCore10;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using ApplicationSendEmailRequestParam = VUSYSTimeProWebAPI.Application.Models.SendEmailRequestParam;
using CommonSendEmailRequestParam = CommonLibCore10.SendEmailRequestParam;

namespace VUSYSTimeProWebAPI.Infrastructure.Email
{
    public sealed class EmailService : IEmailService
    {
        private const string EmailServiceLogClassName = "EmailService";
        private readonly ISMTPCredentialService _smtpCredentialService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Email Service Constructor.
        /// </summary>
        /// <param name="smtpCredentialService"></param>
        /// <param name="mapper"></param>
        public EmailService(ISMTPCredentialService smtpCredentialService, IMapper mapper)
        {
            _smtpCredentialService = smtpCredentialService ?? throw new ArgumentNullException(nameof(smtpCredentialService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// This Method Is Used To Send an Email.
        /// </summary>
        /// <param name="SendEmailRequestParam"></param>
        /// <param name="SourcePurpose"></param>
        /// <param name="FolderPath"></param>
        /// <param name="FileName"></param>
        /// <param name="IsMultipleFiles"></param>
        /// <param name="hasAttachment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SendEmailAPI(ApplicationSendEmailRequestParam SendEmailRequestParam, string SourcePurpose, string FolderPath, string FileName, bool IsMultipleFiles, bool hasAttachment, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(SendEmailRequestParam);
                ConfigureEmailM emailConfigurationDetails = await _smtpCredentialService.GetEmailSMTPCredentialConfiguration(SourcePurpose, cancellationToken);
                if (emailConfigurationDetails == null)
                {
                    return finalDataSet;
                }
                ApplySMTPConfiguration(SendEmailRequestParam, emailConfigurationDetails);
                SendEmailRequestParam.FolderPath = FolderPath;
                SendEmailRequestParam.FileName = FileName;
                SendEmailRequestParam.IsMultipleFiles = IsMultipleFiles;
                SendEmailRequestParam.HasAttachment = hasAttachment;
                cancellationToken.ThrowIfCancellationRequested();
                CommonSendEmailRequestParam commonSendEmailRequestParam = _mapper.Map<CommonSendEmailRequestParam>(SendEmailRequestParam);
                bool isEmailSent = Helper.SendEmail(commonSendEmailRequestParam);
                finalDataSet = BuildEmailResponseDataSet(isEmailSent);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                finalDataSet = null;
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmailLogNamespace, $"{EmailServiceLogClassName}.SendEmailAPI()", ex.Message, false);
            }
            return finalDataSet;
        }



        /// <summary>
        /// This Method Is Used To Send an Email With Multiple Attachments.
        /// </summary>
        /// <param name="SendEmailRequestParam"></param>
        /// <param name="SourcePurpose"></param>
        /// <param name="attachments"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SendEmailAPIWithMultipleAttachments(ApplicationSendEmailRequestParam SendEmailRequestParam, string SourcePurpose, IEnumerable<Attachment>? attachments, CancellationToken cancellationToken = default)
        {
            DataSet finalDataSet = new();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(SendEmailRequestParam);
                ConfigureEmailM emailConfigurationDetails = await _smtpCredentialService.GetEmailSMTPCredentialConfiguration(SourcePurpose, cancellationToken);
                if (emailConfigurationDetails == null)
                {
                    return finalDataSet;
                }

                ApplySMTPConfiguration(SendEmailRequestParam, emailConfigurationDetails);
                SendEmailRequestParam.attachments =attachments;
                cancellationToken.ThrowIfCancellationRequested();
                CommonSendEmailRequestParam commonSendEmailRequestParam = _mapper.Map<CommonSendEmailRequestParam>(SendEmailRequestParam);
                bool isEmailSent = Helper.SendEmailWithMultipleAttachments(commonSendEmailRequestParam);
                finalDataSet = BuildEmailResponseDataSet(isEmailSent);
            }
            catch (OperationCanceledException)
                when (cancellationToken
                    .IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                finalDataSet = null;
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmailLogNamespace, $"{EmailServiceLogClassName}.SendEmailAPIWithMultipleAttachments()", ex.Message, false);
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method Is Used To Send Email Notification.
        /// </summary>
        /// <param name="SendEmailRequestParam"></param>
        /// <param name="SourcePurpose"></param>
        /// <param name="fileAttachments"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> SendEmailNotification(ApplicationSendEmailRequestParam SendEmailRequestParam, string SourcePurpose, List<Attachment> fileAttachments, CancellationToken cancellationToken = default)
        {
            bool IsEmailSent = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                DataSet emailResult = await SendEmailAPIWithMultipleAttachments(SendEmailRequestParam, SourcePurpose, fileAttachments, cancellationToken);
                string json = ObjectToJson.ConvertDataSetToJson(emailResult);
                JObject jObj = ApiHelper.GetDataInJObject(Util.OK(json));

                string ok = ApiHelper.CheckResByTableName(jObj, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);
                if (!string.Equals(ok, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    IsEmailSent = false;
                    return IsEmailSent;
                }

                var emailResponseData = jObj["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>();
                var first = emailResponseData?.FirstOrDefault();

                IsEmailSent = first != null && string.Equals(first.Message, Helper.EmailSuccess, StringComparison.OrdinalIgnoreCase);
                return IsEmailSent;
            }
            catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmailLogNamespace, $"{EmailServiceLogClassName}.SendEmailNotification()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
        /// This Method Is Used To Applies SMTP configuration values to the email request.
        /// </summary>
        /// <param name="sendEmailRequestParam"></param>
        /// <param name="emailConfiguration"></param>
        private static void ApplySMTPConfiguration(ApplicationSendEmailRequestParam sendEmailRequestParam, ConfigureEmailM emailConfiguration)
        {
            string displayName = emailConfiguration.DisplayName ?? CommonHelper.EMAIL_DISPLAY_NAME;

            if (!string.IsNullOrWhiteSpace(sendEmailRequestParam.FromDisplayName))
            {
                displayName = sendEmailRequestParam.FromDisplayName;
            }
            sendEmailRequestParam.FromDisplayName = displayName;
            sendEmailRequestParam.FromEmailID = emailConfiguration.FromEmail;
            sendEmailRequestParam.Password = emailConfiguration.Password;
            sendEmailRequestParam.SmtpHost = emailConfiguration.SmtpHost;
            sendEmailRequestParam.PortNo = Convert.ToString(emailConfiguration.PortNo);
        }

        /// <summary>
        /// This Method Is Used To Build Email Response DataSet.
        /// </summary>
        /// <param name="isEmailSent"></param>
        /// <returns></returns>
        private static DataSet BuildEmailResponseDataSet(bool isEmailSent)
        {
            List<SendEmailResponseParam> sendEmailResponseParams = [
                new SendEmailResponseParam
                {
                    EmailSent = isEmailSent,
                    Message = isEmailSent ? Helper.EmailSuccess : Helper.EmailFailed
                }
                ];

            DataTable responseTable = Helper.ConvertListToDataTable(sendEmailResponseParams);
            DataSet finalDataSet = new();
            if (Helper.CheckDataTableIsNullOrEmpty(responseTable))
            {
                finalDataSet = Helper.ConvertDataTableToDataSet(responseTable);

                if (Helper.CheckDataSetIsNullOrEmpty(finalDataSet))
                {
                    finalDataSet.Tables[0].TableName = Helper.EMAIL_SENT_RESPONSE_TABLE_NAME;
                }
            }
            return finalDataSet;
        }
    }
}