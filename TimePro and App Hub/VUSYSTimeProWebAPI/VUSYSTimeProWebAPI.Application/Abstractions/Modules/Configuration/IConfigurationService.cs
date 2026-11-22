using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Configuration
{
    public interface IConfigurationService
    {
        #region [Email Configuration]

        /// <summary>
        /// This Method Is Used To Save Or Update Email Configuration Details.
        /// </summary>
        /// <param name="_objConfigureEmailM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateEmailConfigurationDetails(ConfigureEmailM _objConfigureEmailM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Email Configuration Detail Or List.
        /// </summary>
        /// <param name="_objConfigureEmailDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetEmailConfigurationDetailOrList(ConfigureEmailDetailsRequestParam _objConfigureEmailDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Email Configuration]
    }
}