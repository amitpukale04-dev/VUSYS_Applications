using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class EmployeeType
    {
    }

    [Table("VUSYS_EmployeeTypeMaster")]
    //[Keyless]
    public class EmployeeTypeM
    {
        #region [Employee Type Properties]

        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Employee Type
        /// </summary>
        [Display(Name = "Employee Type")]
        [Required(ErrorMessage = "Enter The Employee Type.")]
        public string EmployeeType { get; set; }

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


        #endregion
    }
    public class EmployeeTypeDataList : CommonModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// Employee Type
        /// </summary>
        public string EmployeeType { get; set; }
    }
}
