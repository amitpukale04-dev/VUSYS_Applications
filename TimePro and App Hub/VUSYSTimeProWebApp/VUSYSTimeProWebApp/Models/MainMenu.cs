using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VUSYSTimeProWebApp.Models
{
    public class MainMenu: MainMenuMasterM
    {
        #region [Sub Menu List Properties]

        /// <summary>
        /// Sub Menu List
        /// </summary>
        public List<MenusM> SubMenuList { get; set; }

        #endregion
    }
}
