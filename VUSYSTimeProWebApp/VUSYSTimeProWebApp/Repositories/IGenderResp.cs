using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using System.Data;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
	public class IGenderResp : IGender
	{
        private readonly DAC objDAC;

        public IGenderResp(DAC _DAC) : base()
		{
            objDAC = _DAC;
        }

		/// <summary>
		/// This Method is Used to Get Gender List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Type"></param>
		/// <returns></returns>
		public async Task<List<GenderM>> GetGenderList(int ID, string Type)
		{
			List<GenderM> lstGender = new List<GenderM>();
			DataTable dt = new DataTable();
			try
			{
				dt = await GetGender(ID, Type);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IGenderResp.GetGenderList()", ex.Message, false);
				throw;
			}
			if (dt.Rows.Count > 0)
			{
				lstGender = Helper.ConvertDataTableToList<GenderM>(dt);
			}
			return lstGender;
		}

		/// <summary>
		/// This Method is Used to Get Gender Details.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<GenderM> GetGenderDetails(int? id)
		{
			GenderM _objGenderM = new GenderM();
			List<GenderM> lstGender = new List<GenderM>();
			DataTable dt = new DataTable();
			try
			{
				string Type = "GetGenderDetails";
				dt = await GetGender((int)id, Type);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IGenderResp.GetGenderDetails()", ex.Message, false);
				throw;
			}
			if (dt.Rows.Count > 0)
			{
				lstGender = Helper.ConvertDataTableToList<GenderM>(dt);
				_objGenderM = lstGender[0];
			}
			return _objGenderM;
		}


		/// <summary>
		/// This Method is Used to Get Gender List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Type"></param>
		/// <returns></returns>
		public async Task<DataTable> GetGender(int ID, string Type)
		{
			DataTable dt = new DataTable();
			try
			{
                var parameters = new Dictionary<string, object>
				{
					{ "@Id", ID },
					{ "@Type", Type }
				};
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetGenderList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IGenderResp.GetGender()", ex.Message, false);
				throw;
			}
			return dt;
		}

		/// <summary>
		/// This Method is Used to Save Update Gender Details.
		/// </summary>
		/// <param name="_objGenderM"></param>
		/// <returns></returns>
		public async Task<string> SaveUpdateGenderDetails(GenderM _objGenderM)
		{
			string Message = string.Empty;
			DataSet ds = new DataSet();
			try
			{
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objGenderM, "usp_VUSYS_SaveUpdateGenderDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IGenderResp.SaveUpdateGenderDetails()", ex.Message, false);
				throw;
			}
			return Message;
		}
	}
}