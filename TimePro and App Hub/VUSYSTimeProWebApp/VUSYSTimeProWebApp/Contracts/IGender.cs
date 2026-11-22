using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Contracts
{
	public interface IGender
	{
		/// <summary>
		/// This Method is Used to Get Gender List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Type"></param>
		/// <returns></returns>
		Task<List<GenderM>> GetGenderList(int ID, string Type);

		/// <summary>
		/// This Method is Used to Get Gender Details.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<GenderM> GetGenderDetails(int? id);
		
		/// <summary>
		/// This Method is Used to Save Update Gender Details.
		/// </summary>
		/// <param name="_objGenderM"></param>
		/// <returns></returns>
		Task<string> SaveUpdateGenderDetails(GenderM _objGenderM);
	}
}
