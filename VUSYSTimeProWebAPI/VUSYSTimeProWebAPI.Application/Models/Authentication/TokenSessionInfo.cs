namespace VUSYSTimeProWebAPI.Application.Models.Authentication
{
    public class TokenSessionInfo
    {
        /// <summary>
		/// Name
		/// </summary>
		public string Name { get; set; }

        /// <summary>
        /// Login ID
        /// </summary>
        public string LoginId { get; set; }

        /// <summary>
		/// Token
		/// </summary>
		public string Token { get; set; }

        /// <summary>
        /// Expires At
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive { get; set; }
    }
}
