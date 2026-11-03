using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
	public interface ICountry
	{
		/// <summary>
		/// This Method is Used to Get Country List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Type"></param>
		/// <returns></returns>
		Task<List<CountryM>> GetCountryList(int ID, string Type);

		/// <summary>
		/// This Method is Used to Get Country Details.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<CountryM> GetCountryDetails(int? id);

		/// <summary>
		/// This Method is Used to Save Update Country Details.
		/// </summary>
		/// <param name="_objCountryM"></param>
		/// <returns></returns>
		Task<string> SaveUpdateCountryDetails(CountryM _objCountryM);
	}
}
