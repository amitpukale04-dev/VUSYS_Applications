using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using System.Data;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class IEmailConfigurationResp : IEmailConfiguration
    {
        private readonly DAC objDAC;

        public IEmailConfigurationResp(DAC _DAC) : base()
		{
            objDAC = _DAC;
        }

		/// <summary>
		/// This Method is Used to Get Email Configuration List.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Status"></param>
		/// <returns></returns>
		public async Task<List<ConfigureEmailM>> GetEmailConfigurationList(int ID, string Status)
		{
			List<ConfigureEmailM> lstConfigureEmail = new List<ConfigureEmailM>();
			DataTable dt = new DataTable();
			try
			{
				dt = await GetEmailConfiguration(ID, Status);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmailConfigurationResp.GetEmailConfigurationList()", ex.Message, false);
				throw;
			}
			if (dt.Rows.Count > 0)
			{
				lstConfigureEmail = Helper.ConvertDataTableToList<ConfigureEmailM>(dt);
			}
			return lstConfigureEmail;
		}

		/// <summary>
		/// This Method is Used to Get Email Configuration Details.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ConfigureEmailM> GetEmailConfigurationDetails(int? id)
		{
			ConfigureEmailM _objConfigureEmailM = new ConfigureEmailM();
			List<ConfigureEmailM> lstConfigureEmail = new List<ConfigureEmailM>();
			DataTable dt = new DataTable();
			try
			{
				string Type = "GetEmailConfigurationDetails";
				dt = await GetEmailConfiguration((int)id, Type);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmailConfigurationResp.GetEmailConfigurationDetails()", ex.Message, false);
				throw;
			}
			if (dt.Rows.Count > 0)
			{
				lstConfigureEmail = Helper.ConvertDataTableToList<ConfigureEmailM>(dt);
				_objConfigureEmailM = lstConfigureEmail[0];
			}
			return _objConfigureEmailM;
		}


		/// <summary>
		/// This Method is Used to Get Email Configuration.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="Type"></param>
		/// <returns></returns>
		public async Task<DataTable> GetEmailConfiguration(int ID, string Type)
		{
			DataTable dt = new DataTable();
			try
			{
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ID },
                    { "@Type", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetEmailConfigurationList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmailConfigurationResp.GetEmailConfiguration()", ex.Message, false);
				throw;
			}
			return dt;
		}

		/// <summary>
		/// This Method is Used to Save Update Email Configuration Details.
		/// </summary>
		/// <param name="_objConfigureEmailM"></param>
		/// <returns></returns>
		public async Task<string> SaveAndUpdateEmailConfigurationDetails(ConfigureEmailM _objConfigureEmailM)
		{
			string Message = string.Empty;
			DataSet ds = new DataSet();
			try
			{
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objConfigureEmailM, "usp_VUSYS_Save_UpdateEmailConfigurationDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmailConfigurationResp.SaveAndUpdateEmailConfigurationDetails()", ex.Message, false);
				throw;
			}
			return Message;
		}
	}
}
