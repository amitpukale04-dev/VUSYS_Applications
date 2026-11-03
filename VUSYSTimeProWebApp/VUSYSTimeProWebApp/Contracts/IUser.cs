using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface IUser
    {
        /// <summary>
        /// This Method is Used to Get User List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<UsersM>> GetUserList(int ID, string Type);

        /// <summary>
        /// This Method is Used to Get User Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<UsersM> GetUserDetails(int? id);

        /// <summary>
        /// This Method is Used to Save Update User Details.
        /// </summary>
        /// <param name="_objUsersM"></param>
        /// <returns></returns>
        Task<string> SaveUpdateUserDetails(AppDbContext db, IWebHostEnvironment webHostEnvironment, UsersM _objUsersM);
    }
}
