using System.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.Security;
using VUSYSTimeProWebApp.ViewModels;
using CommonLib;
using ExcelHelper = VUSYSTimeProWebApp.DACHelper.ExcelHelper;

namespace VUSYSTimeProWebApp.Controllers
{
    public class TimesheetApprovalRejectionController : Controller
    {
        private AppDbContext db = null;
        private readonly ITimeSheet ITimesheetSvc = null;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IDataProtector protector;

        public TimesheetApprovalRejectionController(AppDbContext appDbContext, ITimeSheet timeSheet, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString, IWebHostEnvironment _webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            db = appDbContext;
            this.ITimesheetSvc = timeSheet;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
            webHostEnvironment = _webHostEnvironment;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// This Action Method Is Used To Invoke Timesheet Approval Rejection Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ApproveReject(AllViewModel allViewModel, bool IsFromPostMethod, string ResponseStatus)
        {
            try
            {
                if (IsFromPostMethod)
                {
                    ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection = new ProjectTimesheetApproveRejection();
                    _objProjectTimesheetApproveRejection.IsOnPageLoad = false;
                    _objProjectTimesheetApproveRejection.ProjectTimeSheetApprovalResponseStatus = ResponseStatus;
                    allViewModel.projectTimesheetApproveRejection = _objProjectTimesheetApproveRejection;
                    return View(allViewModel);
                }
                else
                {
                    string ProjectTimeSheetID = Request.Query["PTSID"];
                    string Source = Request.Query["S"];
                    string GUID = Request.Query["ID"];
                    string EmployeeCode = Request.Query["Code"];
                    string TimesheetMonth = Request.Query["M"];
                    string TimesheetYear = Request.Query["Y"];

                    if (!string.IsNullOrWhiteSpace(ProjectTimeSheetID) && !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(GUID) && !string.IsNullOrWhiteSpace(EmployeeCode) && !string.IsNullOrWhiteSpace(TimesheetMonth) && !string.IsNullOrWhiteSpace(TimesheetYear))
                    {
                        string DecryptedProjectTimeSheetID = protector.Unprotect(ProjectTimeSheetID);
                        string DecryptedSource = protector.Unprotect(Source);
                        string DecryptedGUID = protector.Unprotect(GUID);
                        string DecryptedEmployeeCode = protector.Unprotect(EmployeeCode);
                        string DecryptedTimesheetMonth = protector.Unprotect(TimesheetMonth);
                        string DecryptedTimesheetYear = protector.Unprotect(TimesheetYear);
                        if (DecryptedSource == "Email")
                        {
                            ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection = new ProjectTimesheetApproveRejection();
                            _objProjectTimesheetApproveRejection.EmployeeCode = DecryptedEmployeeCode;
                            _objProjectTimesheetApproveRejection.TimesheetMonth = DecryptedTimesheetMonth;
                            _objProjectTimesheetApproveRejection.TimesheetYear = Convert.ToInt32(DecryptedTimesheetYear);
                            _objProjectTimesheetApproveRejection.ProjectTimeSheetID = Convert.ToInt32(DecryptedProjectTimeSheetID);
                            _objProjectTimesheetApproveRejection.Source = DecryptedSource;
                            _objProjectTimesheetApproveRejection.UserToken = DecryptedGUID;
                            allViewModel.projectTimesheetApproveRejection = _objProjectTimesheetApproveRejection;
                            string Message = string.Empty;
                            string TempDataKey = string.Empty;
                            Message = ITimesheetSvc.CheckTimesheetApprovalStatus(allViewModel.projectTimesheetApproveRejection, out TempDataKey, out Message);
                            _objProjectTimesheetApproveRejection.ProjectTimeSheetApprovalResponseStatus = TempDataKey;
                            _objProjectTimesheetApproveRejection.ProjectTimeSheetApprovalResponseMessage = Message;
                            _objProjectTimesheetApproveRejection.IsOnPageLoad = true;
                            if (Message != "Pending for Approval.")
                            {
                                TempData[TempDataKey + "Message"] = Message;
                            }
                            allViewModel.projectTimesheetApproveRejection = _objProjectTimesheetApproveRejection;
                            SetApprovalStatusDropDownDataInViewBag();
                            return View(allViewModel);
                        }
                    }
                    else
                    {
                        return RedirectToAction("Login", "Login");
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.ApproveReject()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Save Project Timesheet Approval Or Rejection Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveReject(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string Message = string.Empty;
                    string TempDataKey = string.Empty;
                    Message = ITimesheetSvc.ApproveRejectTimesheet(webHostEnvironment, httpContextAccessor, HttpContext.Session, db, allViewModel.projectTimesheetApproveRejection, out TempDataKey, out Message);
                    if (Message != string.Empty)
                    {
                        allViewModel.projectTimesheetApproveRejection.IsOnPageLoad = false;
                        TempData[TempDataKey + "Message"] = Message;
                        return RedirectToAction("ApproveReject", new { IsFromPostMethod = true, ResponseStatus = TempDataKey });
                    }
                }
                else
                {
                    SetApprovalStatusDropDownDataInViewBag();
                    return View(allViewModel);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.ApproveReject()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Action Method Is Used To Invoke Timesheet Approval Rejection List Screen.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllTimesheetApprovalRejectionList(AllViewModel allViewModel)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                SetSearchFilterDropDownDataInViewBag();
                ProjectTimesheetApproveRejectFilter _objProjectTimesheetApproveRejectFilter = new ProjectTimesheetApproveRejectFilter();
                _objProjectTimesheetApproveRejectFilter.TimesheetMonth = DateTimeHelper.GetMonthName(DateTime.Now.Month);
                _objProjectTimesheetApproveRejectFilter.TimesheetYear = DateTime.Now.Year;
                allViewModel.projectTimesheetApproveRejectFilter = _objProjectTimesheetApproveRejectFilter;
                ViewBag.DisplayGrid = true;
                if (TempData["SelectedMonth"] != null && TempData["SelectedYear"] != null && TempData["SelectedEmployee"] != null)
                {
                    string TimesheetMonth = TempData["SelectedMonth"].ToString();
                    string TimesheetYear = TempData["SelectedYear"].ToString();
                    string EmployeeCode = TempData["SelectedEmployee"].ToString();
                    if (TimesheetMonth != string.Empty && TimesheetYear != string.Empty && EmployeeCode != string.Empty)
                    {
                        _objProjectTimesheetApproveRejectFilter.EmployeeCode = EmployeeCode;
                        _objProjectTimesheetApproveRejectFilter.TimesheetMonth = TimesheetMonth;
                        _objProjectTimesheetApproveRejectFilter.TimesheetYear = Convert.ToInt32(TimesheetYear);
                        List<ProjectTimesheetSubmissionApproveRejectReportList> ProjectTimesheetSubmissionApproveRejectReportList = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetApproveRejectList", ProjectTimesheetSubmissionApproveRejectReportList);
                        await GetTimeSheetApproveRejectList(EmployeeCode, TimesheetMonth, Convert.ToInt32(TimesheetYear));
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetSubmissionController.GetAllTimesheetApprovalRejectionList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Method Is Used To Get Timesheet Approve Reject List and Display In The Grid.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="Month"></param>
        /// <param name="Year"></param>
        /// <returns></returns>
		public async Task<IActionResult> GetTimeSheetApproveRejectList(string EmployeeCode, string Month, int Year)
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<ProjectTimesheetSubmissionApproveRejectReportList> actualdata = null;
            try
            {
                SetApprovalStatusDropDownDataInViewBag();
                List<ProjectTimesheetSubmissionApproveRejectReportList> ptList = null;
                var value = HttpContext.Session.GetString("TimesheetApproveRejectList");
                if (value == null || value == "[]")
                {
                    ptList = await ITimesheetSvc.GetTimesheetSubmissionList(EmployeeCode, Month, Year, "GetAllEmployeeProjectTimesheetForApproval");
                    foreach (ProjectTimesheetSubmissionApproveRejectReportList item in ptList)
                    {
                        item.IsShowApproveRejectButton = ((item.TimesheetApprovalStatus != "Approved") && item.IsSentForApproval == true ? true : false);
                        item.IsShowViewDetailsButton = (item.TimesheetApprovalStatus == "Approved" || item.TimesheetApprovalStatus == "Rejected" ? true : false);
                    }
                    ptList.ToList().ForEach(u => u.OtherParameters = (u.TimesheetApprovalStatus == "Pending For Approval" && u.IsSentForApproval == true ? "Approve/Reject" : "") + "^" + u.EmployeeCode + "^" + u.IsShowApproveRejectButton + "^" + u.ProjectTimeSheetID + "^" + u.TimeSheetMonth + "^" + u.TimeSheetYear);

                    ptList.ToList().ForEach(u => u.ApprovedRejectedParameters = (u.TimesheetApprovalStatus != null && u.TimesheetApprovalStatus != "" && u.TimesheetApprovalStatus != string.Empty ? u.TimesheetApprovalStatus : "") + "^" + (u.ApprovedOrRejectedByUser != null && u.ApprovedOrRejectedByUser != "" && u.ApprovedOrRejectedByUser != string.Empty ? u.ApprovedOrRejectedByUser : "") + "^" + (u.ApprovedOrRejectedReason != null && u.ApprovedOrRejectedReason != "" && u.ApprovedOrRejectedReason != string.Empty ? u.ApprovedOrRejectedReason : "") + "^" + (u.ApprovedOrRejectedDateTime != null && u.ApprovedOrRejectedDateTime != "" && u.ApprovedOrRejectedDateTime != string.Empty ? u.ApprovedOrRejectedDateTime : "") + "^" + u.IsShowViewDetailsButton);

                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetApproveRejectList", ptList);
                }
                else
                {
                    ptList = JsonConvert.DeserializeObject<List<ProjectTimesheetSubmissionApproveRejectReportList>>(value);
                }
                if (ptList != null)
                {
                    List<ProjectTimesheetSubmissionApproveRejectReportList> TimesheetApproveRejectList = ptList.ToList();

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "employeeCode":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderBy(o => o.EmployeeCode).ToList();
                                        break;
                                    case "desc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.EmployeeCode).ToList();
                                        break;
                                }
                                break;

                            case "fullName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderBy(o => o.FullName).ToList();
                                        break;
                                    case "desc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.FullName).ToList();
                                        break;
                                }
                                break;

                            case "timeSheetMonth":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderBy(o => o.TimeSheetMonth).ToList();
                                        break;
                                    case "desc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.TimeSheetMonth).ToList();
                                        break;
                                }
                                break;

                            case "timeSheetYear":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderBy(o => o.TimeSheetYear).ToList();
                                        break;
                                    case "desc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.TimeSheetYear).ToList();
                                        break;
                                }
                                break;

                            case "totalHoursWorked":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.TotalHoursWorked).ToList();
                                        break;
                                    case "desc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.TotalHoursWorked).ToList();
                                        break;
                                }
                                break;
                            case "timesheetApprovalStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderBy(o => o.TimesheetApprovalStatus).ToList();
                                        break;
                                    case "desc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.TimesheetApprovalStatus).ToList();
                                        break;
                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        TimesheetApproveRejectList = TimesheetApproveRejectList.OrderByDescending(o => o.SLNo).ToList();
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
                        TimesheetApproveRejectList = TimesheetApproveRejectList.Where(e => e.TimeSheetMonth.Contains(searchValue) || e.TimeSheetYear.ToString().Contains(searchValue) || e.TimesheetApprovalStatus.Contains(searchValue) || e.TotalHoursWorked.ToString().Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = TimesheetApproveRejectList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = TimesheetApproveRejectList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = TimesheetApproveRejectList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.GetTimeSheetApproveRejectList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Post Action Method is Used to Save Project Timesheet Approval Or Rejection Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllTimesheetApprovalRejectionList(AllViewModel allViewModel, string action)
        {
            try
            {
                if (action == "ApproveTimesheet")
                {
                    if (ModelState.IsValid)
                    {
                        string Message = string.Empty;
                        string TempDataKey = string.Empty;
                        Message = ITimesheetSvc.ApproveRejectTimesheet(webHostEnvironment, httpContextAccessor, HttpContext.Session, db, allViewModel.projectTimesheetApproveRejection, out TempDataKey, out Message);
                        if (Message != string.Empty)
                        {
                            allViewModel.projectTimesheetApproveRejection.IsOnPageLoad = false;
                            TempData[TempDataKey + "Message"] = Message;
                            List<ProjectTimesheetSubmissionApproveRejectReportList> ProjectTimesheetSubmissionApproveRejectReportList = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
                            CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetApproveRejectList", ProjectTimesheetSubmissionApproveRejectReportList);
                            return RedirectToAction("GetAllTimesheetApprovalRejectionList");
                        }
                    }
                    else
                    {
                        SetApprovalStatusDropDownDataInViewBag();
                        return View(allViewModel);
                    }
                }
                else
                {
                    if(!(string.IsNullOrEmpty(allViewModel.projectTimesheetApproveRejectFilter.TimesheetMonth) && allViewModel.projectTimesheetApproveRejectFilter.TimesheetYear == 0))
                    {
                        ModelState.Clear();
                    }
                    if (ModelState.IsValid)
                    {
                        SetSearchFilterDropDownDataInViewBag();
                        ViewBag.DisplayGrid = true;
                        TempData["SelectedMonth"] = allViewModel.projectTimesheetApproveRejectFilter.TimesheetMonth;
                        TempData["SelectedYear"] = allViewModel.projectTimesheetApproveRejectFilter.TimesheetYear;
                        TempData["SelectedEmployee"] = allViewModel.projectTimesheetApproveRejectFilter.EmployeeCode;
                        List<ProjectTimesheetSubmissionApproveRejectReportList> ProjectTimesheetSubmissionApproveRejectReportList = new List<ProjectTimesheetSubmissionApproveRejectReportList>();
                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetApproveRejectList", ProjectTimesheetSubmissionApproveRejectReportList);
                        await GetTimeSheetApproveRejectList(allViewModel.projectTimesheetApproveRejectFilter.EmployeeCode, allViewModel.projectTimesheetApproveRejectFilter.TimesheetMonth, allViewModel.projectTimesheetApproveRejectFilter.TimesheetYear);
                        return View(allViewModel);
                    }
                    else
                    {
                        SetSearchFilterDropDownDataInViewBag();
                        return View(allViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.GetAllTimesheetApprovalRejectionList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Get Action Method is Used to Get Timesheet Data and Export To Excel.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileResult> DownloadTimesheetToExcel(int ProjectTimeSheetID, string EmployeeCode, string TimesheetMonth, int TimesheetYear)
        {
            FileContentResult result = null;
            try
            {
                string Title = string.Empty;
                string SheetName = string.Empty;
                string FileName = string.Empty;
                string TitleMonthYear = TimesheetMonth + " " + Convert.ToString(TimesheetYear);
                string SheetMonthYear = TimesheetMonth + "_" + Convert.ToString(TimesheetYear);
                SheetName = EmployeeCode + "_TimeSheet_" + SheetMonthYear;
                Title = "Timesheet - " + TitleMonthYear;
                FileName = SheetName + ".xlsx";

                ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection = new ProjectTimesheetApproveRejection();
                _objProjectTimesheetApproveRejection.ProjectTimeSheetID = ProjectTimeSheetID;
                _objProjectTimesheetApproveRejection.EmployeeCode = EmployeeCode;
                _objProjectTimesheetApproveRejection.TimesheetMonth = TimesheetMonth;
                _objProjectTimesheetApproveRejection.TimesheetYear = TimesheetYear;
                const int NoOfColumns = 9;
                DataTable dt = new DataTable("TimesheetReport");
                dt.Columns.AddRange(new DataColumn[NoOfColumns] {new DataColumn("SL No", typeof(Int64)),
                                            new DataColumn("Employee Code", typeof(string)),
                                            new DataColumn("Name", typeof(string)),
                                            new DataColumn("Task Date", typeof(string)),
                                            new DataColumn("Client", typeof(string)),
                                            new DataColumn("Project", typeof(string)),
                                            new DataColumn("Activity", typeof(string)),
                                            new DataColumn("Task Description", typeof(string)),
                                            new DataColumn("No. of Hours Worked", typeof(decimal))
                    });
                var dtList = await ITimesheetSvc.DownloadTimeSheet(_objProjectTimesheetApproveRejection, "DownloadTimesheet");
                if (dtList != null)
                {
                    List<ProjectTimesheetDownloadList> ProjectTimesheetDownloadListData = dtList.ToList();
                    foreach (var row in ProjectTimesheetDownloadListData)
                    {
                        dt.Rows.Add
                        (
                            row.SLNo, row.EmployeeCode, row.Name, row.DisplayTaskDate, CommonHelper.AddAmpersandToLast(row.ClientName), CommonHelper.AddAmpersandToLast(row.ProjectName), CommonHelper.AddAmpersandToLast(row.ActivityName), row.TaskDescription, row.TotalHoursWorked
                        );
                    }
                }
                result = ExcelHelper.ExportToExcel(dt, SheetName, Title, "A1:I1", 1, NoOfColumns, true, "H", 8, true, 2, true, 5, FileName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.DownloadTimesheetToExcel()", ex.Message, false);
            }
            return result;
        }

        /// <summary>
        /// This Method Is Used View Timesheet Details.
        /// </summary>
        /// <param name="ProjectTimeSheetID"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ViewTimeSheetDetails(int ProjectTimeSheetID, string EmployeeCode, string TimesheetMonth, int TimesheetYear)
        {
            List<ProjectTimesheetDownloadList> lstProjectTimesheetDownloadList = new List<ProjectTimesheetDownloadList>();
            bool IsContainData = false;
            try
            {
                if (ProjectTimeSheetID > 0 && !string.IsNullOrEmpty(EmployeeCode) && !string.IsNullOrEmpty(TimesheetMonth) && TimesheetYear > 0)
                {
                    ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection = new ProjectTimesheetApproveRejection();
                    _objProjectTimesheetApproveRejection.ProjectTimeSheetID = ProjectTimeSheetID;
                    _objProjectTimesheetApproveRejection.EmployeeCode = EmployeeCode;
                    _objProjectTimesheetApproveRejection.TimesheetMonth = TimesheetMonth;
                    _objProjectTimesheetApproveRejection.TimesheetYear = TimesheetYear;
                    var dtList = await ITimesheetSvc.DownloadTimeSheet(_objProjectTimesheetApproveRejection, "DownloadTimesheet");
                    if(dtList != null && dtList.Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.AddRange(new DataColumn[7] {new DataColumn("SLNo", typeof(Int64)),
                                            new DataColumn("DisplayTaskDate", typeof(string)),
                                            new DataColumn("ClientName", typeof(string)),
                                            new DataColumn("ProjectName", typeof(string)),
                                            new DataColumn("ActivityName", typeof(string)),
                                            new DataColumn("TaskDescription", typeof(string)),
                                            new DataColumn("TotalHoursWorked", typeof(decimal))
                        });
                        List<ProjectTimesheetDownloadList> ProjectTimesheetDownloadListData = dtList.ToList();
                        foreach (var row in ProjectTimesheetDownloadListData)
                        {
                            string formattedTaskDescription = CommonHelper.ConvertNumberedTextToHtmlList(row.TaskDescription, skipItems: 0);
                            dt.Rows.Add
                            (
                                row.SLNo, row.DisplayTaskDate, CommonHelper.AddAmpersandToLast(row.ClientName), CommonHelper.AddAmpersandToLast(row.ProjectName), CommonHelper.AddAmpersandToLast(row.ActivityName), formattedTaskDescription, row.TotalHoursWorked
                            );
                        }
                        lstProjectTimesheetDownloadList = Helper.ConvertDataTableToList<ProjectTimesheetDownloadList>(dt);
                        IsContainData = true;
                    }
                    else
                    {
                        IsContainData = false;
                    }
                }
            }
            catch (Exception ex)
            {
                IsContainData = false;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.ViewTimeSheetDetails()", ex.Message, false);
            }
            return Json(new { success = IsContainData, lstProjectTimesheetDownloadList });
        }

        #region [Custom Methods]

        /// <summary>
		/// This Method Is Used To Set Search Filter DropDown Data In View Bag.
		/// </summary>
		/// <returns></returns>
		public void SetSearchFilterDropDownDataInViewBag()
        {
            try
            {
                ViewBag.EmployeeList = DropdownHelper.GetEmployeesWorkingOnProjectWithAll(db);
                ViewBag.YearList = DropdownHelper.FillYearMonthList("Year");
                ViewBag.MonthList = DropdownHelper.FillYearMonthList("Month");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.SetSearchFilterDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
		/// This Method Is Used To Set Approval Status DropDown Data In View Bag.
		/// </summary>
		/// <returns></returns>
		public void SetApprovalStatusDropDownDataInViewBag()
        {
            try
            {
                ViewBag.ProjectTimesheetApprovalStatusList = DropdownHelper.FillProjectTimesheetApprovalStatusList();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetApprovalRejectionController.SetApprovalStatusDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        #endregion
    }
}
