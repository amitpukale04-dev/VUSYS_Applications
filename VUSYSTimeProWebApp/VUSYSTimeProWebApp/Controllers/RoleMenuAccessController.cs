using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VUSYSTimeProWebApp.Contracts;
using CommonLib;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class RoleMenuAccessController : Controller
    {
        private readonly IRoleMenuAccess IRoleMenuAccessSvc;
        private readonly IRole IRoleSvc;
        private readonly IHttpContextAccessor httpContextAccessor;
        private AppDbContext db = null;

        public RoleMenuAccessController(IRoleMenuAccess roleMenuAccess, IRole role, IHttpContextAccessor httpContextAccessor, AppDbContext appDbContext)
        {
            IRoleMenuAccessSvc = roleMenuAccess;
            IRoleSvc = role;
            this.httpContextAccessor = httpContextAccessor;
            db = appDbContext;
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Role Menu Access Screen.
        /// </summary>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> RoleMenuAccess(int? RoleId)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }

                List<TreeViewNode> nodes = new List<TreeViewNode>();
                string sSubMenuParam = string.Empty;

                if (RoleId == null || RoleId == 0)
                {
                    ViewBag.RoleList = DropdownHelper.FillRoleList(db);
                    RoleId = 0;
                    sSubMenuParam = "LoadAllMenuAccessDetails";
                }
                else
                {
                    sSubMenuParam = "Load_MenuAccessDetails";
                }
                string sMenuMasterParam = "Load_MenuMaster";
                List<TreeViewNode> RoleMainMenuList = await GetMenuList(sMenuMasterParam, 0);
                List<TreeViewNode> RoleSubMenuList = await GetMenuList(sSubMenuParam, RoleId);
                nodes = GetMainAndSubMenuList(RoleMainMenuList, RoleSubMenuList);
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "RoleMenuSession", nodes);

                ViewBag.RoleID = 0;
                ViewBag.Json = JsonConvert.SerializeObject(nodes);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleMenuAccessController.RoleMenuAccess()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Get Role Menu Access Based on Role ID and Bind to TreeView.
        /// </summary>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> GetRoleMenuAccessForRoleId(int? RoleId)
        {
            List<TreeViewNode> nodes = new List<TreeViewNode>();
            try
            {
                string sSubMenuParam = string.Empty;
                if (RoleId == null || RoleId == 0)
                {
                    ViewBag.RoleList = DropdownHelper.FillRoleList(db);
                    RoleId = 0;
                    sSubMenuParam = "LoadAllMenuAccessDetails";
                }
                else
                {
                    sSubMenuParam = "Load_MenuAccessDetails";
                }

                string sMenuMasterParam = "Load_MenuMaster";
                List<TreeViewNode> RoleMainMenuList = await GetMenuList(sMenuMasterParam, 0);
                List<TreeViewNode> RoleSubMenuList = await GetMenuList(sSubMenuParam, RoleId);
                var value = HttpContext.Session.GetString("RoleMenuSession");
                var SessionTreeMenuList = JsonConvert.DeserializeObject<List<TreeViewNode>>(value);
                nodes = GetMainAndSubMenuList(RoleMainMenuList, RoleSubMenuList);
                nodes.AddRange(SessionTreeMenuList.Where(a => nodes.All(b => b.id != a.id)).ToList());
                nodes.Where(w => Convert.ToInt32(w.RoleId) > 0).ToList().ForEach(i => i.State.IsChecked = true);
                ViewBag.Json = JsonConvert.SerializeObject(nodes);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleMenuAccessController.GetRoleMenuAccessForRoleId()", ex.Message, false);
            }
            return Json(nodes);
        }

        /// <summary>
        /// This Post Action Method is Used to Save Role Menu Access.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <param name="selectedItems"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveRoleMenuAccess(AllViewModel allViewModel,string selectedItems)
        {
            try
            {
                if (selectedItems == null || selectedItems == string.Empty)
                {
                    return RedirectToAction("RoleMenuAccess");
                }
                else
                {
                    string Message = string.Empty;
                    List<TreeViewNode> items = JsonConvert.DeserializeObject<List<TreeViewNode>>(selectedItems);
                    items.RemoveAll(x => items.Any(y => x.parent == "#"));
                    int RoleID = Convert.ToInt32(items[0].RoleId);
                    Message = await IRoleMenuAccessSvc.SaveRoleMenuAccess(RoleID, items);
                    if (Message != string.Empty)
                    {
						TempData[Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY] = Message;
					}
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleMenuAccessController.SaveRoleMenuAccess()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return RedirectToAction("RoleMenuAccess");
        }

        #region [Custom Methods]

        /// <summary>
        /// This Method is Used to Get Menu List.
        /// </summary>
        /// <param name="Param"></param>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        public async Task<List<TreeViewNode>> GetMenuList(string Param, int? RoleId)
        {
            List<TreeViewNode> nodes = new List<TreeViewNode>();
            try
            {
                var MenuList = await IRoleMenuAccessSvc.GetRoleMenuAccess(Param, RoleId);
                nodes = MenuList.ToList();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleMenuAccessController.GetMenuList()", ex.Message, false);
                throw;
            }
            return nodes;
        }

        /// <summary>
        /// This Method is Used to Get Main and Sub Menu List.
        /// </summary>
        /// <param name="MainMenuList"></param>
        /// <param name="SubMenuList"></param>
        /// <returns></returns>
        public List<TreeViewNode> GetMainAndSubMenuList(List<TreeViewNode> MainMenuList, List<TreeViewNode> SubMenuList)
        {
            List<TreeViewNode> nodes = new List<TreeViewNode>();
            try
            {
                if (MainMenuList.Count > 0 && SubMenuList.Count > 0)
                {
                    foreach (var list in MainMenuList)
                    {
                        nodes.Add(new TreeViewNode
                        {
                            id = list.id.ToString(),
                            parent = list.parent.ToString(),
                            text = list.text,
                            RoleId = list.RoleId,
                            MainMenuId = list.MainMenuId.ToString(),
                            State = new TreeViewNodeState() { IsChecked = false, opened = true }
                        });
                    }

                    foreach (var Sublist in SubMenuList)
                    {
                        nodes.Add(new TreeViewNode
                        {
                            id = Sublist.parent.ToString() + "-" + Sublist.id.ToString(),
                            parent = Sublist.parent.ToString(),
                            text = Sublist.text.ToString(),
                            RoleId = Sublist.RoleId.ToString(),
                            MainMenuId = Sublist.MainMenuId,
                            State = new TreeViewNodeState() { IsChecked = false, opened = true }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleMenuAccessController.GetMainAndSubMenuList()", ex.Message, false);
                throw;
            }
            return nodes;
        }

        #endregion
    }
}