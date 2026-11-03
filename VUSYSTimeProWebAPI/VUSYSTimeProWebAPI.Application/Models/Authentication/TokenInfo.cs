namespace VUSYSTimeProWebAPI.Application.Models.Authentication
{
    public class TokenInfo
    {
        /// <summary>
		/// User Name
		/// </summary>
		public string UserName { get; set; }

        /// <summary>
        /// Email ID
        /// </summary>
        public string EmailID { get; set; }

        /// <summary>
        /// Employee Code
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        public string RoleID { get; set; }

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
