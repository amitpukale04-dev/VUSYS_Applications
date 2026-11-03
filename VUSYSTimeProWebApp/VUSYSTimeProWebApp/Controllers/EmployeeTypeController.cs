using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.Security;
using VUSYSTimeProWebApp.ViewModels;
using CommonLib;
using CommonLib.Constants;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class EmployeeTypeController : Controller
    {
        private AppDbContext db = null;
        private readonly IDataProtector protector;

        public EmployeeTypeController(AppDbContext appDbContext, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString)
        {
            db = appDbContext;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
        }


        /// <summary>
        /// This Get Action Method is Used to Invoke Employee Type List Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllEmployeeTypeList()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeTypeController.GetAllEmployeeTypeList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Method Is Used To Get Employee Type List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public IActionResult GetEmployeeTypeList()
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<EmployeeTypeDataList> actualdata = null;
            try
            {
                List<EmployeeTypeDataList> rList = null;
                var value = HttpContext.Session.GetString("EmployeeTypeList");
                if (value == null || value == "[]")
                {
                    rList = DropdownHelper.GetEmployeeTypeData(db, protector);

                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmployeeTypeList", rList);
                }
                else
                {
                    rList = JsonConvert.DeserializeObject<List<EmployeeTypeDataList>>(value);
                }
                if (rList != null)
                {
                    List<EmployeeTypeDataList> EmployeeTypeList = rList.ToList();
                    
                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "employeeType":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeTypeList = EmployeeTypeList.OrderBy(o => o.EmployeeType).ToList();
                                        break;
                                    case "desc":
                                        EmployeeTypeList = EmployeeTypeList.OrderByDescending(o => o.EmployeeType).ToList();
                                        break;
                                }
                                break;
                            case "activeStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeTypeList = EmployeeTypeList.OrderBy(o => o.ActiveStatus).ToList();
                                        break;
                                    case "desc":
                                        EmployeeTypeList = EmployeeTypeList.OrderByDescending(o => o.ActiveStatus).ToList();
                                        break;

                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeTypeList = EmployeeTypeList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        EmployeeTypeList = EmployeeTypeList.OrderByDescending(o => o.SLNo).ToList();
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
                        EmployeeTypeList = EmployeeTypeList.Where(e => e.EmployeeType.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = EmployeeTypeList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = EmployeeTypeList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = EmployeeTypeList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeTypeController.GetEmployeeTypeList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Create Employee Type Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public IActionResult CreateEmployeeType()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeTypeController.CreateEmployeeType()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Save Employee Type Details.
		/// </summary>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEmployeeType(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string Message = string.Empty;
                    string TempDataKey = string.Empty;
                    Message = SetAndSaveUpdateEmployeeTypeDetails(0, allViewModel, ref TempDataKey);
                    if (Message != string.Empty)
                    {
                        TempData[TempDataKey] = Message;
                        if (TempDataKey != Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY)
                        {
                            return View(allViewModel);
                        }
                    }
                }
                else
                {
                    return View(allViewModel);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeTypeController.CreateEmployeeType()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateEmployeeType");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Edit Employee Type Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> EditEmployeeType(string id, AllViewModel allViewModel)
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
                    var EmployeeTypeDetails = await db.EmployeeTypeMasterList.FirstOrDefaultAsync(p => p.Id == DecryptedID);
                    allViewModel.employeeTypeMaster = EmployeeTypeDetails;
                    ViewBag.IsVisible = true;
                    if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
                else
                {
                    return RedirectToAction("GetAllEmployeeTypeList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeTypeController.EditEmployeeType()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Update Employee Type Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEmployeeType(AllViewModel allViewModel)
        {
            if (allViewModel.employeeTypeMaster.Id == 0)
            {
                return PartialView("_Error500");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = string.Empty;
                    string TempDataKey = string.Empty;
                    Message = SetAndSaveUpdateEmployeeTypeDetails(allViewModel.employeeTypeMaster.Id, allViewModel, ref TempDataKey);
                    if (Message != string.Empty)
                    {
                        TempData[TempDataKey] = Message;
                        if (TempDataKey != Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY)
                        {
                            return View(allViewModel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeTypeController.EditEmployeeType()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            else
            {
                return View(allViewModel);
            }
            ModelState.Clear();
            return RedirectToAction("CreateEmployeeType");
        }

        #region [Custom Methods]

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Type Details.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="allViewModel"></param>
        /// <param name="TempDataKey"></param>
        /// <returns></returns>
		public string SetAndSaveUpdateEmployeeTypeDetails(int ID, AllViewModel allViewModel, ref string TempDataKey)
        {
            string Message = string.Empty;
            try
            {
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                if (allViewModel.employeeTypeMaster.Id == 0)
                {
                    var existingData = db.EmployeeTypeMasterList.FirstOrDefault(p => p.Id == allViewModel.employeeTypeMaster.Id);
                    if (existingData == null)
                    {
                        var mapping = new EmployeeTypeM
                        {
                            Id = allViewModel.employeeTypeMaster.Id,
                            EmployeeType = allViewModel.employeeTypeMaster.EmployeeType,
                            CreatedBy = SessionDetails.Id,
                            CreatedDate = DateTime.Now,
                            UpdatedBy = 0,
                            UpdatedDate = Convert.ToDateTime(DateTimeFormatConstants.DB_DEFAULT_DATETIME),
                            IsDeleted = false
                        };
                        db.EmployeeTypeMasterList.Add(mapping);
                        int affectedRows = db.SaveChanges();
                        Message = affectedRows > 0 ? "Employee Type Added Successfully." : "Failed To Add Employee Type";
                        TempDataKey = affectedRows > 0 ? Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY : Helper.TEMP_DATA_ERROR_MESSAGE_KEY;
                    }
                    else
                    {
                        Message = "The Employee Type Details Already Exists.";
                        TempDataKey = Helper.TEMP_DATA_EXIST_MESSAGE_KEY;
                    }
                }
                else
                {
                    var mapping = new EmployeeTypeM
                    {
                        Id = allViewModel.employeeTypeMaster.Id,
                        EmployeeType = allViewModel.employeeTypeMaster.EmployeeType,
                        CreatedBy = allViewModel.employeeTypeMaster.CreatedBy,
                        CreatedDate = Convert.ToDateTime(allViewModel.employeeTypeMaster.CreatedDate),
                        UpdatedBy = SessionDetails.Id,
                        UpdatedDate = DateTime.Now,
                        IsDeleted = (allViewModel.employeeTypeMaster.IsDeleted == true) ? true : false
                    };
                    db.EmployeeTypeMasterList.Update(mapping);
                    int affectedRows = db.SaveChanges();
                    Message = affectedRows > 0 ? "Employee Type Updated Successfully." : "Failed To Update Employee Type";
                    TempDataKey = affectedRows > 0 ? Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY : Helper.TEMP_DATA_ERROR_MESSAGE_KEY;
                }
                List<EmployeeTypeM> EmployeeTypeList = new List<EmployeeTypeM>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmployeeTypeList", EmployeeTypeList);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeTypeController.SetAndSaveUpdateEmployeeTypeDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        #endregion [Custom Methods]
    }
}
