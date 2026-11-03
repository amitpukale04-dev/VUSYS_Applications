using System.ComponentModel;

namespace VUSYSTimeProWebApp.Models
{
    public class UserProfileImage
    {
        #region [User Profile Image Properties]
        /// <summary>
        /// Profile Image
        /// </summary>
        //[Required(ErrorMessage = "Select The Profile Image")]
        [DisplayName("Image")]
        public IFormFile ProfileImage { get; set; }

        #endregion
    }
}
