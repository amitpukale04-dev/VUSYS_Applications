using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface IProject
    {
        /// <summary>
        /// This Method is Used to Get Project List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<ProjectM>> GetProjectList(int ID, string Type);

        /// <summary>
        /// This Method is Used to Get Project Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ProjectM> GetProjectDetails(int? id);

        /// <summary>
        /// This Method is Used to Save Update Project Details.
        /// </summary>
        /// <param name="_objProjectM"></param>
        /// <returns></returns>
        Task<string> SaveUpdateProjectDetails(ProjectM _objProjectM);

        /// <summary>
        /// This Method is Used to Notify Project Allocation To Employee.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="ID"></param>
        /// <param name="DBResponseMessage"></param>
        /// <param name="EmployeeProjectMappingDataList"></param>
        /// <returns></returns>
        string NotifyProjectAllocationToEmployee(AppDbContext db, IWebHostEnvironment webHostEnvironment, int ID, string DBResponseMessage, List<EmployeeProjectMappingDataList> EmployeeProjectMappingDataList);

        /// <summary>
        /// This Method is Used to Get Project Activity List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        Task<List<ProjectActivityM>> GetProjectActivityList(int ID, string Type);

        /// <summary>
        /// This Method is Used to Get Project Activity Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ProjectActivityM> GetProjectActivityDetails(int? id);

        /// <summary>
        /// This Method is Used to Save Update Project Activity Details.
        /// </summary>
        /// <param name="_objProjectActivityM"></param>
        /// <returns></returns>
        Task<string> SaveUpdateProjectActivityDetails(ProjectActivityM _objProjectActivityM);
    }
}
