using CommonLib;
using CommonLib.Constants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.Security;
using VUSYSTimeProWebApp.ViewModels;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class ProjectController : Controller
    {
        private AppDbContext db = null;
        private readonly IProject IProjectSvc = null;
        private readonly IDataProtector protector;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ProjectController(AppDbContext appDbContext, IWebHostEnvironment _webHostEnvironment,
            IProject Project, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString)
        {
            db = appDbContext;
            webHostEnvironment = _webHostEnvironment;
            this.IProjectSvc = Project;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
        }


        #region [Project Master]

        /// <summary>
        /// This Get Action Method is Used to Invoke Project List Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllProjectList()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.GetAllProjectList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Method Is Used To Get Project List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetProjectList()
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<ProjectM> actualdata = null;
            try
            {
                int ID = 0;
                List<ProjectM> rList = null;
                var value = HttpContext.Session.GetString("ProjectList");
                if (value == null || value == "[]")
                {
                    rList = await IProjectSvc.GetProjectList(ID, "GetAllProjectList");
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "ProjectList", rList);
                }
                else
                {
                    rList = JsonConvert.DeserializeObject<List<ProjectM>>(value);
                }
                if (rList != null)
                {
                    List<ProjectM> ProjectList = rList.ToList();
                    ProjectList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.ProjectID.ToString()));
                    ProjectList.ToList().ForEach(u =>
                    {
                        // Convert the integer value of ProjectStatus to the corresponding enum string
                        string statusName = Enum.GetName(typeof(ProjectStatus), Convert.ToInt32(u.ProjectStatus));

                        // Replace underscores with spaces
                        u.ProjectStatus = statusName.Replace("_", " ");
                    });

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "projectCode":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.ProjectCode).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.ProjectCode).ToList();
                                        break;
                                }
                                break;
                            case "clientName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.ClientName).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.ClientName).ToList();
                                        break;
                                }
                                break;
                            case "projectName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.ProjectName).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.ProjectName).ToList();
                                        break;
                                }
                                break;
                            case "displayStartDate":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.DisplayStartDate).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.DisplayStartDate).ToList();
                                        break;
                                }
                                break;
                            case "displayEndDate":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.DisplayEndDate).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.DisplayEndDate).ToList();
                                        break;
                                }
                                break;
                            case "projectStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.ProjectStatus).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.ProjectStatus).ToList();
                                        break;
                                }
                                break;
                            case "activeStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.ActiveStatus).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.ActiveStatus).ToList();
                                        break;

                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectList = ProjectList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        ProjectList = ProjectList.OrderByDescending(o => o.SLNo).ToList();
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
                        ProjectList = ProjectList.Where(e => e.ProjectCode.Contains(searchValue) || e.ClientName.Contains(searchValue) || e.ProjectName.Contains(searchValue) || e.DisplayStartDate.Contains(searchValue) || e.DisplayEndDate.Contains(searchValue) || e.ProjectStatus.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = ProjectList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = ProjectList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = ProjectList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.GetProjectList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Create Project Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public IActionResult CreateProject()
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                SetDropDownDataInViewBag(true);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.CreateProject()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Save Project Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    allViewModel.ProjectMaster.ProjectID = 0;
                    string Message = await SetAndSaveUpdateProjectDetails(allViewModel.ProjectMaster.ProjectID, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Project Details Already Exists.")
                        {
                            TempData[Helper.TEMP_DATA_EXIST_MESSAGE_KEY] = Message;
                            SetDropDownDataInViewBag(true);
                            return View(allViewModel);
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY] = Message;
                        }
                    }
                }
                else
                {
                    SetDropDownDataInViewBag(true);
                    return View(allViewModel);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.CreateProject()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateProject");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Edit Project Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> EditProject(string id, AllViewModel allViewModel)
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
                    var ProjectDetails = await IProjectSvc.GetProjectDetails(DecryptedID);
                    allViewModel.ProjectMaster = ProjectDetails;
                    ProjectDetails.EncryptedId = protector.Protect(id.ToString());
                    allViewModel.ProjectMaster = ProjectDetails;
                    ViewBag.IsVisible = true;
                    ViewBag.ProjectID = Convert.ToInt32(DecryptedID);
                    SetDropDownDataInViewBag(true);
                    if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
                else
                {
                    return RedirectToAction("GetAllProjectList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.EditProject()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Update Project Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(AllViewModel allViewModel)
        {
            if (allViewModel.ProjectMaster.ProjectID == 0)
            {
                return PartialView("_Error500");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = await SetAndSaveUpdateProjectDetails(allViewModel.ProjectMaster.ProjectID, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Project Details Already Exists.")
                        {
                            TempData[Helper.TEMP_DATA_EXIST_MESSAGE_KEY] = Message;
                            SetDropDownDataInViewBag(true);
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
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.EditProject()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            else
            {
                SetDropDownDataInViewBag(true);
                return View(allViewModel);
            }
            ModelState.Clear();
            return RedirectToAction("CreateProject");
        }

        #endregion [Project Master]

        #region [Employee Project Mapping]

        /// <summary>
        /// This Get Action Method is Used to Invoke Project List Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllEmployeeProjectMappingList()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.GetAllEmployeeProjectMappingList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Method Is Used To Get Employee Project Mapping List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public IActionResult GetEmployeeProjectMappingList()
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<EmployeeProjectMappingDataList> actualdata = null;
            try
            {
                List<EmployeeProjectMappingDataList> rList = null;
                var value = HttpContext.Session.GetString("EmployeeProjectMappingList");
                if (value == null || value == "[]")
                {
                    rList = DropdownHelper.GetEmployeeProjectMappingData(db, protector);

                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmployeeProjectMappingList", rList);
                }
                else
                {
                    rList = JsonConvert.DeserializeObject<List<EmployeeProjectMappingDataList>>(value);
                }
                if (rList != null)
                {
                    List<EmployeeProjectMappingDataList> EmployeeProjectMappingList = rList.ToList();
                    
                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "projectCode":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderBy(o => o.ProjectCode).ToList();
                                        break;
                                    case "desc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderByDescending(o => o.ProjectCode).ToList();
                                        break;
                                }
                                break;
                            case "projectName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderBy(o => o.ProjectName).ToList();
                                        break;
                                    case "desc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderByDescending(o => o.ProjectName).ToList();
                                        break;
                                }
                                break;
                            case "employeeCode":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderBy(o => o.EmployeeCode).ToList();
                                        break;
                                    case "desc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderByDescending(o => o.EmployeeCode).ToList();
                                        break;
                                }
                                break;
                            case "employeeName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderBy(o => o.EmployeeName).ToList();
                                        break;
                                    case "desc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderByDescending(o => o.EmployeeName).ToList();
                                        break;
                                }
                                break;
                            case "activeStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderBy(o => o.ActiveStatus).ToList();
                                        break;
                                    case "desc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderByDescending(o => o.ActiveStatus).ToList();
                                        break;

                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        EmployeeProjectMappingList = EmployeeProjectMappingList.OrderByDescending(o => o.SLNo).ToList();
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
                        EmployeeProjectMappingList = EmployeeProjectMappingList.Where(e => e.ProjectCode.Contains(searchValue) || e.ProjectName.Contains(searchValue) || e.EmployeeCode.Contains(searchValue) || e.EmployeeName.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = EmployeeProjectMappingList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = EmployeeProjectMappingList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = EmployeeProjectMappingList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.GetEmployeeProjectMappingList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Create Employee Project Mapping Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public IActionResult CreateEmployeeProjectMapping()
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                ViewBag.IsNotifyEmployee = true;
                SetDropDownDataInViewBag(false);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.CreateEmployeeProjectMapping()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Save Employee Project Mapping Details.
		/// </summary>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEmployeeProjectMapping(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string Message = string.Empty;
                    string TempDataKey = string.Empty;
                    Message = SetAndSaveUpdateEmployeeProjectMappingDetails(0, allViewModel, ref TempDataKey);
                    if (Message != string.Empty)
                    {
                        TempData[TempDataKey] = Message;
                        if (TempDataKey != Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY)
                        {
                            SetDropDownDataInViewBag(false);
                            return View(allViewModel);
                        }
                    }
                    SetDropDownDataInViewBag(false);
                }
                else
                {
                    SetDropDownDataInViewBag(false);
                    return View(allViewModel);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.CreateEmployeeProjectMapping()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateEmployeeProjectMapping");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Edit Employee Project Mapping Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> EditEmployeeProjectMapping(string id, AllViewModel allViewModel)
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
                    var EmployeeProjectDetails = await db.EmployeeProjectMappingList.FirstOrDefaultAsync(p => p.Id == DecryptedID);
                    allViewModel.employeeProjectMapping = EmployeeProjectDetails;
                    EmployeeProjectDetails.IsNotifyToEmployee = false;
                    allViewModel.employeeProjectMapping = EmployeeProjectDetails;
                    ViewBag.IsVisible = true;
                    SetDropDownDataInViewBag(false);
                    if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
                else
                {
                    return RedirectToAction("GetAllEmployeeProjectMappingList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.EditEmployeeProjectMapping()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Update Employee Project Mapping Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEmployeeProjectMapping(AllViewModel allViewModel)
        {
            if (allViewModel.employeeProjectMapping.Id == 0)
            {
                return PartialView("_Error500");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = string.Empty;
                    string TempDataKey = string.Empty;
                    Message = SetAndSaveUpdateEmployeeProjectMappingDetails(allViewModel.employeeProjectMapping.Id, allViewModel, ref TempDataKey);
                    if (Message != string.Empty)
                    {
                        TempData[TempDataKey] = Message;
                        if (TempDataKey != Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY)
                        {
                            SetDropDownDataInViewBag(false);
                            return View(allViewModel);
                        }
                    }
                    SetDropDownDataInViewBag(false);
                }
                catch (Exception ex)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.EditProject()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            else
            {
                SetDropDownDataInViewBag(true);
                return View(allViewModel);
            }
            ModelState.Clear();
            return RedirectToAction("CreateEmployeeProjectMapping");
        }


        #endregion [Employee Project Mapping]

        #region [Project Activity]

        /// <summary>
        /// This Get Action Method is Used to Invoke Project Activity List Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllProjectActivityList()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ProjectController.GetAllProjectActivityList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Method Is Used To Get ProjectActivity List and Display In The Grid.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetProjectActivityList()
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<ProjectActivityM> actualdata = null;
            try
            {
                int ID = 0;
                List<ProjectActivityM> rList = null;
                var value = HttpContext.Session.GetString("ProjectActivityList");
                if (value == null || value == "[]")
                {
                    rList = await IProjectSvc.GetProjectActivityList(ID, "GetAllProjectActivityList");
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "ProjectActivityList", rList);
                }
                else
                {
                    rList = JsonConvert.DeserializeObject<List<ProjectActivityM>>(value);
                }
                if (rList != null)
                {
                    List<ProjectActivityM> ProjectActivityList = rList.ToList();
                    ProjectActivityList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.Id.ToString()));

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "activityName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectActivityList = ProjectActivityList.OrderBy(o => o.ActivityName).ToList();
                                        break;
                                    case "desc":
                                        ProjectActivityList = ProjectActivityList.OrderByDescending(o => o.ActivityName).ToList();
                                        break;
                                }
                                break;
                            case "activeStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectActivityList = ProjectActivityList.OrderBy(o => o.ActiveStatus).ToList();
                                        break;
                                    case "desc":
                                        ProjectActivityList = ProjectActivityList.OrderByDescending(o => o.ActiveStatus).ToList();
                                        break;

                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectActivityList = ProjectActivityList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        ProjectActivityList = ProjectActivityList.OrderByDescending(o => o.SLNo).ToList();
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
                        ProjectActivityList = ProjectActivityList.Where(e => e.ActivityName.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = ProjectActivityList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = ProjectActivityList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = ProjectActivityList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ProjectController.GetProjectActivityList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Create Project Activity Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CreateProjectActivity()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ProjectController.CreateProjectActivity()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Save Project Activity Details.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProjectActivity(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    allViewModel.ProjectActivity.Id = 0;
                    string Message = await SetAndSaveUpdateProjectActivityDetails(allViewModel.ProjectActivity.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Project Activity Already Exists.")
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ProjectController.CreateProjectActivity()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateProjectActivity");
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Edit Project Activity Screen.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EditProjectActivity(string id, AllViewModel allViewModel)
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
                    var ProjectActivityDetails = await IProjectSvc.GetProjectActivityDetails(DecryptedID);
                    allViewModel.ProjectActivity = ProjectActivityDetails;
                    ProjectActivityDetails.EncryptedId = protector.Protect(id.ToString());
                    allViewModel.ProjectActivity = ProjectActivityDetails;
                    ViewBag.IsVisible = true;
                    ViewBag.ProjectActivityID = Convert.ToInt32(DecryptedID);
                    if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
                else
                {
                    return RedirectToAction("GetAllProjectActivityList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ProjectController.EditProjectActivity()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Update Project Activity Details.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProjectActivity(AllViewModel allViewModel)
        {
            if (allViewModel.ProjectActivity.Id == 0)
            {
                return PartialView("_Error500");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = await SetAndSaveUpdateProjectActivityDetails(allViewModel.ProjectActivity.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Project Activity Already Exists.")
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
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ProjectController.EditProjectActivity()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            else
            {
                return View(allViewModel);
            }
            ModelState.Clear();
            return RedirectToAction("CreateProjectActivity");
        }


        #endregion [Project Activity]

        #region [Custom Methods]

        /// <summary>
        /// This Method Is Used To Set DropDown Data In View Bag.
        /// </summary>
        /// <param name="IsProjectMaster"></param>
        public void SetDropDownDataInViewBag(bool IsProjectMaster)
        {
            try
            {
                if (IsProjectMaster)
                {
                    ViewBag.ProjectStatusList = DropdownHelper.FillProjectStatusList();
                }
                else
                {
                    ViewBag.ProjectList = DropdownHelper.FillProjectList(db);
                    ViewBag.EmployeeList = DropdownHelper.FillEmployeeList(db);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.SetDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
		/// This Method Is Used To Save Or Update Project Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateProjectDetails(int ID, AllViewModel allViewModel)
        {
            string Message = string.Empty;
            try
            {
                allViewModel.ProjectMaster.ProjectID = ID;
                allViewModel.ProjectMaster.IsDeleted = (allViewModel.ProjectMaster.IsDeleted == true) ? true : false;
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                allViewModel.ProjectMaster.CreatedBy = SessionDetails.Id;
                allViewModel.ProjectMaster.UpdatedBy = SessionDetails.Id;
                allViewModel.ProjectMaster.Status = "ADD";
                Message = await IProjectSvc.SaveUpdateProjectDetails(allViewModel.ProjectMaster);
                List<ProjectM> ProjectList = new List<ProjectM>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "ProjectList", ProjectList);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.SetAndSaveUpdateProjectDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
        /// This Method Is Used To Save Or Update Employee Project Mapping Details.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="allViewModel"></param>
        /// <param name="TempDataKey"></param>
        /// <returns></returns>
		public string SetAndSaveUpdateEmployeeProjectMappingDetails(int ID, AllViewModel allViewModel, ref string TempDataKey)
        {
            string Message = string.Empty;
            try
            {
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                if (allViewModel.employeeProjectMapping.Id == 0)
                {
                    var existingData = db.EmployeeProjectMappingList.FirstOrDefault(p => p.ProjectID == allViewModel.employeeProjectMapping.ProjectID && p.EmployeeCode == allViewModel.employeeProjectMapping.EmployeeCode);
                    if (existingData == null)
                    {
                        var mapping = new EmployeeProjectMappingM
                        {
                            Id = allViewModel.employeeProjectMapping.Id,
                            EmployeeCode = allViewModel.employeeProjectMapping.EmployeeCode,
                            ProjectID = allViewModel.employeeProjectMapping.ProjectID,
                            IsNotifyToEmployee = (allViewModel.employeeProjectMapping.IsNotifyToEmployee == true) ? true : false,
                            CreatedBy = SessionDetails.Id,
                            CreatedDate = DateTime.Now,
                            UpdatedBy = 0,
                            UpdatedDate = Convert.ToDateTime(DateTimeFormatConstants.DB_DEFAULT_DATETIME),
                            IsDeleted = false
                        };
                        db.EmployeeProjectMappingList.Add(mapping);
                        int affectedRows = db.SaveChanges();
                        Message = affectedRows > 0 ? "Employee Project Mapping Added Successfully." : "Failed To Add Employee Project Mapping";
                        TempDataKey = affectedRows > 0 ? Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY : Helper.TEMP_DATA_ERROR_MESSAGE_KEY;
                    }
                    else
                    {
                        Message = "The Employee Project Mapping Details Already Exists.";
                        TempDataKey = Helper.TEMP_DATA_EXIST_MESSAGE_KEY;
                    }
                }
                else
                {
                    var mapping = new EmployeeProjectMappingM
                    {
                        Id = allViewModel.employeeProjectMapping.Id,
                        EmployeeCode = allViewModel.employeeProjectMapping.EmployeeCode,
                        ProjectID = allViewModel.employeeProjectMapping.ProjectID,
                        IsNotifyToEmployee = (allViewModel.employeeProjectMapping.IsNotifyToEmployee == true) ? true : false,
                        CreatedBy = allViewModel.employeeProjectMapping.CreatedBy,
                        CreatedDate = Convert.ToDateTime(allViewModel.employeeProjectMapping.CreatedDate),
                        UpdatedBy = SessionDetails.Id,
                        UpdatedDate = DateTime.Now,
                        IsDeleted = (allViewModel.employeeProjectMapping.IsDeleted == true) ? true : false
                    };
                    db.EmployeeProjectMappingList.Update(mapping);
                    int affectedRows = db.SaveChanges();
                    Message = affectedRows > 0 ? "Employee Project Mapping Updated Successfully." : "Failed To Update Employee Project Mapping";
                    TempDataKey = affectedRows > 0 ? Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY : Helper.TEMP_DATA_ERROR_MESSAGE_KEY;
                }
                if(allViewModel.employeeProjectMapping.IsNotifyToEmployee && TempDataKey == Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY)
                {
                    var List = DropdownHelper.GetEmployeeProjectMappingData(db, protector, allViewModel.employeeProjectMapping.ProjectID, allViewModel.employeeProjectMapping.EmployeeCode);
                    Message = IProjectSvc.NotifyProjectAllocationToEmployee(db, webHostEnvironment, allViewModel.employeeProjectMapping.Id, Message, List);
                }
                List<EmployeeProjectMappingDataList> EmployeeProjectMappingDataList = new List<EmployeeProjectMappingDataList>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmployeeProjectMappingList", EmployeeProjectMappingDataList);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ProjectController.SetAndSaveUpdateEmployeeProjectMappingDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        /// <summary>
		/// This Method Is Used To Save Or Update Project Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateProjectActivityDetails(int ID, AllViewModel allViewModel)
        {
            string Message = string.Empty;
            try
            {
                allViewModel.ProjectActivity.Id = ID;
                allViewModel.ProjectActivity.IsDeleted = (allViewModel.ProjectActivity.IsDeleted == true) ? true : false;
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                allViewModel.ProjectActivity.CreatedBy = SessionDetails.Id;
                allViewModel.ProjectActivity.UpdatedBy = SessionDetails.Id;
                allViewModel.ProjectActivity.Status = "ADD";
                Message = await IProjectSvc.SaveUpdateProjectActivityDetails(allViewModel.ProjectActivity);
                List<ProjectActivityM> ProjectActivityList = new List<ProjectActivityM>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "ProjectActivityList", ProjectActivityList);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ProjectController.SetAndSaveUpdateProjectActivityDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        #endregion [Custom Methods]
    }
}
