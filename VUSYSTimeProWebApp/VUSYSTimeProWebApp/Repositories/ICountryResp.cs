using System.Data;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
	public class ICountryResp : ICountry
	{
        private readonly DAC objDAC;
        public ICountryResp(DAC _DAC) : base()
		{
            objDAC = _DAC;
        }
		/// <summary>
		/// This Method is Used to Get Country List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Type"></param>
		/// <returns></returns>
		public async Task<List<CountryM>> GetCountryList(int ID, string Type)
		{
			List<CountryM> lstCountrys = new List<CountryM>();
			DataTable dt = new DataTable();
			try
			{
				dt = await GetCountry(ID, Type);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ICountryResp.GetCountryList()", ex.Message, false);
				throw;
			}
			if (dt.Rows.Count > 0)
			{
				lstCountrys = Helper.ConvertDataTableToList<CountryM>(dt);
			}
			return lstCountrys;
		}

		/// <summary>
		/// This Method is Used to Get Country List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Type"></param>
		/// <returns></returns>
		public async Task<DataTable> GetCountry(int ID, string Type)
		{
			DataTable dt = new DataTable();
			try
			{
                var parameters = new Dictionary<string, object>
				{
					{ "@Id", ID },
					{ "@Type", Type }
				};

                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetCountryList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ICountryResp.GetCountry()", ex.Message, false);
				throw;
			}
			return dt;
		}
		/// <summary>
		/// This Method is Used to Get Country Details.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<CountryM> GetCountryDetails(int? id)
		{
			CountryM _objCountrysM = new CountryM();
			List<CountryM> lstCountrys = new List<CountryM>();
			DataTable dt = new DataTable();
			try
			{
				string Type = "GetCountryDetails";
				dt = await GetCountry((int)id, Type);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ICountryResp.GetCountryDetails()", ex.Message, false);
				throw;
			}
			if (dt.Rows.Count > 0)
			{
				lstCountrys = Helper.ConvertDataTableToList<CountryM>(dt);
				_objCountrysM = lstCountrys[0];
			}
			return _objCountrysM;
		}

		/// <summary>
		/// This Method is Used to Save Update Country Details.
		/// </summary>
		/// <param name="_objCountryM"></param>
		/// <returns></returns>
		public async Task<string> SaveUpdateCountryDetails(CountryM _objCountryM)
		{
			string Message = string.Empty;
			DataSet ds = new DataSet();
			try
			{
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objCountryM, "usp_VUSYS_SaveUpdateCountryDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "ICountryResp.SaveUpdateCountryDetails()", ex.Message, false);
				throw;
			}
			return Message;
		}
	}
}
