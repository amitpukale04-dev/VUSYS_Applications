namespace VUSYSTimeProWebAPI.Application.Abstractions.Security
{
    public interface IPasswordHashService
    {
        /// <summary>
        /// This Method Is Used To Generate Password Hash.
        /// </summary>
        /// <param name="Password"></param>
        /// <returns></returns>
        (string Hash, string Salt) GeneratePasswordHash(string Password);

        /// <summary>
        /// This Method Is Used Compute Password With Existing Salt.
        /// </summary>
        /// <param name="PlainPassword"></param>
        /// <param name="PasswordSalt"></param>
        /// <returns></returns>
        (string Hash, string Salt) ComputePasswordWithExistingSalt(string PlainPassword, string PasswordSalt);
    }
}
