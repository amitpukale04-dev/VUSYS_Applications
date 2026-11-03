using VUSYSTimeProWebAPI.Application.Abstractions.Authentication;
using VUSYSTimeProWebAPI.Application.Models.Authentication;

namespace VUSYSTimeProWebAPI.Infrastructure.Authentication
{
    public class TokenStoreService : ITokenStoreService
    {
        private readonly HashSet<string> _blacklistedTokens = new();
        private readonly List<RefreshToken> _store = new();
        private readonly Dictionary<string, TokenInfo> _tokenInfo = new Dictionary<string, TokenInfo>();

        /// <summary>
        /// This Method Is Used To Add Token To Blacklist.
        /// </summary>
        /// <param name="Token"></param>
        public void BlacklistToken(string Token)
        {
            _blacklistedTokens.Add(Token);
        }

        /// <summary>
        /// This Method Is Used To Check Whether The Token is Blacklisted Or Not.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public bool IsTokenBlacklisted(string Token)
        {
            return _blacklistedTokens.Contains(Token);
        }

        /// <summary>
        /// This Method Is Used To Save Token Details.
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="EmailID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="RoleID"></param>
        /// <param name="token"></param>
        /// <param name="isActive"></param>
        /// <param name="expiresAt"></param>
        /// <returns></returns>
        public Task SaveTokenInfoAsync(string UserName, string EmailID, string EmployeeCode, string RoleID, string token, bool isActive, DateTime expiresAt)
        {
            DeleteTokenAsync(EmailID);
            _tokenInfo[EmailID] = new TokenInfo
            {
                Token = token,
                IsActive = isActive,
                ExpiresAt = expiresAt,
                EmailID = EmailID,
                EmployeeCode = EmployeeCode,
                UserName = UserName,
                RoleID = RoleID
            };
            return Task.CompletedTask;
        }

        /// <summary>
        /// This Method Is Used To Get Token Details.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <returns></returns>
        public Task<TokenInfo> GetTokenInfo(string EmailID)
        {
            var session = _tokenInfo.Values.FirstOrDefault(s => s.EmailID == EmailID);
            return Task.FromResult(session);
        }

        /// <summary>
        /// This Method Is Used To Delete Token Details.
        /// </summary>
        /// <param name="EmailID"></param>
        /// <returns></returns>
        public Task DeleteTokenAsync(string EmailID)
        {
            if (_tokenInfo.ContainsKey(EmailID))
            {
                _tokenInfo.Remove(EmailID);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// This Method Is Used To Save Refresh Token.
        /// </summary>
        /// <param name="token"></param>
        public void Save(RefreshToken token) => _store.Add(token);

        /// <summary>
        /// This Method Is Used To Get Refresh Token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public RefreshToken? Get(string token) =>
            _store.FirstOrDefault(t => t.Token == token && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

        /// <summary>
        /// This Method Is Used To Get Refresh Token Details By Username.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public RefreshToken? GetByUsername(string username)
        {
            return _store
                .Where(t => t.UserName == username && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.ExpiresAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// This Method Is Used To Revoke Refresh Token.
        /// </summary>
        /// <param name="token"></param>
        public void Revoke(string token)
        {
            var match = _store.FirstOrDefault(t => t.Token == token);
            if (match != null) match.IsRevoked = true;
        }
    }
}
