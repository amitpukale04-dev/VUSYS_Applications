using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using System.Data;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class IStateResp : IState
    {
        private readonly DAC objDAC;

        public IStateResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get State List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<StateM>> GetStateList(int ID, string Type)
        {
            List<StateM> lstState = new List<StateM>();
            DataTable dt = new DataTable();
            try
            {
                dt = await GetState(ID, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IStateResp.GetStateList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstState = Helper.ConvertDataTableToList<StateM>(dt);
            }
            return lstState;
        }

        /// <summary>
        /// This Method is Used to Get State Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<StateM> GetStateDetails(int? id)
        {
            StateM _objStateM = new StateM();
            List<StateM> lstState = new List<StateM>();
            DataTable dt = new DataTable();
            try
            {
                string Type = "GetStateDetails";
                dt = await GetState((int)id, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IStateResp.GetStateDetails()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstState = Helper.ConvertDataTableToList<StateM>(dt);
                _objStateM = lstState[0];
            }
            return _objStateM;
        }


        /// <summary>
        /// This Method is Used to Get State List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<DataTable> GetState(int ID, string Type)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ID },
                    { "@Type", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetStateList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IStateResp.GetState()", ex.Message, false);
                throw;
            }
            return dt;
        }

        /// <summary>
        /// This Method is Used to Save Update State Details.
        /// </summary>
        /// <param name="_objStateM"></param>
        /// <returns></returns>
        public async Task<string> SaveUpdateStateDetails(StateM _objStateM)
        {
            string Message = string.Empty;
            DataSet ds = new DataSet();
            try
            {
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objStateM, "usp_VUSYS_SaveUpdateStateDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IStateResp.SaveUpdateStateDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }
    }
}