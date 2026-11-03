using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VUSYSTimeProWebApp.Models
{
    public class CommonModel
    {
        #region [Common Model Properties]

        /// <summary>
        /// Encrypted ID
        /// </summary>
        [NotMapped]
        public string EncryptedId { get; set; }

        /// <summary>
        /// Is Deleted
        /// </summary>
        [Display(Name = "Obsolete")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Created By
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Updated By
        /// </summary>
        public int UpdatedBy { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Updated Date
        /// </summary>
        public DateTime UpdatedDate { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// SL No
        /// </summary>
        public Int64 SLNo { get; set; }

        /// <summary>
        /// Active Status
        /// </summary>
        public string ActiveStatus { get; set; }


        #endregion
    }
}
