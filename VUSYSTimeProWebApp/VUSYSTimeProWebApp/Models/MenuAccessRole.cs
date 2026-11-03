
using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.Models
{
    public class MenuAccessRole:CommonModel
    {
        /// <summary>
        /// Role ID
        /// </summary>
        [Required(ErrorMessage = "Select The Role.")]
        public int RoleID { get; set; }

        /// <summary>
        /// Role
        /// </summary>
        public string Role { get; set; }
    }
}