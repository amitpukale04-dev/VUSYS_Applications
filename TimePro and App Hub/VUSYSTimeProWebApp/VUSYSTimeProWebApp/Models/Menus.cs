using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VUSYSTimeProWebApp.Models
{
    public class Menus
    {

    }
    public class MenusM:CommonModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Menu Name
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// Controller Name
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// Action Name
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Main Menu Id
        /// </summary>
        public string MainMenuId { get; set; }

        /// <summary>
        /// Main Menu Name
        /// </summary>
        public string MainMenuName { get; set; }

        /// <summary>
        /// Menu Title
        /// </summary>
        public string MenuTitle { get; set; }

        /// <summary>
        /// Menu Description
        /// </summary>
        public string MenuDescription { get; set; }

        /// <summary>
        /// Menu Display Text
        /// </summary>
        public string MenuDisplayText { get; set; }

        /// <summary>
        /// Menu Css Font
        /// </summary>
        public string MenuCssFont { get; set; }

        /// <summary>
        /// Menu List Class Name
        /// </summary>
        public string MenuListClassName { get; set; }

        /// <summary>
        /// Menu Link Class Name
        /// </summary>
        public string MenuLinkClassName { get; set; }

       /// <summary>
       /// Menu IClass Name
       /// </summary>
        public string MenuIClassName { get; set; }

        /// <summary>
        /// Menu Param
        /// </summary>
        public string MenuParam { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Assigned By
        /// </summary>
        public string AssignedBy { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        new public string CreatedDate { get; set; }

        /// <summary>
        /// Mode
        /// </summary>
        public string MMode { get; set; }

        public string Category { get; set; }

        /// <summary>
        /// Access Id
        /// </summary>
        public int AccessId { get; set; }

        /// <summary>
        /// ToolTip Text
        /// </summary>
        public string ToolTipText { get; set; }

        /// <summary>
        /// Sort Order
        /// </summary>
        public string SortOrder { get; set; }

        /// <summary>
        /// Is Displayed In Menu
        /// </summary>
        public bool IsDisplayedInMenu { get; set; }
    }
}
