using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class Project
    {
    }

    [Table("VUSYS_Projects")]
    [Keyless]
    public class ProjectM:CommonModel
    {
        #region [Project Properties]

        /// <summary>
        /// Project ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectID { get; set; } = 0;

        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// Project Code
        /// </summary>
        [Display(Name = "Project Code")]
        [Required(ErrorMessage = "Enter The Project Code.")]
        public string ProjectCode { get; set; }

        /// <summary>
        /// Client Name
        /// </summary>
        [Display(Name = "Client Name")]
        [Required(ErrorMessage = "Enter The Client Name.")]
        public string ClientName { get; set; }

        /// <summary>
        /// Project Name
        /// </summary>
        [Display(Name = "Project Name")]
        [Required(ErrorMessage = "Enter The Project Name.")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Project Description
        /// </summary>
        [Display(Name = "Project Description")]
        [Required(ErrorMessage = "Enter The Project Description.")]
        public string ProjectDescription { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        [Required(ErrorMessage = "Select The Start Date."), DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End Date
        /// </summary>
        [Required(ErrorMessage = "Select The End Date."), DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Display Start Date
        /// </summary>
        public string DisplayStartDate { get; set; }

        /// <summary>
        /// Display End Date
        /// </summary>
        public string DisplayEndDate { get; set; }

        /// <summary>
        /// Allocated Hours
        /// </summary>
        [Display(Name = "Allocated Hours")]
        [Required(ErrorMessage = "Enter The Allocated Hours.")]
        public decimal AllocatedHours { get; set; }

        /// <summary>
        /// Consumed Hours
        /// </summary>
        [Display(Name = "Consumed Hours")]
        [Required(ErrorMessage = "Enter The Consumed Hours.")]
        public decimal ConsumedHours { get; set; }

        /// <summary>
        /// Project Status
        /// </summary>
        [Required(ErrorMessage = "Select The Project Status.")]
        [Display(Name = "Project Status")]
        public string ProjectStatus { get; set; }

        #endregion
    }

    public enum ProjectStatus
    {
        Not_Started = 1,
        In_Progress = 2,
        Completed = 3,
        On_Hold = 4
    }
}
