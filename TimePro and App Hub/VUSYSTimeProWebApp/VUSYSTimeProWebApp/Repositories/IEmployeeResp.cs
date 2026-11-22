using System.Data;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using CommonLib;

namespace VUSYSTimeProWebApp.Repositories
{
    public class IEmployeeResp:IEmployee
    {
        private readonly DAC objDAC;

        public IEmployeeResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get Employee List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<List<EmployeeM>> GetEmployeeList(int ID, string Type)
        {
            List<EmployeeM> lstEmployee = new List<EmployeeM>();
            DataTable dt = new DataTable();
            try
            {
                dt = await GetEmployee(ID, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmployeeResp.GetEmployeeList()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstEmployee = Helper.ConvertDataTableToList<EmployeeM>(dt);
            }
            return lstEmployee;
        }

        /// <summary>
        /// This Method is Used to Get Employee Details.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<EmployeeM> GetEmployeeDetails(int? id)
        {
            EmployeeM _objEmployeeM = new EmployeeM();
            List<EmployeeM> lstEmployeeM = new List<EmployeeM>();
            DataTable dt = new DataTable();
            try
            {
                string Type = "GetEmployeeDetails";
                dt = await GetEmployee((int)id, Type);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmployeeResp.GetEmployeeDetails()", ex.Message, false);
                throw;
            }
            if (dt.Rows.Count > 0)
            {
                lstEmployeeM = Helper.ConvertDataTableToList<EmployeeM>(dt);
                _objEmployeeM = lstEmployeeM[0];
            }
            return _objEmployeeM;
        }

        /// <summary>
        /// This Method is Used to Get Employee List.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public async Task<DataTable> GetEmployee(int ID, string Type)
        {
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Id", ID },
                    { "@Type", Type }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_GetEmployeeList", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmployeeResp.GetEmployee()", ex.Message, false);
                throw;
            }
            return dt;
        }

        /// <summary>
        /// This Method is Used to Save Update Employee Details.
        /// </summary>
        /// <param name="_objEmployeeM"></param>
        /// <returns></returns>
        public async Task<string> SaveUpdateEmployeeDetails(EmployeeM _objEmployeeM)
        {
            string Message = string.Empty;
            DataSet ds = new DataSet();
            try
            {
                ds = await objDAC.ExecuteStoredProcedureWithXMLParametersAsync(_objEmployeeM, "usp_VUSYS_SaveUpdateEmployeeDetails", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Message = ds.Tables[0].Rows[0]["Message"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IEmployeeResp.SaveUpdateEmployeeDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }
    }
}
