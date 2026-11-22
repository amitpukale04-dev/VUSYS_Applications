using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface IRole
    {
        /// <summary>
        /// This Method is Used to Get Role List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<RolesM>> GetRoleList(int ID, string Type);

        /// <summary>
        /// This Method is Used to Get Role Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RolesM> GetRoleDetails(int? id);

        /// <summary>
        /// This Method is Used to Save Update Role Details.
        /// </summary>
        /// <param name="_objRolesM"></param>
        /// <returns></returns>
        Task<string> SaveUpdateRoleDetails(RolesM _objRolesM);
    }
}
