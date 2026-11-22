using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface IRoleMenuAccess
    {
        /// <summary>
        /// This Method is Used to Get Role Menu Access.
        /// </summary>
        /// <param name="sParam"></param>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        Task<IEnumerable<TreeViewNode>> GetRoleMenuAccess(string sParam, int? RoleId);

        /// <summary>
        /// This Method is Used to Save Role Menu Access.
        /// </summary>
        /// <param name="RoleID"></param>
        /// <param name="lsttreeViewNodes"></param>
        /// <returns></returns>
        Task<string> SaveRoleMenuAccess(int RoleID, List<TreeViewNode> lsttreeViewNodes);
    }
}
