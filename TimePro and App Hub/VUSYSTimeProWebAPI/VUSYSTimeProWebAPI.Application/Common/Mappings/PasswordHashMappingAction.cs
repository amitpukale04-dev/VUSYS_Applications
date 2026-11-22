using AutoMapper;
using VUSYSTimeProWebAPI.Application.Abstractions.Security;
using VUSYSTimeProWebAPI.Application.Common.Contracts.Security;

namespace VUSYSTimeProWebAPI.Application.Common.Mappings.Actions
{
    public sealed class PasswordHashMappingAction<TSource, TDestination> : IMappingAction<TSource, TDestination> where TSource : IHasLoginPassword where TDestination : IHasPasswordHash
    {
        private readonly IPasswordHashService _passwordHashService;

        public PasswordHashMappingAction(IPasswordHashService passwordHashService)
        {
            _passwordHashService = passwordHashService ?? throw new ArgumentNullException(nameof(passwordHashService));
        }

        public void Process(TSource source, TDestination destination, ResolutionContext context)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            // For edit operations, retain the existing password
            // when a new password was not entered.
            if (string.IsNullOrWhiteSpace(source.LoginPassword))
            {
                return;
            }

            var passwordDetails = _passwordHashService.GeneratePasswordHash(source.LoginPassword);

            destination.EncryptedPassword = passwordDetails.Hash;
            destination.PasswordSalt = passwordDetails.Salt;
        }
    }
}