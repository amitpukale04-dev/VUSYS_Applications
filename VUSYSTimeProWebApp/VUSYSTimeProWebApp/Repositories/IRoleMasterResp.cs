using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using System.Data;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class IRoleMasterResp : IRole
    {
        private readonly DAC objDAC;
        public IRoleMasterResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get Role List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<RolesM>> GetRoleList(int ID, string Type)
        {
            List<RolesM> lstRoles = new List<RolesM>();
            DataTable dt = new DataTable();
            try
            {
                dt = await GetRole(ID, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMasterResp.GetRoleList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstRoles = Helper.ConvertDataTableToList<RolesM>(dt);
            }
            return lstRoles;
        }

        /// <summary>
        /// This Method is Used to Get Role Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RolesM> GetRoleDetails(int? id)
        {
            RolesM _objRolesM = new RolesM();
            List<RolesM> lstRoles = new List<RolesM>();
            DataTable dt = new DataTable();
            try
            {
                string Type = "GetRoleDetails";
                dt = await GetRole((int)id, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMasterResp.GetRoleDetails()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstRoles = Helper.ConvertDataTableToList<RolesM>(dt);
                _objRolesM = lstRoles[0];
            }
            return _objRolesM;
        }


        /// <summary>
        /// This Method is Used to Get Role List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<DataTable> GetRole(int ID, string Type)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ID },
                    { "@Type", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetRoleList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMasterResp.GetRole()", ex.Message, false);
                throw;
            }
            return dt;
        }

        /// <summary>
        /// This Method is Used to Save Update Role Details.
        /// </summary>
        /// <param name="_objRolesM"></param>
        /// <returns></returns>
        public async Task<string> SaveUpdateRoleDetails(RolesM _objRolesM)
        {
            string Message = string.Empty;
            DataSet ds = new DataSet();
            try
            {
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objRolesM, "usp_VUSYS_SaveUpdateRoleDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMasterResp.SaveUpdateRoleDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }
    }
}