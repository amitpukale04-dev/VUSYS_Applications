using System.Data;
using System.Net.Mail;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// This Method Is Used To Send an Email.
        /// </summary>
        /// <param name="SendEmailRequestParam"></param>
        /// <param name="SourcePurpose"></param>
        /// <param name="FolderPath"></param>
        /// <param name="FileName"></param>
        /// <param name="IsMultipleFiles"></param>
        /// <param name="HasAttachment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SendEmailAPI(SendEmailRequestParam SendEmailRequestParam, string SourcePurpose, string FolderPath, string FileName, bool IsMultipleFiles, bool HasAttachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Send an Email With Multiple Attachments.
        /// </summary>
        /// <param name="SendEmailRequestParam"></param>
        /// <param name="SourcePurpose"></param>
        /// <param name="attachments"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SendEmailAPIWithMultipleAttachments(SendEmailRequestParam SendEmailRequestParam, string SourcePurpose, IEnumerable<Attachment>? attachments, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Send Email Notification.
        /// </summary>
        /// <param name="SendEmailRequestParam"></param>
        /// <param name="SourcePurpose"></param>
        /// <param name="FileAttachments"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> SendEmailNotification(SendEmailRequestParam SendEmailRequestParam, string SourcePurpose, List<Attachment> FileAttachments, CancellationToken cancellationToken = default);
    }
}