using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface IEmployee
    {
        /// <summary>
        /// This Method is Used to Get Employee List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<EmployeeM>> GetEmployeeList(int ID, string Type);

        /// <summary>
        /// This Method is Used to Get Employee Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<EmployeeM> GetEmployeeDetails(int? id);

        /// <summary>
        /// This Method is Used to Save Update Employee Details.
        /// </summary>
        /// <param name="_objEmployeeM"></param>
        /// <returns></returns>
        Task<string> SaveUpdateEmployeeDetails(EmployeeM _objEmployeeM);
    }
}
