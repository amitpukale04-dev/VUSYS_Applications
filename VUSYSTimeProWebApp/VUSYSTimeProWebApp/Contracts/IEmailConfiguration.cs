using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
    public interface IEmailConfiguration
    {
		/// <summary>
		/// This Method is Used to Get Email Configuration List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Status"></param>
		/// <returns></returns>
		Task<List<ConfigureEmailM>> GetEmailConfigurationList(int ID, string Status);

		/// <summary>
		/// This Method is Used to Get Email Configuration Details.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<ConfigureEmailM> GetEmailConfigurationDetails(int? id);

		/// <summary>
		/// This Method is Used to Save Update Email Configuration Details.
		/// </summary>
		/// <param name="_objConfigureEmailM"></param>
		/// <returns></returns>
		Task<string> SaveAndUpdateEmailConfigurationDetails(ConfigureEmailM _objConfigureEmailM);
    }
}
