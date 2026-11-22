namespace VUSYSTimeProWebAPI.Application.Common.Contracts.Security
{
    public interface IHasPasswordHash
    {
        /// <summary>
        /// Encrypted Password
        /// </summary>
        string EncryptedPassword { get; set; }

        /// <summary>
        /// Password Salt
        /// </summary>
        string PasswordSalt { get; set; }
    }
}
