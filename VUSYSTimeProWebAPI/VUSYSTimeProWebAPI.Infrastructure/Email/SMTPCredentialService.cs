using System.Data;
using CommonLibCore10;
using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.EntityFramework;
using VUSYSTimeProWebAPI.Application.Abstractions.Caching;
using VUSYSTimeProWebAPI.Application.Abstractions.Email;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Email
{
    public sealed class SMTPCredentialService : ISMTPCredentialService
    {
        private const string SMTPCredentialServiceLogClassName = "SMTPCredentialService";
        private readonly ICacheService _CacheService;
        private readonly AppDbContext _dbContext;
        private static readonly SemaphoreSlim _smtpCredGate = new(1, 1);
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(Convert.ToDouble(CommonHelper.KeyOption.CacheTimeSpanInHours));

        /// <summary>
        /// SMTP Credential Service Constructor
        /// </summary>
        /// <param name="cacheService"></param>
        /// <param name="dbContext"></param>
        public SMTPCredentialService(ICacheService cacheService, AppDbContext dbContext)
        {
            _CacheService = cacheService;
            _dbContext = dbContext;
        }

        /// <summary>
        /// This Method Is Used To Get Email SMTP Credential Configuration.
        /// First checks cache → if not available → calls SP → stores in cache (20 hrs).
        /// </summary>
        /// <param name="SourcePurpose"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConfigureEmailM> GetEmailSMTPCredentialConfiguration(string SourcePurpose, CancellationToken cancellationToken = default)
        {
            ConfigureEmailM _objConfigureEmail = new ConfigureEmailM();
            try
            {
                var cacheKey = $"SMTP_CREDENTIALS::{SourcePurpose}";

                // Check Cache First
                var cachedData = _CacheService.Get<ConfigureEmailM>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData; // Return from cache
                }

                // Prevent multiple threads hitting DB at same time
                await _smtpCredGate.WaitAsync(cancellationToken);
                try
                {
                    // Double-check cache after acquiring lock
                    cachedData = _CacheService.Get<ConfigureEmailM>(cacheKey);
                    if (cachedData != null)
                    {
                        return cachedData;
                    }

                    _objConfigureEmail = await GetEmailConfigurationSettingsAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (_objConfigureEmail != null)
                    {
                        _CacheService.Set(cacheKey, _objConfigureEmail, CacheDuration, null);
                        return _objConfigureEmail;
                    }
                    _objConfigureEmail = new ConfigureEmailM();
                    // Cache empty result for short duration
                    _CacheService.Set(cacheKey, _objConfigureEmail, CacheDuration, null);
                }
                finally
                {
                    _smtpCredGate.Release();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmailLogNamespace, $"{SMTPCredentialServiceLogClassName}.GetEmailSMTPCredentialConfiguration()", ex.Message, false);
            }
            return _objConfigureEmail;
        }

        /// <summary>
        /// This Method Is Used To Get Email Configuration Settings.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<ConfigureEmailM> GetEmailConfigurationSettingsAsync(CancellationToken cancellationToken = default)
        {
            ConfigureEmailM _objConfigureEmail = new ConfigureEmailM();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConfigureEmailM? emailConfiguration = await _dbContext.ConfigureEmailList
                        .AsNoTracking()
                        .Where(x => x.IsDeleted == false)
                        .Select(x => new ConfigureEmailM
                        {
                            Id = x.Id,
                            FromEmail = x.FromEmail,
                            Password = x.Password,
                            SmtpHost = x.SmtpHost,
                            PortNo = x.PortNo
                        })
                        .FirstOrDefaultAsync(
                            cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (emailConfiguration != null)
                {
                    emailConfiguration.Password = AESEncrytDecry.DecryptAES256(emailConfiguration.Password, Helper.EncryptDecryptKey);
                    _objConfigureEmail = emailConfiguration;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmailLogNamespace, $"{SMTPCredentialServiceLogClassName}.GetEmailConfigurationSettingsAsync()", ex.Message, false);
            }
            return _objConfigureEmail;
        }
    }
}
