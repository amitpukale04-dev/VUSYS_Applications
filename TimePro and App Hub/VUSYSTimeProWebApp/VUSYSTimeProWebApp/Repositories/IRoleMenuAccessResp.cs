using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using System.Data;
using System.Xml;
using System.Xml.Serialization;
using CommonLib;
using static CommonLib.CONST;

namespace VUSYSTimeProWebApp.Repositories
{
    public class IRoleMenuAccessResp:IRoleMenuAccess
    {
        private readonly DAC objDAC;

        public IRoleMenuAccessResp(DAC _DAC) : base()
        {
            objDAC = _DAC;
        }

        /// <summary>
        /// This Method is Used to Get Role Menu Access.
        /// </summary>
        /// <param name="sParam"></param>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TreeViewNode>> GetRoleMenuAccess(string sParam, int? RoleId)
        {
            List<TreeViewNode> lstTreeViewNode = new List<TreeViewNode>();
            DataTable dt = new DataTable();
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Param", sParam },
                    { "@RoleId", RoleId }
                };
                dt = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_RoleMenuAccess_GetData", parameters, CONST.DBRetType.DATATABLE) as DataTable;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMenuAccessResp.GetRoleMenuAccess()", ex.Message, false);
                throw;
            }
            if(dt.Rows.Count > 0)
            {
                lstTreeViewNode = Helper.ConvertDataTableToList<TreeViewNode>(dt);
            }
            return lstTreeViewNode;
        }

        /// <summary>
        /// This Method is Used to Save Role Menu Access.
        /// </summary>
        /// <param name="RoleID"></param>
        /// <param name="lsttreeViewNodes"></param>
        /// <returns></returns>
        public async Task<string> SaveRoleMenuAccess(int RoleID, List<TreeViewNode> lsttreeViewNodes)
        {
            string Message = string.Empty;
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Param", "Delete_MenuAccessDetailsByRoleId"},
                    { "@RoleId", RoleID},
                };
                var count = await objDAC.ExecuteStoredProcedureWithMultipleParametersAsync("usp_VUSYS_RoleMenuAccess_GetData", parameters, CONST.DBRetType.NONE);
                if (lsttreeViewNodes.Count > 0)
                {
                    Message = SaveMenuAccessDetails(lsttreeViewNodes);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMenuAccessResp.SaveRoleMenuAccess()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method is Used to Save Role Menu Access Details.
        /// </summary>
        /// <param name="lsttreeViewNodes"></param>
        /// <returns></returns>
        public string SaveMenuAccessDetails(List<TreeViewNode> lsttreeViewNodes)
        {
            string ReturnMessage = string.Empty;
            try
            {
                var ds = objDAC.ExecuteStoredProcedureWithXMLListParameters(lsttreeViewNodes, lsttreeViewNodes.GetType(), lsttreeViewNodes, "usp_VUSYS_MenuAccessDetails_Insert", Helper.XML_PARAMS, CONST.DBRetType.DATASET) as DataSet;
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    ReturnMessage = ds.Tables[0].Rows[0]["Result"].ToString();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMenuAccessResp.SaveMenuAccessDetails()", ex.Message, false);
                throw;

            }
            return ReturnMessage;
        }

        /// <summary>
        /// This Method is Used to Get XML String TreeViewNode List.
        /// </summary>
        /// <param name="ItemList"></param>
        /// <param name="xmlSerializerType"></param>
        /// <param name="List"></param>
        /// <returns></returns>
        public string GetXMLString(List<TreeViewNode> ItemList, Type xmlSerializerType, object List)
        {
            string xmlString = string.Empty;
            try
            {
                if (ItemList != null)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlSerializer xmlSerializer = new XmlSerializer(xmlSerializerType);

                    using (MemoryStream xmlStream = new MemoryStream())
                    {
                        xmlSerializer.Serialize(xmlStream, List);
                        xmlStream.Position = 0;
                        xmlDoc.Load(xmlStream);
                    }
                    xmlString = xmlDoc.InnerXml;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Repositories", "IRoleMenuAccessResp.GetXMLString()", ex.Message, false);

                throw;
            }
            return xmlString;
        }
    }
}
