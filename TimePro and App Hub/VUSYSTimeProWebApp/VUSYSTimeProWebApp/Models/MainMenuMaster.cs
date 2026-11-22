using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{

    public class MainMenuMaster
    {
    }

    [Table("VUSYS_MainMenuMaster")]
    [Keyless]
    public class MainMenuMasterM:CommonModel
    {

        #region [Main Menu Master Properties]

        /// <summary>
        /// Main Menu ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Menu Name
        /// </summary>
        [Display(Name = "Menu Name")]
        [Required(ErrorMessage = "Enter The Menu Name.")]
        public string MainMenuName { get; set; }

        /// <summary>
        /// Main Menu List Class Name
        /// </summary>
        [Display(Name = "Menu List Class Name")]
        [Required(ErrorMessage = "Enter The Menu List Class Name.")]
        public string MainMenuListClassName { get; set; }

        /// <summary>
        /// Main Menu Link Class Name
        /// </summary>
        [Display(Name = "Menu Link Class Name")]
        [Required(ErrorMessage = "Enter The Menu Link Class Name.")]
        public string MainMenuLinkClassName { get; set; }

        /// <summary>
        /// Main Menu IClass Name
        /// </summary>
        [Display(Name = "IClass Name")]
        [Required(ErrorMessage = "Enter The IClass Name.")]
        public string MainMenuIClassName { get; set; }

        /// <summary>
        /// Main Menu Display Text
        /// </summary>
        [Display(Name = "Display Text")]
        [Required(ErrorMessage = "Enter The Display Text.")]
        public string MainMenuDisplayText { get; set; }

        /// <summary>
        /// Main Menu IClass Name Display Text
        /// </summary>
        [Display(Name = "IClass Name Display Text")]
        [Required(ErrorMessage = "Enter The IClass Name Display Text.")]
        public string MainMenuIClassNameDisplayText { get; set; }

        /// <summary>
        /// Main Menu Span Class
        /// </summary>
        [Display(Name = "Span Class")]
        public string MainMenuSpanClass { get; set; }

        /// <summary>
        /// Is Displayed In Menu
        /// </summary>
        [Display(Name = "Is Displayed In Menu")]
        public bool IsDisplayedInMenu { get; set; }

        #endregion
    }
}
