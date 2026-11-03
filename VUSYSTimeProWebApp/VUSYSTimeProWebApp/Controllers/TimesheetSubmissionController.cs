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
    public class TimesheetSubmissionController : Controller
    {
        private AppDbContext db = null;
        private readonly ITimeSheet ITimesheetSvc = null;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IDataProtector protector;

        public TimesheetSubmissionController(AppDbContext appDbContext, ITimeSheet timeSheet, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString, IWebHostEnvironment _webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            db = appDbContext;
            this.ITimesheetSvc = timeSheet;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
            webHostEnvironment = _webHostEnvironment;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// This Action Method Is Used To Invoke Timesheet Submission Screen.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllTimesheetSubmissionList(AllViewModel allViewModel)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                ViewBag.YearList = DropdownHelper.FillYearMonthList("Year");
                ViewBag.MonthList = DropdownHelper.FillYearMonthList("Month");
                ProjectTimesheetSubmissionFilter _objProjectTimesheetSubmissionFilter = new ProjectTimesheetSubmissionFilter();
                _objProjectTimesheetSubmissionFilter.TimesheetMonth = DateTimeHelper.GetMonthName(DateTime.Now.Month);
                _objProjectTimesheetSubmissionFilter.TimesheetYear = DateTime.Now.Year;
                allViewModel.projectTimesheetSubmissionFilter = _objProjectTimesheetSubmissionFilter;
                ViewBag.DisplayGrid = true;
                if(TempData["SelectedMonth"] != null && TempData["SelectedYear"] != null)
                {
                    string TimesheetMonth = TempData["SelectedMonth"].ToString();
                    string TimesheetYear = TempData["SelectedYear"].ToString();
                    if (TimesheetMonth != string.Empty && TimesheetYear != string.Empty)
                    {
                        _objProjectTimesheetSubmissionFilter.TimesheetMonth = TimesheetMonth;
                        _objProjectTimesheetSubmissionFilter.TimesheetYear = Convert.ToInt32(TimesheetYear);
                        List<ProjectTimesheetSubmissionApproveRejectReportList> ProjectTimesheetSubmissionApproveRejectReportList = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetSubmissionList", ProjectTimesheetSubmissionApproveRejectReportList);
                        await GetTimeSheetSubmissionList(TimesheetMonth, Convert.ToInt32(TimesheetYear));
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetSubmissionController.GetAllTimesheetSubmissionList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Post Action Method is Used to Get Timesheet Submission List.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllTimesheetSubmissionList(AllViewModel allViewModel, string action)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    ViewBag.DisplayGrid = true;
                    TempData["SelectedMonth"] = allViewModel.projectTimesheetSubmissionFilter.TimesheetMonth;
                    TempData["SelectedYear"] = allViewModel.projectTimesheetSubmissionFilter.TimesheetYear;
                    List<ProjectTimesheetSubmissionApproveRejectReportList> ProjectTimesheetSubmissionApproveRejectReportList = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetSubmissionList", ProjectTimesheetSubmissionApproveRejectReportList);
                    await GetTimeSheetSubmissionList(allViewModel.projectTimesheetSubmissionFilter.TimesheetMonth, allViewModel.projectTimesheetSubmissionFilter.TimesheetYear);
                    ViewBag.YearList = DropdownHelper.FillYearMonthList("Year");
                    ViewBag.MonthList = DropdownHelper.FillYearMonthList("Month");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetController.GetAllTimesheetSubmissionList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Method Is Used To Get Timesheet Submission List and Display In The Grid.
        /// </summary>
        /// <param name="Month"></param>
        /// <param name="Year"></param>
        /// <returns></returns>
		public async Task<IActionResult> GetTimeSheetSubmissionList(string Month, int Year)
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<ProjectTimesheetSubmissionApproveRejectReportList> actualdata = null;
            try
            {
                List<ProjectTimesheetSubmissionApproveRejectReportList> ptList = null;
                var value = HttpContext.Session.GetString("TimesheetSubmissionList");
                if (value == null || value == "[]")
                {
                    var SessionDetails = CommonHelper.GetUserSessionDetails(HttpContext.Session);
                    ptList = await ITimesheetSvc.GetTimesheetSubmissionList(SessionDetails.EmployeeCode, Month, Year, "GetEmployeeProjectTimesheetForSubmission");
                    foreach (ProjectTimesheetSubmissionApproveRejectReportList item in ptList)
                    {
                        item.IsShowSendForApprovalButton = (item.TimesheetApprovalStatus != "Approved" || item.IsAvailableToResendForApproval == true ? true : false);
                        item.IsShowViewDetailsButton = (item.TimesheetApprovalStatus != "Not Submitted" && item.TimesheetApprovalStatus != "Pending For Approval" ? true : false);
                    }
                    ptList.ToList().ForEach(u => u.OtherParameters = (u.TimesheetApprovalStatus == "Not Submitted" && u.IsAvailableToResendForApproval == false ? "Send For Approval" : u.TimesheetApprovalStatus != "Not Submitted" && u.IsAvailableToResendForApproval == true ? "Resend For Approval" : "") + "^" + u.IsShowSendForApprovalButton + "^" + u.ProjectTimeSheetID + "^" + u.TimeSheetMonth + "^" + u.TimeSheetYear + "^" + u.EmployeeCode);

                    ptList.ToList().ForEach(u => u.ApprovedRejectedParameters = (u.TimesheetApprovalStatus != null && u.TimesheetApprovalStatus != "" && u.TimesheetApprovalStatus != string.Empty ? u.TimesheetApprovalStatus : "") + "^" + (u.ApprovedOrRejectedByUser != null && u.ApprovedOrRejectedByUser != "" && u.ApprovedOrRejectedByUser != string.Empty ? u.ApprovedOrRejectedByUser : "") + "^" + (u.ApprovedOrRejectedReason != null && u.ApprovedOrRejectedReason != "" && u.ApprovedOrRejectedReason != string.Empty ? u.ApprovedOrRejectedReason : "") + "^" + (u.ApprovedOrRejectedDateTime != null && u.ApprovedOrRejectedDateTime != "" && u.ApprovedOrRejectedDateTime != string.Empty ? u.ApprovedOrRejectedDateTime : "") + "^" + u.IsShowViewDetailsButton);

                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetSubmissionList", ptList);
                }
                else
                {
                    ptList = JsonConvert.DeserializeObject<List<ProjectTimesheetSubmissionApproveRejectReportList>>(value);
                }
                if (ptList != null)
                {
                    List<ProjectTimesheetSubmissionApproveRejectReportList> TimesheetSubmissionList = ptList.ToList();

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "timeSheetMonth":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderBy(o => o.TimeSheetMonth).ToList();
                                        break;
                                    case "desc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderByDescending(o => o.TimeSheetMonth).ToList();
                                        break;
                                }
                                break;

                            case "timeSheetYear":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderBy(o => o.TimeSheetYear).ToList();
                                        break;
                                    case "desc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderByDescending(o => o.TimeSheetYear).ToList();
                                        break;
                                }
                                break;
                            
                            case "totalHoursWorked":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderByDescending(o => o.TotalHoursWorked).ToList();
                                        break;
                                    case "desc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderByDescending(o => o.TotalHoursWorked).ToList();
                                        break;
                                }
                                break;
                            case "timesheetApprovalStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderBy(o => o.TimesheetApprovalStatus).ToList();
                                        break;
                                    case "desc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderByDescending(o => o.TimesheetApprovalStatus).ToList();
                                        break;
                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        TimesheetSubmissionList = TimesheetSubmissionList.OrderByDescending(o => o.SLNo).ToList();
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
                        TimesheetSubmissionList = TimesheetSubmissionList.Where(e => e.TimeSheetMonth.Contains(searchValue) || e.TimeSheetYear.ToString().Contains(searchValue) || e.TimesheetApprovalStatus.Contains(searchValue) || e.TotalHoursWorked.ToString().Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = TimesheetSubmissionList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = TimesheetSubmissionList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = TimesheetSubmissionList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetSubmissionController.GetTimeSheetSubmissionList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
        /// This Get Action Method is Used to Send Time Sheet For Approval.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
		[HttpGet]
        public IActionResult SendTimesheetForApproval(int ProjectTimeSheetID, string TimesheetMonth, int TimesheetYear, string Status)
        {
            try
            {
                if (ProjectTimeSheetID > 0)
                {
                    string Message = string.Empty;
                    string TempDataKey = string.Empty;
                    Message = ITimesheetSvc.SubmitTimesheetDetailsForApproval(webHostEnvironment, httpContextAccessor, HttpContext.Session, protector, db, ProjectTimeSheetID, TimesheetMonth, TimesheetYear, Status, out TempDataKey, out Message);
                    if (Message != string.Empty)
                    {
                        TempData[TempDataKey + "Message"] = Message;
                        TempData["SelectedMonth"] = TimesheetMonth;
                        TempData["SelectedYear"] = TimesheetYear;
                        return RedirectToAction("GetAllTimesheetSubmissionList");
                    }
                }
                else
                {
                    return RedirectToAction("GetAllTimesheetSubmissionList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetSubmissionController.GetTimeSheetSubmissionList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }
    }
}
