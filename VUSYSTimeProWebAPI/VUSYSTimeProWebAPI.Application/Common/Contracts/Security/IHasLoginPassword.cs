namespace VUSYSTimeProWebAPI.Application.Common.Contracts.Security
{
    public interface IHasLoginPassword
    {
        /// <summary>
        /// Login Password
        /// </summary>
        string LoginPassword { get; set; }
    }
}
