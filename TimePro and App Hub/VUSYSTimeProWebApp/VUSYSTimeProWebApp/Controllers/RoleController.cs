using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.Security;
using VUSYSTimeProWebApp.ViewModels;
using CommonLib;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class RoleController : Controller
    {
        private readonly IRole IRoleSvc = null;
        private readonly IDataProtector protector;

        public RoleController(IRole Role, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString)
        {
            this.IRoleSvc = Role;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Role List Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllRoleList()
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleController.GetAllRoleList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Method Is Used To Get Role List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetRoleList()
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<RolesM> actualdata = null;
            try
            {
                int ID = 0;
                List<RolesM> rList = null;
                var value = HttpContext.Session.GetString("RoleList");
                if (value == null || value == "[]")
                {
                    rList = await IRoleSvc.GetRoleList(ID, "GetAllRoleList");
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "RoleList", rList);
                }
                else
                {
                    rList = JsonConvert.DeserializeObject<List<RolesM>>(value);
                }
                if (rList != null)
                {
                    List<RolesM> RoleList = rList.ToList();
                    RoleList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.Id.ToString()));

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "roleName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        RoleList = RoleList.OrderBy(o => o.RoleName).ToList();
                                        break;
                                    case "desc":
                                        RoleList = RoleList.OrderByDescending(o => o.RoleName).ToList();
                                        break;
                                }
                                break;
                            case "activeStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        RoleList = RoleList.OrderBy(o => o.ActiveStatus).ToList();
                                        break;
                                    case "desc":
                                        RoleList = RoleList.OrderByDescending(o => o.ActiveStatus).ToList();
                                        break;

                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        RoleList = RoleList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        RoleList = RoleList.OrderByDescending(o => o.SLNo).ToList();
                                        break;
                                }
                                break;
                        }
                    }
                    #endregion

                    #region Search Functionality           

                    var searchValue = Request.Form["search[value]"].FirstOrDefault();

                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        RoleList = RoleList.Where(e => e.RoleName.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = RoleList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = RoleList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = RoleList.Skip(skip).Take(recordsTotal).ToList();
                    }

                    #endregion
                    return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = actualdata });
                }
                else
                {
                    return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = actualdata });
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleController.GetRoleList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Create Role Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public IActionResult CreateRole()
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleController.CreateRole()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Save Role Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    allViewModel.RoleMaster.Id = 0;
                    string Message = await SetAndSaveUpdateRoleDetails(allViewModel.RoleMaster.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Role Details Already Exists.")
                        {
                            TempData[Helper.TEMP_DATA_EXIST_MESSAGE_KEY] = Message;
                            return View(allViewModel);
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY] = Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleController.CreateRole()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateRole");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Edit Role Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> EditRole(string id, AllViewModel allViewModel)
        {
            try
            {
                if (id != null || id != string.Empty)
                {
                    int DecryptedID = Convert.ToInt32(protector.Unprotect(id));
                    bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                    if (IsAuthorize == false)
                    {
                        return PartialView("_AccessDenied");
                    }
                    var RoleDetails = await IRoleSvc.GetRoleDetails(DecryptedID);
                    allViewModel.RoleMaster = RoleDetails;
                    RoleDetails.EncryptedId = protector.Protect(id.ToString());
                    allViewModel.RoleMaster = RoleDetails;
                    ViewBag.IsVisible = true;
                    ViewBag.RoleID = Convert.ToInt32(DecryptedID);
                    if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
                else
                {
                    return RedirectToAction("GetAllRoleList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleController.EditRole()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Update Role Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(AllViewModel allViewModel)
        {
            if (allViewModel.RoleMaster.Id == 0)
            {
                return PartialView("_Error500");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = await SetAndSaveUpdateRoleDetails(allViewModel.RoleMaster.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Role Details Already Exists.")
                        {
                            TempData[Helper.TEMP_DATA_EXIST_MESSAGE_KEY] = Message;
                            return View(allViewModel);
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY] = Message;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleController.EditRole()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            else
            {
                return View(allViewModel);
            }
            ModelState.Clear();
            return RedirectToAction("CreateRole");
        }

        #region [Custom Methods]

        /// <summary>
		/// This Method Is Used To Save Or Update Role Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateRoleDetails(int ID, AllViewModel allViewModel)
        {
            string Message = string.Empty;
            try
            {
                allViewModel.RoleMaster.Id = ID;
                allViewModel.RoleMaster.IsDeleted = (allViewModel.RoleMaster.IsDeleted == true) ? true : false;
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                allViewModel.RoleMaster.CreatedBy = SessionDetails.Id;
                allViewModel.RoleMaster.UpdatedBy = SessionDetails.Id;
                allViewModel.RoleMaster.Status = "ADD";
                Message = await IRoleSvc.SaveUpdateRoleDetails(allViewModel.RoleMaster);
                List<RolesM> RoleList = new List<RolesM>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "RoleList", RoleList);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "RoleController.SetAndSaveUpdateRoleDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        #endregion [Custom Methods]
    }
}
