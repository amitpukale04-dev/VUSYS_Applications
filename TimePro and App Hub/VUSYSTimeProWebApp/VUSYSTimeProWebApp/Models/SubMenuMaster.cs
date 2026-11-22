using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VUSYSTimeProWebApp.Models
{
    public class SubMenuMaster
    {
    }

    public class SubMenuMasterM:CommonModel
    {
        #region [Sub Menu Master Properties]

        /// <summary>
        /// ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Menu Name
        /// </summary>
        [Display(Name = "Menu Name")]
        [Required(ErrorMessage = "Enter The Menu Name.")]
        public string MenuName { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        [Display(Name = "Title")]
        [Required(ErrorMessage = "Enter The Title.")]
        public string Title { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [Display(Name = "Description")]
        [Required(ErrorMessage = "Enter The Description.")]
        public string Description { get; set; }

        /// <summary>
        /// Display Text
        /// </summary>
        [Display(Name = "Display Text")]
        [Required(ErrorMessage = "Enter The Display Text.")]
        public string DisplayText { get; set; }

        /// <summary>
        /// Main Menu ID
        /// </summary>
        [Required(ErrorMessage = "Select The Main Menu Name.")]
        public int MainMenuId { get; set; }

        /// <summary>
        /// Previous Main Menu ID
        /// </summary>
        public int PreviousMainMenuId { get; set; }

        /// <summary>
        /// Main Menu Name
        /// </summary>
        [Display(Name = "Main Menu Name")]
        public string MainMenuName { get; set; }

        /// <summary>
        /// Controller
        /// </summary>
        [Required(ErrorMessage = "Enter The Controller Name.")]
        public string Controller { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        [Required(ErrorMessage = "Enter The Action Name.")]
        public string Action { get; set; }

        /// <summary>
        /// CSS Font
        /// </summary>
        [Display(Name = "CSS Font")]
        [Required(ErrorMessage = "Enter The Css Font.")]
        public string CSSFont { get; set; }

        /// <summary>
        /// List Class Name
        /// </summary>
        [Display(Name = "List Class Name")]
        [Required(ErrorMessage = "Enter The List Class Name.")]
        public string ListClassName { get; set; }

        /// <summary>
        /// Link Class Name
        /// </summary>
        [Display(Name = "Link Class Name")]
        [Required(ErrorMessage = "Enter The Link Class Name.")]
        public string LinkClassName { get; set; }

        /// <summary>
        /// IClass Name
        /// </summary>
        [Display(Name = "IClass Name")]
        [Required(ErrorMessage = "Enter The IClass Name.")]
        public string IClassName { get; set; }

        /// <summary>
        /// Sort Order
        /// </summary>
        [Display(Name = "Sort Order")]
        [Required(ErrorMessage = "Enter The Sort Order.")]
        public int SortOrder { get; set; }

        /// <summary>
        /// ToolTip Text
        /// </summary>
        [Display(Name = "Tool Tip Text")]
        public string ToolTipText { get; set; }

        /// <summary>
        /// Is Displayed In Menu
        /// </summary>
        [Display(Name = "Is Display In Menu ")]
        public bool IsDisplayedInMenu { get; set; } = true;

        /// <summary>
        /// Displayed In Menu
        /// </summary>
        public string DisplayedInMenu { get; set; }

        #endregion
    }
}
