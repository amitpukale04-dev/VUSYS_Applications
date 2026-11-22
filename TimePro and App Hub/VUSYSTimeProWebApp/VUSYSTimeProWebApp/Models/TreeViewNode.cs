using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VUSYSTimeProWebApp.Models
{
    public class TreeViewNode:CommonModel
    {
        /// <summary>
        /// id
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// parent
        /// </summary>
        public string parent { get; set; }
        
        /// <summary>
        /// text
        /// </summary>
        public string text { get; set; }

        /// <summary>
        /// RoleId
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// Main Menu Id
        /// </summary>
        public string MainMenuId { get; set; }

        /// <summary>
        /// State (Checked or UnChecked)
        /// </summary>
        public TreeViewNodeState State { get; set; }
    }
}
