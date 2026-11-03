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
using System.Data;
using CommonLib.Constants;
using ExcelHelper = VUSYSTimeProWebApp.DACHelper.ExcelHelper;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class TimesheetController : Controller
    {
        private AppDbContext db = null;
        private readonly ITimeSheet ITimesheetSvc = null;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IDataProtector protector;

        public TimesheetController(AppDbContext appDbContext, ITimeSheet timeSheet, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString, IWebHostEnvironment _webHostEnvironment)
        {
            db = appDbContext;
            this.ITimesheetSvc = timeSheet;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
            webHostEnvironment = _webHostEnvironment;
        }

        #region [Log Timesheet]

        /// <summary>
        /// This Action Method Is Used To Invoke Timesheet Screen
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllTimesheetList(AllViewModel allViewModel)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                ProjectTimesheetFilter _objProjectTimesheetFilter = new ProjectTimesheetFilter();
                _objProjectTimesheetFilter.FromTaskDate = DateTimeHelper.GetFirstDateofMonth();
                _objProjectTimesheetFilter.ToTaskDate = DateTimeHelper.GetLastDateofMonth();
                allViewModel.projectTimesheetFilter = _objProjectTimesheetFilter;
                ViewBag.DisplayGrid = true;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.GetAllTimesheetList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Post Action Method is Used to Get Timesheet List Between From and To Date Task Date.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllTimesheetList(AllViewModel allViewModel, string action)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string FromDate = allViewModel.projectTimesheetFilter.FromTaskDate.ToString(DateTimeFormatConstants.YYYYMMDD_DATEFORMAT);
                    string ToDate = allViewModel.projectTimesheetFilter.ToTaskDate.ToString(DateTimeFormatConstants.YYYYMMDD_DATEFORMAT);
                    ViewBag.DisplayGrid = true;
                    List<ProjectTimesheet> ProjectTimesheet = new List<ProjectTimesheet>();
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetList", ProjectTimesheet);
                    await GetTimeSheetList(FromDate, ToDate);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.GetAllTimesheetList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Method Is Used To Get Timesheet List and Display In The Grid.
        /// </summary>
        /// <param name="FromTaskDate"></param>
        /// <param name="ToTaskDate"></param>
        /// <returns></returns>
		public async Task<IActionResult> GetTimeSheetList(string FromTaskDate, string ToTaskDate)
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<ProjectTimesheet> actualdata = null;
            try
            {
                List<ProjectTimesheet> ptList = null;
                var value = HttpContext.Session.GetString("TimesheetList");
                if (value == null || value == "[]")
                {
                    var SessionDetails = CommonHelper.GetUserSessionDetails(HttpContext.Session);
                    ptList = await ITimesheetSvc.GetTimesheetList(0, SessionDetails.EmployeeCode, FromTaskDate, ToTaskDate, "ByTaskDate");
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TimesheetList", ptList);
                }
                else
                {
                    ptList = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(value);
                }
                if (ptList != null)
                {
                    List<ProjectTimesheet> TimesheetList = ptList.ToList();
                    TimesheetList.ToList().ForEach(u => u.EncryptedId = protector.Protect($"{u.ProjectTimeSheetID.ToString() + "^" + u.TaskDate.ToString() + "^" + u.EmployeeCode}"));

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "clientName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetList = TimesheetList.OrderBy(o => o.ClientName).ToList();
                                        break;
                                    case "desc":
                                        TimesheetList = TimesheetList.OrderByDescending(o => o.ClientName).ToList();
                                        break;
                                }
                                break;

                            case "projectName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetList = TimesheetList.OrderBy(o => o.ProjectName).ToList();
                                        break;
                                    case "desc":
                                        TimesheetList = TimesheetList.OrderByDescending(o => o.ProjectName).ToList();
                                        break;
                                }
                                break;

                            case "displayTaskDate":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetList = TimesheetList.OrderByDescending(o => o.DisplayTaskDate).ToList();
                                        break;
                                    case "desc":
                                        TimesheetList = TimesheetList.OrderByDescending(o => o.DisplayTaskDate).ToList();
                                        break;
                                }
                                break;
                            case "totalHoursWorked":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetList = TimesheetList.OrderBy(o => o.TotalHoursWorked).ToList();
                                        break;
                                    case "desc":
                                        TimesheetList = TimesheetList.OrderByDescending(o => o.TotalHoursWorked).ToList();
                                        break;
                                }
                                break;
                            case "projectStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetList = TimesheetList.OrderBy(o => o.ProjectStatus).ToList();
                                        break;
                                    case "desc":
                                        TimesheetList = TimesheetList.OrderByDescending(o => o.ProjectStatus).ToList();
                                        break;
                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        TimesheetList = TimesheetList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        TimesheetList = TimesheetList.OrderByDescending(o => o.SLNo).ToList();
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
                        TimesheetList = TimesheetList.Where(e => e.ClientName.Contains(searchValue) || e.ProjectName.Contains(searchValue) || e.DisplayTaskDate.Contains(searchValue) || e.TotalHoursWorked.ToString().Contains(searchValue) || e.ProjectStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = TimesheetList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = TimesheetList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = TimesheetList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.GetTimeSheetList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Create Timesheet Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public IActionResult CreateTimesheet()
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                var SessionDetails = CommonHelper.GetUserSessionDetails(HttpContext.Session);
                var IsApproved = DropdownHelper.CheckTimesheetApproved(db, SessionDetails.EmployeeCode);
                if (!IsApproved)
                {
                    var IsSentForApproval = DropdownHelper.CheckTimesheetSentForApproval(db, SessionDetails.EmployeeCode);
                    if (!IsSentForApproval)
                    {
                        var IsClosed = DropdownHelper.CheckCurrentMonthTimesheetClosedStatus(db, SessionDetails.EmployeeCode);
                        if (!IsClosed)
                        {
                            var value = HttpContext.Session.GetString("TempTimesheetList");
                            if (value != null && value != "[]")
                            {
                                List<ProjectTimesheet> tempTimesheetList = null;
                                tempTimesheetList = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(value);
                                List<ProjectTimesheet> tList = tempTimesheetList.ToList();
                                ViewBag.TempTimesheetList = tList;
                            }
                            SetDropDownDataInViewBag();
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.TIMESHEET_CLOSED;
                            return RedirectToAction("GetAllTimesheetList");
                        }
                    }
                    else
                    {
                        TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Create", "NotApproved");
                        return RedirectToAction("GetAllTimesheetList");
                    }
                }
                else
                {
                    TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Create", "Approved");
                    return RedirectToAction("GetAllTimesheetList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.CreateTimesheet()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Save Timesheet Details.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <param name="action"></param>
        /// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimesheet(AllViewModel allViewModel, string action)
        {
            string Message = string.Empty;
            try
            {
                var SessionDetails = CommonHelper.GetUserSessionDetails(HttpContext.Session);
                switch (action)
                {
                    case "Add":
                        List<ProjectTimesheet> tempTimesheetList = new List<ProjectTimesheet>();
                        if (ModelState.IsValid)
                        {
                            var IsApproved = DropdownHelper.CheckTimesheetApproved(db, SessionDetails.EmployeeCode, allViewModel.projectTimesheet.TaskDate);
                            if (!IsApproved)
                            {
                                var IsSentForApproval = DropdownHelper.CheckTimesheetSentForApproval(db, SessionDetails.EmployeeCode, allViewModel.projectTimesheet.TaskDate);
                                if (!IsSentForApproval)
                                {
                                    var IsClosed = DropdownHelper.CheckCurrentMonthTimesheetClosedStatus(db, SessionDetails.EmployeeCode);
                                    if (!IsClosed)
                                    {
                                        var value = HttpContext.Session.GetString("TempTimesheetList");
                                        if (value != null && value != "[]")
                                        {
                                            List<ProjectTimesheet> tempSessionTimesheetList = null;
                                            tempSessionTimesheetList = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(value);
                                            List<ProjectTimesheet> tList = tempSessionTimesheetList.ToList();
                                            tempTimesheetList = tList;
                                        }
                                        var IsExists = tempTimesheetList.Any(a => a.ProjectID == allViewModel.projectTimesheet.ProjectID && a.TaskDate == allViewModel.projectTimesheet.TaskDate && a.ProjectActivityID == allViewModel.projectTimesheet.ProjectActivityID);
                                        if (IsExists)
                                        {
                                            // Find the existing item to update
                                            var existingItem = tempTimesheetList.FirstOrDefault(a => a.ProjectID == allViewModel.projectTimesheet.ProjectID && a.TaskDate == allViewModel.projectTimesheet.TaskDate && a.EmployeeCode == SessionDetails.EmployeeCode);

                                            if (existingItem != null)
                                            {
                                                existingItem.TaskDescriptionRaw = allViewModel.projectTimesheet.TaskDescription;
                                                existingItem.EmployeeCode = SessionDetails.EmployeeCode;
                                                existingItem.Status = "ADD";
                                                existingItem.CreatedBy = SessionDetails.Id;
                                                existingItem.NoofHoursWorked = allViewModel.projectTimesheet.NoofHoursWorked;
                                                existingItem.ProjectName = DropdownHelper.GetProjectNameByProjectID(db, allViewModel.projectTimesheet.ProjectID);
                                                existingItem.IsOnLeave = allViewModel.projectTimesheet.IsOnLeave;
                                                existingItem.ActivityName = DropdownHelper.GetProjectActivityNameByProjectActivityID(db, allViewModel.projectTimesheet.ProjectActivityID);
                                                existingItem.TaskDescription = CommonHelper.GetFormattedStringFromRawHTML(allViewModel.projectTimesheet.TaskDescription);
                                            }
                                        }
                                        else
                                        {
                                            Random random = new Random();
                                            ProjectTimesheet _objProjectTimesheet = new ProjectTimesheet();
                                            _objProjectTimesheet = allViewModel.projectTimesheet;
                                            _objProjectTimesheet.TaskDescriptionRaw = allViewModel.projectTimesheet.TaskDescription;
                                            _objProjectTimesheet.EmployeeCode = SessionDetails.EmployeeCode;
                                            _objProjectTimesheet.Status = "ADD";
                                            _objProjectTimesheet.Mode = "Create";
                                            _objProjectTimesheet.ProjectTimeSheetDetailID = random.Next(0, 101);
                                            _objProjectTimesheet.CreatedBy = SessionDetails.Id;
                                            _objProjectTimesheet.NoofHoursWorked = allViewModel.projectTimesheet.NoofHoursWorked;
                                            _objProjectTimesheet.ProjectName = DropdownHelper.GetProjectNameByProjectID(db, allViewModel.projectTimesheet.ProjectID);
                                            _objProjectTimesheet.IsOnLeave = allViewModel.projectTimesheet.IsOnLeave;
                                            _objProjectTimesheet.ActivityName = DropdownHelper.GetProjectActivityNameByProjectActivityID(db, allViewModel.projectTimesheet.ProjectActivityID);
                                            _objProjectTimesheet.TaskDescription = CommonHelper.GetFormattedStringFromRawHTML(allViewModel.projectTimesheet.TaskDescription);
                                            tempTimesheetList.Add(_objProjectTimesheet);
                                        }
                                        // Adding SLNo using LINQ's ForEach in a single line
                                        tempTimesheetList.Select((timesheet, index) => { timesheet.SLNo = index + 1; return timesheet; }).ToList();
                                        CommonHelper.SetJsonSessionObject(HttpContext.Session, "TempTimesheetList", tempTimesheetList);
                                        SetDropDownDataInViewBag();
                                    }
                                    else
                                    {
                                        SetDropDownDataInViewBag();
                                        TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.TIMESHEET_CLOSED;
                                        return View(allViewModel);
                                    }
                                }
                                else
                                {
                                    SetDropDownDataInViewBag();
                                    TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Create", "NotApproved", allViewModel.projectTimesheet.TaskDate);
                                    return View(allViewModel);
                                }
                            }
                            else
                            {
                                SetDropDownDataInViewBag();
                                TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Create", "Approved", allViewModel.projectTimesheet.TaskDate);
                                return View(allViewModel);
                            }
                        }
                        else
                        {
                            SetDropDownDataInViewBag();
                            return View(allViewModel);
                        }
                        break;
                    case "Save":
                        SetDropDownDataInViewBag();
                        List<ProjectTimesheet> TimesheetListSaveToDB = new List<ProjectTimesheet>();

                        var selectedRecords = HttpContext.Session.GetString("TempTimesheetList");
                        if (selectedRecords != null && selectedRecords != "[]")
                        {
                            TimesheetListSaveToDB = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(selectedRecords);

                            TimesheetListSaveToDB.ForEach(ts =>
                            {
                                if (ts.Mode != "EditFromDB")
                                {
                                    ts.ProjectTimeSheetDetailID = 0;
                                }
                            });

                            bool isAnyTaskApproved = false;
                            bool isSentForApproval = false;
                            DateTime? tDate = null;

                            foreach (var item in TimesheetListSaveToDB)
                            {
                                tDate = item.TaskDate;
                                if (DropdownHelper.CheckTimesheetApproved(db, SessionDetails.EmployeeCode, item.TaskDate))
                                {
                                    isAnyTaskApproved = true;
                                    break; // No need to continue checking if one is approved
                                }
                            }
                            if (isAnyTaskApproved)
                            {
                                TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Create", "Approved", tDate);
                                ModelState.Clear();
                                return View(allViewModel);
                            }

                            foreach (var item in TimesheetListSaveToDB)
                            {
                                tDate = item.TaskDate;
                                if (DropdownHelper.CheckTimesheetSentForApproval(db, SessionDetails.EmployeeCode, item.TaskDate))
                                {
                                    isSentForApproval = true;
                                    break; // No need to continue checking if it is sent for Approval
                                }
                            }

                            if (isSentForApproval)
                            {
                                TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Create", "NotApproved", tDate);
                                ModelState.Clear();
                                return View(allViewModel);
                            }

                            var IsClosed = DropdownHelper.CheckCurrentMonthTimesheetClosedStatus(db, SessionDetails.EmployeeCode);
                            if (!IsClosed)
                            {
                                string Status = string.Empty;
                                Message = ITimesheetSvc.SaveTimesheet(TimesheetListSaveToDB, ref Status);
                                if (Message != string.Empty)
                                {
                                    switch (Status)
                                    {
                                        case "Success":
                                            TempData[Status + "Message"] = Message;
                                            ModelState.Clear();
                                            CommonHelper.RemoveSessionObject(HttpContext.Session, "TempTimesheetList");
                                            CommonHelper.RemoveSessionObject(HttpContext.Session, "TimesheetList");
                                            break;
                                        case "Error":
                                            TempData[Status + "Message"] = Message;
                                            break;
                                    }
                                    return (Status == "Success" ? RedirectToAction("CreateTimesheet") : View(allViewModel));
                                }
                            }
                            else
                            {
                                TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.TIMESHEET_CLOSED;
                                ModelState.Clear();
                                return View(allViewModel);
                            }
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = "Add Timesheet Details and Click on Save Timesheet Button !!!";
                            ModelState.Clear();
                            return View(allViewModel);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.CreateTimesheet()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateTimesheet");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Edit Time Sheet Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> EditTimesheet(string id, string source, AllViewModel allViewModel)
        {
            try
            {
                if (source == "List")
                {
                    string DecryptedValue = protector.Unprotect(id);
                    string[] DecryptedValueArray = DecryptedValue.Split("^");
                    int ProjectTimeSheetID = 0;
                    string EmployeeCode = string.Empty;
                    DateTime TaskDate;
                    if (DecryptedValueArray != null && DecryptedValueArray.Length > 0)
                    {
                        ProjectTimeSheetID = Convert.ToInt32(DecryptedValueArray[0]);
                        TaskDate = Convert.ToDateTime(DecryptedValueArray[1]);
                        EmployeeCode = DecryptedValueArray[2];
                        var IsApproved = DropdownHelper.CheckTimesheetApproved(db, EmployeeCode, TaskDate);
                        if (!IsApproved)
                        {
                            var IsSentForApproval = DropdownHelper.CheckTimesheetSentForApproval(db, EmployeeCode, TaskDate);
                            if (!IsSentForApproval)
                            {
                                List<ProjectTimesheet> TimesheetList = await ITimesheetSvc.GetTimesheetList(ProjectTimeSheetID, EmployeeCode, TaskDate.ToShortDateString(), string.Empty, "GetProjectTimeSheetDetails");
                                if (TimesheetList != null && TimesheetList.Count > 0)
                                {
                                    TimesheetList.ToList().ForEach(u => u.Mode = "EditFromDB");
                                    CommonHelper.RemoveSessionObject(HttpContext.Session, "TempTimesheetList");
                                    TimesheetList.Select((timesheet, index) => { timesheet.SLNo = index + 1; return timesheet; }).ToList();
                                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "TempTimesheetList", TimesheetList);
                                    ViewBag.TempTimesheetList = TimesheetList;
                                    SetDropDownDataInViewBag();
                                    return View();
                                }
                            }
                            else
                            {
                                TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Edit", "NotApproved", TaskDate);
                                return RedirectToAction("GetAllTimesheetList");
                            }
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = CommonHelper.GetCustomTimesheetErrorMessage("Edit", "Approved", TaskDate);
                            return RedirectToAction("GetAllTimesheetList");
                        }
                    }
                }
                else
                {
                    int TempProjectTimeSheetDetailID = Convert.ToInt32(id);
                    if (TempProjectTimeSheetDetailID > 0)
                    {
                        var value = HttpContext.Session.GetString("TempTimesheetList");
                        if (value != null && value != "[]")
                        {
                            List<ProjectTimesheet> tempTimesheetList = null;
                            tempTimesheetList = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(value);
                            List<ProjectTimesheet> tList = tempTimesheetList.ToList();
                            var ExistingFormData = tList.FirstOrDefault(f => f.ProjectTimeSheetDetailID == TempProjectTimeSheetDetailID);
                            if (ExistingFormData != null)
                            {
                                ExistingFormData.TaskDescription = ExistingFormData.TaskDescriptionRaw;
                                allViewModel.projectTimesheet = ExistingFormData;
                            }
                            SetDropDownDataInViewBag();
                            if (allViewModel != null)
                            {
                                return View(allViewModel);
                            }
                        }
                        else
                        {
                            return RedirectToAction("GetAllTimesheetList");
                        }
                    }
                    else
                    {
                        return RedirectToAction("GetAllTimesheetList");
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.EditTimesheet()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Update Timesheet Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTimesheet(AllViewModel allViewModel, string ProjectID)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var value = HttpContext.Session.GetString("TempTimesheetList");
                    if (value != null && value != "[]")
                    {
                        List<ProjectTimesheet> tempTimesheetList = null;
                        tempTimesheetList = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(value);
                        List<ProjectTimesheet> tList = tempTimesheetList.ToList();
                        var ExistingFormData = tList.FirstOrDefault(f => f.ProjectTimeSheetDetailID == allViewModel.projectTimesheet.ProjectTimeSheetDetailID);
                        if (ExistingFormData != null)
                        {
                            var SessionDetails = CommonHelper.GetUserSessionDetails(HttpContext.Session);
                            //Update the Existing Form Data.
                            ExistingFormData.ProjectID = (ProjectID == null ? allViewModel.projectTimesheet.ProjectID : Convert.ToInt32(ProjectID));
                            ExistingFormData.EmployeeCode = SessionDetails.EmployeeCode;
                            ExistingFormData.ProjectName = DropdownHelper.GetProjectNameByProjectID(db, (ProjectID == null ? allViewModel.projectTimesheet.ProjectID : Convert.ToInt32(ProjectID)));
                            ExistingFormData.IsOnLeave = allViewModel.projectTimesheet.IsOnLeave;
                            ExistingFormData.ActivityName = DropdownHelper.GetProjectActivityNameByProjectActivityID(db, allViewModel.projectTimesheet.ProjectActivityID);
                            ExistingFormData.ProjectActivityID = allViewModel.projectTimesheet.ProjectActivityID;
                            ExistingFormData.TaskDate = allViewModel.projectTimesheet.TaskDate;
                            ExistingFormData.TaskDescriptionRaw = allViewModel.projectTimesheet.TaskDescription;
                            ExistingFormData.TaskDescription = CommonHelper.GetFormattedStringFromRawHTML(allViewModel.projectTimesheet.TaskDescription);
                            ExistingFormData.NoofHoursWorked = allViewModel.projectTimesheet.NoofHoursWorked;
                            ExistingFormData.CreatedBy = SessionDetails.Id;
                            ExistingFormData.UpdatedBy = SessionDetails.Id;

                            // Adding SLNo using LINQ's ForEach in a single line
                            tList.Select((timesheet, index) => { timesheet.SLNo = index + 1; return timesheet; }).ToList();
                            CommonHelper.SetJsonSessionObject(HttpContext.Session, "TempTimesheetList", tList);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.EditTimesheet()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            else
            {
                SetDropDownDataInViewBag();
                return View(allViewModel);
            }
            ModelState.Clear();
            return RedirectToAction("CreateTimesheet");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Delete Time Sheet Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> DeleteTimesheet(int id, AllViewModel allViewModel)
        {
            try
            {
                if (id > 0)
                {
                    var value = HttpContext.Session.GetString("TempTimesheetList");
                    if (value != null && value != "[]")
                    {
                        List<ProjectTimesheet> tempTimesheetList = null;
                        tempTimesheetList = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(value);
                        List<ProjectTimesheet> tList = tempTimesheetList.ToList();
                        var ExistingFormData = tList.FirstOrDefault(f => f.ProjectTimeSheetDetailID == id);
                        if (ExistingFormData != null)
                        {
                            ExistingFormData.IsDeleted = true;
                            //tList.Remove(ExistingFormData);
                            if (tList != null && tList.Count > 0)
                            {
                                // Adding SLNo using LINQ's ForEach in a single line
                                tList.Select((timesheet, index) => { timesheet.SLNo = index + 1; return timesheet; }).ToList();
                                CommonHelper.SetJsonSessionObject(HttpContext.Session, "TempTimesheetList", tList);
                            }
                            else
                            {
                                List<ProjectTimesheet> lstProjectTimesheet = new List<ProjectTimesheet>();
                                CommonHelper.SetJsonSessionObject(HttpContext.Session, "TempTimesheetList", tList);
                            }
                            return RedirectToAction("CreateTimesheet");
                        }
                    }
                    else
                    {
                        return RedirectToAction("GetAllTimesheetList");
                    }
                }
                else
                {
                    return RedirectToAction("GetAllTimesheetList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.EditTimesheet()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        #endregion [Log Timesheet]

        #region [Download Timesheet]

        /// <summary>
        /// This Get Action Method is Used to Invoke Download Timesheet Screen.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult DownloadTimesheet(AllViewModel allViewModel)
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                SetDownloadTimesheetFilterDropDownDataInViewBag();
                DownloadTimesheetFilter _objDownloadTimesheetFilter = new DownloadTimesheetFilter();
                if (TempData["DownloadTimesheetFilter"] != null)
                {
                    var DownloadTimesheetFilter = JsonConvert.DeserializeObject<DownloadTimesheetFilter>(TempData["DownloadTimesheetFilter"].ToString());
                    _objDownloadTimesheetFilter.EmployeeCode = DownloadTimesheetFilter.EmployeeCode;
                    _objDownloadTimesheetFilter.TimesheetMonth = DownloadTimesheetFilter.TimesheetMonth;
                    _objDownloadTimesheetFilter.TimesheetYear = DownloadTimesheetFilter.TimesheetYear;
                }
                else
                {
                    _objDownloadTimesheetFilter.TimesheetMonth = DateTimeHelper.GetMonthName(DateTime.Now.Month);
                    _objDownloadTimesheetFilter.TimesheetYear = DateTime.Now.Year;
                }
                allViewModel.downloadTimesheetFilter = _objDownloadTimesheetFilter;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.DownloadTimesheet()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View(allViewModel);
        }

        /// <summary>
		/// This Post Action Method is Used to Download Timesheet Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadTimesheet(AllViewModel allViewModel, string action)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    return RedirectToAction("DownloadTimesheetToExcel", new { ActionButton = action, EmployeeCode = allViewModel.downloadTimesheetFilter.EmployeeCode, TimesheetMonth = allViewModel.downloadTimesheetFilter.TimesheetMonth, TimesheetYear = allViewModel.downloadTimesheetFilter.TimesheetYear });
                }
                else
                {
                    SetDownloadTimesheetFilterDropDownDataInViewBag();
                    return View(allViewModel);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.DownloadTimesheet()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
        /// This Method Is Used View Timesheet Details.
        /// </summary>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ViewTimeSheetDetails(string EmployeeCode, string TimesheetMonth, int TimesheetYear)
        {
            List<ProjectTimesheetDownloadList> lstProjectTimesheetDownloadList = new List<ProjectTimesheetDownloadList>();
            bool IsContainData = false;
            try
            {
                if (!string.IsNullOrEmpty(EmployeeCode) && !string.IsNullOrEmpty(TimesheetMonth) && TimesheetYear > 0)
                {
                    ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection = new ProjectTimesheetApproveRejection();
                    _objProjectTimesheetApproveRejection.ProjectTimeSheetID = 0;
                    _objProjectTimesheetApproveRejection.EmployeeCode = EmployeeCode;
                    _objProjectTimesheetApproveRejection.TimesheetMonth = TimesheetMonth;
                    _objProjectTimesheetApproveRejection.TimesheetYear = TimesheetYear;
                    var dtList = await ITimesheetSvc.DownloadTimeSheet(_objProjectTimesheetApproveRejection, "DownloadEmployeeTimesheet");
                    if (dtList != null && dtList.Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.AddRange(new DataColumn[9] {new DataColumn("SLNo", typeof(Int64)),
                                            new DataColumn("EmployeeCode", typeof(string)),
                                            new DataColumn("Name", typeof(string)),
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
                                row.SLNo, row.EmployeeCode, row.Name, row.DisplayTaskDate, CommonHelper.AddAmpersandToLast(row.ClientName), CommonHelper.AddAmpersandToLast(row.ProjectName), CommonHelper.AddAmpersandToLast(row.ActivityName), formattedTaskDescription, row.TotalHoursWorked
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.ViewTimeSheetDetails()", ex.Message, false);
            }
            return Json(new { success = IsContainData, lstProjectTimesheetDownloadList });
        }

        /// <summary>
        /// This Get Action Method is Used to Get Timesheet Data and Export To Excel.
        /// </summary>
        /// <param name="ActionButton"></param>
        /// <param name="EmployeeCode"></param>
        /// <param name="TimesheetMonth"></param>
        /// <param name="TimesheetYear"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DownloadTimesheetToExcel(string ActionButton, string EmployeeCode, string TimesheetMonth, int TimesheetYear)
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
                FileName = (ActionButton == "DownloadTimesheet" ? SheetName + ".xlsx" : SheetName + "_DateWise" + ".xlsx");
                string ParamType = (ActionButton == "DownloadTimesheet" ? "DownloadEmployeeTimesheet" : "DownloadEmployeeTimesheetDateWise");

                ProjectTimesheetApproveRejection _objProjectTimesheetApproveRejection = new ProjectTimesheetApproveRejection();
                _objProjectTimesheetApproveRejection.ProjectTimeSheetID = 0;
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
                var dtList = await ITimesheetSvc.DownloadTimeSheet(_objProjectTimesheetApproveRejection, ParamType);
                if (dtList != null && dtList.Count > 0)
                {
                    List<ProjectTimesheetDownloadList> ProjectTimesheetDownloadListData = dtList.ToList();
                    foreach (var row in ProjectTimesheetDownloadListData)
                    {
                        dt.Rows.Add
                        (
                            row.SLNo, row.EmployeeCode, row.Name, row.DisplayTaskDate, CommonHelper.AddAmpersandToLast(row.ClientName), CommonHelper.AddAmpersandToLast(row.ProjectName), CommonHelper.AddAmpersandToLast(row.ActivityName), row.TaskDescription, row.TotalHoursWorked
                        );
                    }
                    result = ExcelHelper.ExportToExcel(dt, SheetName, Title, "A1:I1", 1, NoOfColumns, true, "H", 8, true, 2, true, 5, FileName);
                }
                else
                {
                    string ErrorMessage = $"The Timesheet For {TimesheetMonth}-{TimesheetYear} is not available for download !!!";
                    TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = ErrorMessage;
                    DownloadTimesheetFilter _objDownloadTimesheetFilter = new DownloadTimesheetFilter();
                    _objDownloadTimesheetFilter.TimesheetMonth = TimesheetMonth;
                    _objDownloadTimesheetFilter.TimesheetYear = TimesheetYear;
                    _objDownloadTimesheetFilter.EmployeeCode = EmployeeCode;
                    TempData["DownloadTimesheetFilter"] = JsonConvert.SerializeObject(_objDownloadTimesheetFilter);
                    return RedirectToAction("DownloadTimesheet");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.DownloadTimesheetToExcel()", ex.Message, false);
            }
            return result;
        }

        #endregion [Download Timesheet]



        #region [Custom Methods]

        /// <summary>
        /// This Method Is Used To Get Leave Activity.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetLeaveActivity(bool IsOnLeave)
        {
            List<ProjectActivityM> lstProjectActivityM = new List<ProjectActivityM>();
            try
            {
                lstProjectActivityM = DropdownHelper.GetProjectLeaveActivityName(db, IsOnLeave);
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.GetLeaveActivity()", ex.Message, false);
            }
            return Json(lstProjectActivityM);
        }

        /// <summary>
        /// This Method Is Used To Get Project Timesheet Details.
        /// </summary>
        /// <param name="ProjectTimesheetDetailID"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetProjectTimesheetDetails(int ProjectTimesheetDetailID)
        {
            ProjectTimesheet _objProjectTimesheet = new ProjectTimesheet();
            try
            {
                if (ProjectTimesheetDetailID > 0)
                {
                    var value = HttpContext.Session.GetString("TempTimesheetList");

                    if (value != null && value != "[]")
                    {
                        List<ProjectTimesheet> tempTimesheetList = null;
                        tempTimesheetList = JsonConvert.DeserializeObject<List<ProjectTimesheet>>(value);
                        List<ProjectTimesheet> tList = tempTimesheetList.ToList();
                        var FilteredData = tList.FirstOrDefault(f => f.ProjectTimeSheetDetailID == ProjectTimesheetDetailID);
                        if(FilteredData != null)
                        {
                            _objProjectTimesheet = FilteredData;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.GetProjectTimesheetDetails()", ex.Message, false);
            }
            return Json(_objProjectTimesheet);
        }

        /// <summary>
		/// This Method Is Used To Set DropDown Data In View Bag.
		/// </summary>
		/// <returns></returns>
		public void SetDropDownDataInViewBag()
        {
            try
            {
                var SessionDetails = CommonHelper.GetUserSessionDetails(HttpContext.Session);
                string EmployeeCode = string.Empty;
                if(SessionDetails != null)
                {
                    EmployeeCode = SessionDetails.EmployeeCode;
                }
                ViewBag.ProjectList = DropdownHelper.FillProjectNameBasedOnEmployeeCode(db, EmployeeCode);
                ViewBag.ProjectActivityList = DropdownHelper.FillProjectActivityList(db);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "TimesheetController.SetDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
		/// This Method Is Used To Set Download Timesheet Filter DropDown Data In View Bag.
		/// </summary>
		/// <returns></returns>
		public void SetDownloadTimesheetFilterDropDownDataInViewBag()
        {
            try
            {
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                if (SessionDetails != null)
                {
                    var UserRole = SessionDetails.UserRole;
                    if (UserRole == "Employee")
                    {
                        ViewBag.EmployeeList = DropdownHelper.GetLoggedInEmployeeWorkingOnProject(db, SessionDetails.EmployeeCode);
                        ViewBag.ShowViewTimeSheet = false;
                    }
                    else
                    {
                        ViewBag.EmployeeList = DropdownHelper.GetEmployeesWorkingOnProject(db);
                        ViewBag.ShowViewTimeSheet = true;
                        ViewBag.ShowTimeSheetDetailsPopup = true;
                    }
                }
                ViewBag.YearList = DropdownHelper.FillYearMonthList("Year");
                ViewBag.MonthList = DropdownHelper.FillYearMonthList("Month");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "TimesheetController.SetDownloadTimesheetFilterDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        #endregion [Custom Methods]
    }
}
