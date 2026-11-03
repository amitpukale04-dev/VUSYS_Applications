namespace VUSYSTimeProWebAPI.Application.Models.Authentication
{
    public class JwtSettings
    {
        /// <summary>
        /// Validate Issuer Signing Key
        /// </summary>
        public bool ValidateIssuerSigningKey
        {
            get;
            set;
        }

        /// <summary>
        /// Issuer Signing Key
        /// </summary>
        public string IssuerSigningKey
        {
            get;
            set;
        }

        /// <summary>
        /// Validate Issuer
        /// </summary>
        public bool ValidateIssuer
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Valid Issuer
        /// </summary>
        public string ValidIssuer
        {
            get;
            set;
        }

        /// <summary>
        /// Validate Audience
        /// </summary>
        public bool ValidateAudience
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Validate Audience
        /// </summary>
        public string ValidAudience
        {
            get;
            set;
        }

        /// <summary>
        /// Require Expiration Time
        /// </summary>
        public bool RequireExpirationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Validate Life Time
        /// </summary>
        public bool ValidateLifetime
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Token Expiration In Minutes
        /// </summary>
        public int TokenExpirationInMinutes
        {
            get;
            set;
        }

        /// <summary>
		/// Valid Audiences
		/// </summary>
		public List<string> ValidAudiences { get; set; }

        /// <summary>
        /// Refresh Token Expiration In Days
        /// </summary>
        public int RefreshTokenExpirationInDays
        {
            get;
            set;
        }
    }
}
