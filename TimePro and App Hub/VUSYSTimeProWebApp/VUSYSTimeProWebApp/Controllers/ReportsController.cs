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
using Microsoft.CodeAnalysis;
using System.Data;
using ExcelHelper = VUSYSTimeProWebApp.DACHelper.ExcelHelper;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class ReportsController : Controller
    {
        private AppDbContext db = null;
        private readonly ITimeSheet ITimesheetSvc = null;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IDataProtector protector;

        public ReportsController(AppDbContext appDbContext, ITimeSheet timeSheet, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString, IWebHostEnvironment _webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            db = appDbContext;
            this.ITimesheetSvc = timeSheet;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
            webHostEnvironment = _webHostEnvironment;
            this.httpContextAccessor = httpContextAccessor;
        }

        #region [Timesheet Report]

        /// <summary>
        /// This Action Method Is Used To Invoke Timesheet Report Screen.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllTimesheetReportList(AllViewModel allViewModel)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                SetSearchFilterDropDownDataInViewBag();
                ProjectTimesheetReportFilter _objProjectTimesheetReportFilter = new ProjectTimesheetReportFilter();
                _objProjectTimesheetReportFilter.TimesheetMonth = DateTimeHelper.GetMonthName(DateTime.Now.Month);
                _objProjectTimesheetReportFilter.TimesheetYear = DateTime.Now.Year;
                allViewModel.projectTimesheetReportFilter = _objProjectTimesheetReportFilter;
                ViewBag.DisplayGrid = true;
                if (TempData["SelectedMonth"] != null && TempData["SelectedYear"] != null && TempData["SelectedEmployee"] != null)
                {
                    string TimesheetMonth = TempData["SelectedMonth"].ToString();
                    string TimesheetYear = TempData["SelectedYear"].ToString();
                    string EmployeeCode = TempData["SelectedEmployee"].ToString();
                    if (TimesheetMonth != string.Empty && TimesheetYear != string.Empty)
                    {
                        _objProjectTimesheetReportFilter.TimesheetMonth = TimesheetMonth;
                        _objProjectTimesheetReportFilter.TimesheetYear = Convert.ToInt32(TimesheetYear);
                        List<ProjectTimesheetSubmissionApproveRejectReportList> ProjectTimesheetSubmissionApproveRejectReportList = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetReportList", ProjectTimesheetSubmissionApproveRejectReportList);
                        await GetTimesheetReportList(EmployeeCode, TimesheetMonth, Convert.ToInt32(TimesheetYear));
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetAllTimesheetReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Post Action Method is Used to Get Timesheet Report List.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllTimesheetReportList(AllViewModel allViewModel, string action)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    SetSearchFilterDropDownDataInViewBag();
                    ViewBag.DisplayGrid = true;
                    TempData["SelectedMonth"] = allViewModel.projectTimesheetReportFilter.TimesheetMonth;
                    TempData["SelectedYear"] = allViewModel.projectTimesheetReportFilter.TimesheetYear;
                    TempData["SelectedEmployee"] = allViewModel.projectTimesheetReportFilter.EmployeeCode;
                    List<ProjectTimesheetSubmissionApproveRejectReportList> ProjectTimesheetSubmissionApproveRejectReportList = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetReportList", ProjectTimesheetSubmissionApproveRejectReportList);
                    await GetTimesheetReportList(allViewModel.projectTimesheetReportFilter.EmployeeCode, allViewModel.projectTimesheetReportFilter.TimesheetMonth, allViewModel.projectTimesheetReportFilter.TimesheetYear);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetAllTimesheetReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Method Is Used To Get Timesheet Report List and Display In The Grid.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="Month"></param>
        /// <param name="Year"></param>
        /// <returns></returns>
		public async Task<IActionResult> GetTimesheetReportList(string EmployeeCode, string Month, int Year)
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<ProjectTimesheetSubmissionApproveRejectReportList> actualdata = null;
            try
            {
                List<ProjectTimesheetSubmissionApproveRejectReportList> ptList = null;
                var value = HttpContext.Session.GetString("TimesheetReportList");
                if (value == null || value == "[]")
                {
                    ptList = await ITimesheetSvc.GetTimesheetSubmissionList(EmployeeCode, Month, Year, "GetEmployeeProjectTimesheetForReport");
                    foreach (ProjectTimesheetSubmissionApproveRejectReportList item in ptList)
                    {
                        item.IsShowSendNotificationButton = (item.TimesheetApprovalStatus != "Approved" || item.IsAvailableToResendForApproval == true ? true : false);
                        item.IsShowViewDetailsButton = (item.TimesheetApprovalStatus != "Not Submitted" && item.TimesheetApprovalStatus != "Pending For Approval" ? true : false);
                    }
                    ptList.ToList().ForEach(u => u.OtherParameters = (u.TimesheetApprovalStatus == "Not Submitted" ? "Notify User" : "") + "^" + u.IsShowSendNotificationButton + "^" + u.ProjectTimeSheetID + "^" + u.TimeSheetMonth + "^" + u.TimeSheetYear + "^" + u.EmployeeCode);

                    ptList.ToList().ForEach(u => u.ApprovedRejectedParameters = (u.TimesheetApprovalStatus != null && u.TimesheetApprovalStatus != "" && u.TimesheetApprovalStatus != string.Empty ? u.TimesheetApprovalStatus : "") + "^" + (u.ApprovedOrRejectedByUser != null && u.ApprovedOrRejectedByUser != "" && u.ApprovedOrRejectedByUser != string.Empty ? u.ApprovedOrRejectedByUser : "") + "^" + (u.ApprovedOrRejectedReason != null && u.ApprovedOrRejectedReason != "" && u.ApprovedOrRejectedReason != string.Empty ? u.ApprovedOrRejectedReason : "") + "^" + (u.ApprovedOrRejectedDateTime != null && u.ApprovedOrRejectedDateTime != "" && u.ApprovedOrRejectedDateTime != string.Empty ? u.ApprovedOrRejectedDateTime : "") + "^" + u.IsShowViewDetailsButton);

                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetReportList", ptList);
                }
                else
                {
                    ptList = JsonConvert.DeserializeObject<List<ProjectTimesheetSubmissionApproveRejectReportList>>(value);
                }
                if (ptList != null)
                {
                    List<ProjectTimesheetSubmissionApproveRejectReportList> TimesheetReportList = ptList.ToList();

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "fullName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetReportList = TimesheetReportList.OrderBy(o => o.FullName).ToList();
                                        break;
                                    case "desc":
                                        TimesheetReportList = TimesheetReportList.OrderByDescending(o => o.FullName).ToList();
                                        break;
                                }
                                break;

                            case "timeSheetMonth":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetReportList = TimesheetReportList.OrderBy(o => o.TimeSheetMonth).ToList();
                                        break;
                                    case "desc":
                                        TimesheetReportList = TimesheetReportList.OrderByDescending(o => o.TimeSheetMonth).ToList();
                                        break;
                                }
                                break;

                            case "timeSheetYear":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetReportList = TimesheetReportList.OrderBy(o => o.TimeSheetYear).ToList();
                                        break;
                                    case "desc":
                                        TimesheetReportList = TimesheetReportList.OrderByDescending(o => o.TimeSheetYear).ToList();
                                        break;
                                }
                                break;

                            case "totalHoursWorked":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetReportList = TimesheetReportList.OrderByDescending(o => o.TotalHoursWorked).ToList();
                                        break;
                                    case "desc":
                                        TimesheetReportList = TimesheetReportList.OrderByDescending(o => o.TotalHoursWorked).ToList();
                                        break;
                                }
                                break;
                            case "timesheetApprovalStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetReportList = TimesheetReportList.OrderBy(o => o.TimesheetApprovalStatus).ToList();
                                        break;
                                    case "desc":
                                        TimesheetReportList = TimesheetReportList.OrderByDescending(o => o.TimesheetApprovalStatus).ToList();
                                        break;
                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetReportList = TimesheetReportList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        TimesheetReportList = TimesheetReportList.OrderByDescending(o => o.SLNo).ToList();
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
                        TimesheetReportList = TimesheetReportList.Where(e => e.FullName.Contains(searchValue) || e.TimeSheetMonth.ToString().Contains(searchValue) || e.TimeSheetYear.ToString().Contains(searchValue) || e.TimesheetApprovalStatus.Contains(searchValue) || e.TotalHoursWorked.ToString().Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = TimesheetReportList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = TimesheetReportList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = TimesheetReportList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetTimesheetReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
        /// This Get Action Method is Used to Send Notification To Employee.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <returns></returns>
		[HttpGet]
        public IActionResult SendTimesheetSubmissionReminderNotificationToEmployee(int ProjectTimeSheetID, string EmployeeCode, string TimesheetMonth, int TimesheetYear)
        {
            try
            {
                if (ProjectTimeSheetID > 0)
                {
                    string Message = string.Empty;
                    string TempDataKey = string.Empty;
                    Message = ITimesheetSvc.SendTimesheetSubmissionReminderNotificationToEmployee(webHostEnvironment, httpContextAccessor, db, EmployeeCode, TimesheetMonth, TimesheetYear, out TempDataKey, out Message);
                    if (Message != string.Empty)
                    {
                        TempData[TempDataKey + "Message"] = Message;
                        TempData["SelectedMonth"] = TimesheetMonth;
                        TempData["SelectedYear"] = TimesheetYear;
                        TempData["SelectedEmployee"] = EmployeeCode;
                        return RedirectToAction("GetAllTimesheetReportList");
                    }
                }
                else
                {
                    return RedirectToAction("GetAllTimesheetReportList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.SendTimesheetSubmissionReminderNotificationToEmployee()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        #endregion [Timesheet Report]

        #region [Project Allocated Consumed Report]

        /// <summary>
        /// This Action Method Is Used To Invoke Project Allocated Consumed Report Screen.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllProjectAllocatedConsumedReportList(AllViewModel allViewModel)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                ProjectAllocatedConsumedReportFilter _objProjectAllocatedConsumedReportFilter = new ProjectAllocatedConsumedReportFilter();
                allViewModel.projectAllocatedConsumedReportFilter = _objProjectAllocatedConsumedReportFilter;
                SetSearchFilterDropDownDataInViewBag(true);
                ViewBag.DisplayGrid = true;
                List<ProjectAllocatedConsumedReportList> ProjectAllocatedConsumedReportList = new List<ProjectAllocatedConsumedReportList>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "ProjectAllocatedConsumedReportList", ProjectAllocatedConsumedReportList);
                int ProjectID = 0;
                if (TempData["SelectedProjectID"] != null)
                {
                    ProjectID = Convert.ToInt32(TempData["SelectedProjectID"].ToString());
                }
                await GetProjectAllocatedConsumedReportList(ProjectID);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetAllProjectAllocatedConsumedReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Post Action Method is Used to Get Project Allocated Consumed Report List.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllProjectAllocatedConsumedReportList(AllViewModel allViewModel, string action)
        {
            try
            {
                if(action == "ByGetProjectAllocatedConsumedReportDetails")
                {
                    if (ModelState.IsValid)
                    {
                        SetSearchFilterDropDownDataInViewBag(true);
                        ViewBag.DisplayGrid = true;
                        TempData["SelectedProjectID"] = allViewModel.projectAllocatedConsumedReportFilter.ProjectID;
                        List<ProjectAllocatedConsumedReportList> ProjectAllocatedConsumedReportList = new List<ProjectAllocatedConsumedReportList>();
                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "ProjectAllocatedConsumedReportList", ProjectAllocatedConsumedReportList);
                        await GetProjectAllocatedConsumedReportList(allViewModel.projectAllocatedConsumedReportFilter.ProjectID);
                    }
                }
                else
                {
                    return RedirectToAction("DownloadProjectAllocatedConsumedReportToExcel", new {ProjectID = allViewModel.projectAllocatedConsumedReportFilter.ProjectID });
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetAllProjectAllocatedConsumedReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Method Is Used To Get Project Allocated Consumed Report List and Display In The Grid.
        /// </summary>
        /// <param name="ProjectID"></param>
        /// <returns></returns>
		public async Task<IActionResult> GetProjectAllocatedConsumedReportList(int ProjectID)
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<ProjectAllocatedConsumedReportList> actualdata = null;
            try
            {
                List<ProjectAllocatedConsumedReportList> ptList = null;
                var value = HttpContext.Session.GetString("ProjectAllocatedConsumedReportList");
                if (value == null || value == "[]")
                {
                    ptList = await ITimesheetSvc.GetProjectAllocatedConsumedReportList(ProjectID);
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "ProjectAllocatedConsumedReportList", ptList);
                }
                else
                {
                    ptList = JsonConvert.DeserializeObject<List<ProjectAllocatedConsumedReportList>>(value);
                }
                if (ptList != null)
                {
                    List<ProjectAllocatedConsumedReportList> ProjectAllocatedConsumedReportList = ptList.ToList();

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
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.ProjectCode).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.ProjectCode).ToList();
                                        break;
                                }
                                break;

                            case "projectName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.ProjectName).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.ProjectName).ToList();
                                        break;
                                }
                                break;

                            case "clientName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.ClientName).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.ClientName).ToList();
                                        break;
                                }
                                break;

                            case "allocatedHours":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.AllocatedHours).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.AllocatedHours).ToList();
                                        break;
                                }
                                break;
                            case "consumedHours":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.ConsumedHours).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.ConsumedHours).ToList();
                                        break;
                                }
                                break;
                            case "requirementGatheringAndAnalysis":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.RequirementGatheringAndAnalysis).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.RequirementGatheringAndAnalysis).ToList();
                                        break;
                                }
                                break;
                            case "systemDesign":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.SystemDesign).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.SystemDesign).ToList();
                                        break;
                                }
                                break;
                            case "frontendDevelopment":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.FrontendDevelopment).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.FrontendDevelopment).ToList();
                                        break;
                                }
                                break;
                            case "backendDevelopment":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.BackendDevelopment).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.BackendDevelopment).ToList();
                                        break;
                                }
                                break;
                            case "codeReview":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.CodeReview).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.CodeReview).ToList();
                                        break;
                                }
                                break;
                            case "systemTesting":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.SystemTesting).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.SystemTesting).ToList();
                                        break;
                                }
                                break;
                            case "projectManagement":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.ProjectManagement).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.ProjectManagement).ToList();
                                        break;
                                }
                                break;
                            case "deployment":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.Deployment).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.Deployment).ToList();
                                        break;
                                }
                                break;
                            case "postDeploymentMonitoring":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.PostDeploymentMonitoring).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.PostDeploymentMonitoring).ToList();
                                        break;
                                }
                                break;
                            case "fixPostReleaseBugs":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.FixPostReleaseBugs).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.FixPostReleaseBugs).ToList();
                                        break;
                                }
                                break;
                            case "maintenance":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.Maintenance).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.Maintenance).ToList();
                                        break;
                                }
                                break;
                            case "knowledgeTransfer":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.KnowledgeTransfer).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.KnowledgeTransfer).ToList();
                                        break;
                                }
                                break;
                            case "learning":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.Learning).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.Learning).ToList();
                                        break;
                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.OrderByDescending(o => o.SLNo).ToList();
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
                        ProjectAllocatedConsumedReportList = ProjectAllocatedConsumedReportList.Where(e => e.ProjectCode.Contains(searchValue) || e.ProjectName.Contains(searchValue) || e.ClientName.Contains(searchValue) || e.AllocatedHours.ToString().Contains(searchValue) || e.ConsumedHours.ToString().Contains(searchValue) || e.RequirementGatheringAndAnalysis.ToString().Contains(searchValue) || e.SystemDesign.ToString().Contains(searchValue) || e.FrontendDevelopment.ToString().Contains(searchValue) || e.BackendDevelopment.ToString().Contains(searchValue) || e.CodeReview.ToString().Contains(searchValue) || e.SystemTesting.ToString().Contains(searchValue) || e.ProjectManagement.ToString().Contains(searchValue) || e.Deployment.ToString().Contains(searchValue) || e.PostDeploymentMonitoring.ToString().Contains(searchValue) || e.FixPostReleaseBugs.ToString().Contains(searchValue) || e.Maintenance.ToString().Contains(searchValue) || e.KnowledgeTransfer.ToString().Contains(searchValue) || e.Learning.ToString().Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = ProjectAllocatedConsumedReportList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = ProjectAllocatedConsumedReportList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = ProjectAllocatedConsumedReportList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetProjectAllocatedConsumedReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
        /// This Get Action Method is Used to Get Project Allocated Consumed Report Data and Export To Excel.
        /// </summary>
        /// <param name="ProjectID"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileResult> DownloadProjectAllocatedConsumedReportToExcel(int ProjectID)
        {
            FileContentResult result = null;
            try
            {
                string Title = string.Empty;
                string SheetName = string.Empty;
                string FileName = string.Empty;
                SheetName = "ProjectAllocatedOrConsumed";
                Title = "Project Allocated/Consumed Activity Split Report";
                FileName = "ProjectAllocatedOrConsumedReport.xlsx";
                const int NoOfColumns = 17;
                DataTable dt = new DataTable("ProjectAllocatedOrConsumed");
                dt.Columns.AddRange(new DataColumn[NoOfColumns] {new DataColumn("SL No", typeof(Int64)),
                                            new DataColumn("Project Code", typeof(string)),
                                            new DataColumn("Project Name", typeof(string)),
                                            new DataColumn("Client", typeof(string)),
                                            new DataColumn("Allocated Hours", typeof(decimal)),
                                            new DataColumn("ConsumedHours", typeof(decimal)),
                                            new DataColumn("Requirement Gathering & Analysis", typeof(decimal)),
                                            new DataColumn("System Design", typeof(decimal)),
                                            new DataColumn("Frontend Development", typeof(decimal)),
                                            new DataColumn("Backend Development", typeof(decimal)),
                                            new DataColumn("Code Review", typeof(decimal)),
                                            new DataColumn("System Testing", typeof(decimal)),
                                            new DataColumn("Project Management", typeof(decimal)),
                                            new DataColumn("Deployment", typeof(decimal)),
                                            new DataColumn("Post-Deployment Monitoring", typeof(decimal)),
                                            new DataColumn("Fix Post-Release Bugs", typeof(decimal)),
                                            new DataColumn("Maintenance", typeof(decimal))
                    });
                var dtList = await ITimesheetSvc.GetProjectAllocatedConsumedReportList(ProjectID);
                if (dtList != null)
                {
                    List<ProjectAllocatedConsumedReportList> ProjectAllocatedConsumedReportListData = dtList.ToList();
                    foreach (var row in ProjectAllocatedConsumedReportListData)
                    {
                        dt.Rows.Add
                        (
                            row.SLNo, row.ProjectCode, row.ProjectName, row.ClientName, row.AllocatedHours, row.ConsumedHours, 
                            row.RequirementGatheringAndAnalysis, row.SystemDesign, row.FrontendDevelopment, row.BackendDevelopment,
                            row.CodeReview, row.SystemTesting, row.ProjectManagement, row.Deployment, row.PostDeploymentMonitoring,
                            row.FixPostReleaseBugs, row.Maintenance
                        );
                    }
                }
                result = ExcelHelper.ExportToExcel(dt, SheetName, Title, "A1:Q1", 1, NoOfColumns, FileName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.DownloadProjectAllocatedConsumedReportToExcel()", ex.Message, false);
            }
            return result;
        }


        #endregion [Project Allocated Consumed Report]

        #region [Leave Report]

        /// <summary>
        /// This Action Method Is Used To Invoke Leave Report Screen.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllLeaveReportList(AllViewModel allViewModel)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                SetSearchFilterDropDownDataInViewBag();
                LeaveReportFilter _objLeaveReportFilter = new LeaveReportFilter();
                _objLeaveReportFilter.TimesheetMonth = DateTimeHelper.GetMonthName(DateTime.Now.Month);
                _objLeaveReportFilter.TimesheetYear = DateTime.Now.Year;
                allViewModel.leaveReportFilter = _objLeaveReportFilter;
                ViewBag.DisplayGrid = true;
                if (TempData["SelectedMonth"] != null && TempData["SelectedYear"] != null && TempData["SelectedEmployee"] != null)
                {
                    string TimesheetMonth = TempData["SelectedMonth"].ToString();
                    string TimesheetYear = TempData["SelectedYear"].ToString();
                    string EmployeeCode = TempData["SelectedEmployee"].ToString();
                    if (TimesheetMonth != string.Empty && TimesheetYear != string.Empty)
                    {
                        _objLeaveReportFilter.TimesheetMonth = TimesheetMonth;
                        _objLeaveReportFilter.TimesheetYear = Convert.ToInt32(TimesheetYear);
                        List<LeaveReportList> LeaveReportList = new List<LeaveReportList>();
                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "LeaveReportList", LeaveReportList);
                        await GetLeaveReportList(EmployeeCode, TimesheetMonth, Convert.ToInt32(TimesheetYear));
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetAllLeaveReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Post Action Method is Used to Get Leave Report List.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllLeaveReportList(AllViewModel allViewModel, string action)
        {
            try
            {
                if (action == "ByGetLeaveDetails")
                {
                    if (ModelState.IsValid)
                    {
                        SetSearchFilterDropDownDataInViewBag();
                        ViewBag.DisplayGrid = true;
                        TempData["SelectedMonth"] = allViewModel.leaveReportFilter.TimesheetMonth;
                        TempData["SelectedYear"] = allViewModel.leaveReportFilter.TimesheetYear;
                        TempData["SelectedEmployee"] = allViewModel.leaveReportFilter.EmployeeCode;
                        List<LeaveReportList> LeaveReportList = new List<LeaveReportList>();
                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "LeaveReportList", LeaveReportList);
                        await GetLeaveReportList(allViewModel.leaveReportFilter.EmployeeCode, allViewModel.leaveReportFilter.TimesheetMonth, allViewModel.leaveReportFilter.TimesheetYear);
                    }
                }
                else
                {
                    return RedirectToAction("DownloadLeaveReportToExcel", new { EmployeeCode = allViewModel.leaveReportFilter.EmployeeCode, Month = allViewModel.leaveReportFilter.TimesheetMonth, Year = allViewModel.leaveReportFilter.TimesheetYear });
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetAllLeaveReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Method Is Used To Get Leave Report List and Display In The Grid.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="Month"></param>
        /// <param name="Year"></param>
        /// <returns></returns>
		public async Task<IActionResult> GetLeaveReportList(string EmployeeCode, string Month, int Year)
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<LeaveReportList> actualdata = null;
            try
            {
                List<LeaveReportList> ptList = null;
                var value = HttpContext.Session.GetString("LeaveReportList");
                if (value == null || value == "[]")
                {
                    ptList = await ITimesheetSvc.GetLeaveReportList(EmployeeCode, Month, Year, "GetLeaveReport");

                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "LeaveReportList", ptList);
                }
                else
                {
                    ptList = JsonConvert.DeserializeObject<List<LeaveReportList>>(value);
                }
                if (ptList != null)
                {
                    List<LeaveReportList> LeaveReportList = ptList.ToList();

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "fullName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        LeaveReportList = LeaveReportList.OrderBy(o => o.FullName).ToList();
                                        break;
                                    case "desc":
                                        LeaveReportList = LeaveReportList.OrderByDescending(o => o.FullName).ToList();
                                        break;
                                }
                                break;
                            case "emailID":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        LeaveReportList = LeaveReportList.OrderBy(o => o.EmailID).ToList();
                                        break;
                                    case "desc":
                                        LeaveReportList = LeaveReportList.OrderByDescending(o => o.EmailID).ToList();
                                        break;
                                }
                                break;
                            case "onLeave":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        LeaveReportList = LeaveReportList.OrderBy(o => o.OnLeave).ToList();
                                        break;
                                    case "desc":
                                        LeaveReportList = LeaveReportList.OrderByDescending(o => o.OnLeave).ToList();
                                        break;
                                }
                                break;

                            case "holiday":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        LeaveReportList = LeaveReportList.OrderBy(o => o.Holiday).ToList();
                                        break;
                                    case "desc":
                                        LeaveReportList = LeaveReportList.OrderByDescending(o => o.Holiday).ToList();
                                        break;
                                }
                                break;

                            case "compOff":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        LeaveReportList = LeaveReportList.OrderByDescending(o => o.CompOff).ToList();
                                        break;
                                    case "desc":
                                        LeaveReportList = LeaveReportList.OrderByDescending(o => o.CompOff).ToList();
                                        break;
                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        LeaveReportList = LeaveReportList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        LeaveReportList = LeaveReportList.OrderByDescending(o => o.SLNo).ToList();
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
                        LeaveReportList = LeaveReportList.Where(e => e.FullName.Contains(searchValue) || e.EmailID.Contains(searchValue) || e.OnLeave.ToString().Contains(searchValue) || e.Holiday.ToString().Contains(searchValue) || e.CompOff.ToString().Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = LeaveReportList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = LeaveReportList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = LeaveReportList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.GetLeaveReportList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
        /// This Get Action Method is Used to Get Leave Report Data and Export To Excel.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="Month"></param>
        /// <param name="Year"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileResult> DownloadLeaveReportToExcel(string EmployeeCode, string Month, int Year)
        {
            FileContentResult result = null;
            try
            {
                string Title = string.Empty;
                string SheetName = string.Empty;
                string FileName = string.Empty;
                SheetName = $"Leave Report {Month + "_" + Year}";
                Title = "Leave Report For: " + Month + " " + Year;
                FileName = $"{SheetName}.xlsx";
                const int NoOfColumns = 6;
                DataTable dt = new DataTable("LeaveReport");
                dt.Columns.AddRange(new DataColumn[NoOfColumns] {new DataColumn("SL No", typeof(Int64)),
                                            new DataColumn("Employee Name", typeof(string)),
                                            new DataColumn("Email ID", typeof(string)),
                                            new DataColumn("On Leave", typeof(int)),
                                            new DataColumn("Holiday", typeof(int)),
                                            new DataColumn("Compoff", typeof(int))
                    });
                var dtList = await ITimesheetSvc.GetLeaveReportList(EmployeeCode, Month, Year, "GetLeaveReport");
                if (dtList != null)
                {
                    List<LeaveReportList> LeaveReportListListData = dtList.ToList();
                    foreach (var row in LeaveReportListListData)
                    {
                        dt.Rows.Add
                        (
                            row.SLNo, row.FullName, row.EmailID, row.OnLeave, row.Holiday, row.CompOff
                        );
                    }
                }
                result = ExcelHelper.ExportToExcel(dt, SheetName, Title, "A1:E1", 1, NoOfColumns, FileName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.DownloadLeaveReportToExcel()", ex.Message, false);
            }
            return result;
        }

        #endregion [Leave Report]

        #region [Custom Methods]

        /// <summary>
        /// This Method Is Used To Set Search Filter DropDown Data In View Bag.
        /// </summary>
        /// <param name="IsShowProjectList"></param>
        public void SetSearchFilterDropDownDataInViewBag(bool IsShowProjectList = false)
        {
            try
            {
                if(!IsShowProjectList)
                {
                    ViewBag.EmployeeList = DropdownHelper.GetEmployeesWorkingOnProjectWithAll(db);
                    ViewBag.YearList = DropdownHelper.FillYearMonthList("Year");
                    ViewBag.MonthList = DropdownHelper.FillYearMonthList("Month");
                }
                else
                {
                    ViewBag.ProjectList = DropdownHelper.FillProjectListReport(db);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "ReportsController.SetSearchFilterDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        #endregion [Custom Methods]
    }
}
