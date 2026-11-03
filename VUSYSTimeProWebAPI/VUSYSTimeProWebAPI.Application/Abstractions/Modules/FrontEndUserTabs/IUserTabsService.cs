using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.FrontEndUserTabs
{
    public interface IUserTabsService
    {
        #region [User Tabs]

        /// <summary>
        /// This Method Is Used To Get User Tabs.
        /// </summary>
        /// <param name="_objUserTabsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetUserTabs(UserTabsRequestParam _objUserTabsRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Save User Tabs.
        /// </summary>
        /// <param name="_objSaveUserTabsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUserTabs(SaveUserTabsRequestParam _objSaveUserTabsRequestParam, CancellationToken cancellationToken = default);

        #endregion [User Tabs]
    }
}
