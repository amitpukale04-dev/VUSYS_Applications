using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Email
{
    public interface ISMTPCredentialService
    {
        /// <summary>
        /// This Method Is Used To Get Email SMTP Credential Configuration.
        /// </summary>
        /// <param name="SourcePurpose"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ConfigureEmailM> GetEmailSMTPCredentialConfiguration(string SourcePurpose, CancellationToken cancellationToken = default);
    }
}