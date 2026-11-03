using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Project
{
    public interface IProjectService
    {
        #region [Project Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Project Details.
        /// </summary>
        /// <param name="_objProjectM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateProjectDetails(ProjectM _objProjectM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Project Detail Or List.
        /// </summary>
        /// <param name="_objProjectDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectDetailOrList(ProjectDetailsRequestParam _objProjectDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Project Master]

        #region [Employee Project Mapping]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Project Mapping Details.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateEmployeeProjectMappingDetails(EmployeeProjectMappingM _objEmployeeProjectMappingM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Employee Project Mapping Detail Or List.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmployeeProjectMappingDetailOrList(EmployeeProjectMappingDetailsRequestParam _objEmployeeProjectMappingDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Employee Project Mapping]

        #region [Project Activity]

        /// <summary>
        /// This Method Is Used To Save Or Update Project Activity Details.
        /// </summary>
        /// <param name="_objProjectActivityM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateProjectActivityDetails(ProjectActivityM _objProjectActivityM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Project Activity Detail Or List.
        /// </summary>
        /// <param name="_objProjectActivityDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetProjectActivityDetailOrList(ProjectActivityDetailsRequestParam _objProjectActivityDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Project Activity]
    }
}