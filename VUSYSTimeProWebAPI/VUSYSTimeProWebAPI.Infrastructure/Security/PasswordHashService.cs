using CommonLibCore10;
using VUSYSTimeProWebAPI.Application.Abstractions.Security;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using SimpleCrypto;

namespace VUSYSTimeProWebAPI.Infrastructure.Security
{
    public sealed class PasswordHashService : IPasswordHashService
    {
        private readonly ICryptoService _cryptoService;
        private readonly int _saltSize;
        private readonly int _hashIterations;
        private const string PasswordHashServiceLogClassName = "PasswordHashService";

        public PasswordHashService(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _saltSize = 16;
            _hashIterations = 100;
        }

        /// <summary>
        /// This Method Is Used To Generate Password Hash.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public (string Hash, string Salt) GeneratePasswordHash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }

            return GeneratePasswordHashInternal(password, _cryptoService, _saltSize, _hashIterations);
        }

        /// <summary>
        /// This Method Is Used Compute Password With Existing Salt.
        /// </summary>
        /// <param name="PlainPassword"></param>
        /// <param name="PasswordSalt"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public (string Hash, string Salt) ComputePasswordWithExistingSalt(string PlainPassword, string PasswordSalt)
        {
            if (string.IsNullOrWhiteSpace(PlainPassword))
            {
                throw new ArgumentException("Plain Password cannot be empty.", nameof(PlainPassword));
            }
            if (string.IsNullOrWhiteSpace(PasswordSalt))
            {
                throw new ArgumentException("Password Salt cannot be empty.", nameof(PasswordSalt));
            }
            return ComputePasswordWithExistingSaltInternal(PlainPassword, PasswordSalt, _cryptoService);
        }


        /// <summary>
        /// This Method Is Used To Generate Password Hash.
        /// </summary>
        /// <param name="PlainPassword"></param>
        /// <param name="CryptoService"></param>
        /// <param name="SaltSize"></param>
        /// <param name="HashIterations"></param>
        /// <returns></returns>
        private (string Hash, string Salt) GeneratePasswordHashInternal(string PlainPassword, ICryptoService CryptoService, int SaltSize, int HashIterations)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PlainPassword))
                    return (null, null);

                var hash = CryptoService.Compute(PlainPassword, SaltSize, HashIterations);
                var salt = CryptoService.Salt;

                return (hash, salt);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureSecurityLogNamespace, $"{PasswordHashServiceLogClassName}.GeneratePasswordHash()", ex.Message, false);
                return (null, null);
            }
        }

        /// <summary>
        /// This Method Is Used To Compute Password With Existing Salt.
        /// </summary>
        /// <param name="PlainPassword"></param>
        /// <param name="PasswordSalt"></param>
        /// <param name="CryptoService"></param>
        /// <returns></returns>
        private (string Hash, string Salt) ComputePasswordWithExistingSaltInternal(string PlainPassword, string PasswordSalt, ICryptoService CryptoService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PlainPassword) || string.IsNullOrWhiteSpace(PasswordSalt))
                    return (null, PasswordSalt);

                var hash = CryptoService.Compute(PlainPassword, PasswordSalt);
                return (hash, PasswordSalt);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureSecurityLogNamespace, $"{PasswordHashServiceLogClassName}.ComputePasswordWithExistingSalt()", ex.Message, false);
                return (null, PasswordSalt);
            }
        }
    }
}