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
    public class EmployeeController : Controller
    {
        private AppDbContext db = null;
        private readonly IEmployee IEmployeeSvc = null;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IDataProtector protector;

        public EmployeeController(AppDbContext appDbContext, IEmployee employee, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString, IWebHostEnvironment _webHostEnvironment)
        {
            db = appDbContext;
            this.IEmployeeSvc = employee;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
            webHostEnvironment = _webHostEnvironment;
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Employee List Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllEmployeeList()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.GetAllEmployeeList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Method Is Used To Get Employee List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetEmployeeList()
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<EmployeeM> actualdata = null;
            try
            {
                int ID = 0;
                List<EmployeeM> rList = null;
                var value = HttpContext.Session.GetString("EmployeeList");
                if (value == null || value == "[]")
                {
                    rList = await IEmployeeSvc.GetEmployeeList(ID, "GetAllEmployeeList");
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmployeeList", rList);
                }
                else
                {
                    rList = JsonConvert.DeserializeObject<List<EmployeeM>>(value);
                }
                if (rList != null)
                {
                    List<EmployeeM> EmployeeList = rList.ToList();
                    EmployeeList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.EmployeeID.ToString()));

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
                                        EmployeeList = EmployeeList.OrderBy(o => o.EmployeeCode).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.EmployeeCode).ToList();
                                        break;
                                }
                                break;
                            case "fullName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.FullName).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.FullName).ToList();
                                        break;
                                }
                                break;
                            case "genderName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.GenderName).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.GenderName).ToList();
                                        break;
                                }
                                break;
                            case "displayDateofBirth":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.DisplayDateofBirth).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.DisplayDateofBirth).ToList();
                                        break;
                                }
                                break;
                            case "mobileNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.MobileNo).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.MobileNo).ToList();
                                        break;
                                }
                                break;
                            case "emailID":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.EmailID).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.EmailID).ToList();
                                        break;
                                }
                                break;
                            case "employeeType":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.EmployeeType).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.EmployeeType).ToList();
                                        break;
                                }
                                break;
                            case "countryName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.CountryName).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.CountryName).ToList();
                                        break;
                                }
                                break;
                            case "stateName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.StateName).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.StateName).ToList();
                                        break;
                                }
                                break;
                            case "displayHireDate":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.DisplayHireDate).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.DisplayHireDate).ToList();
                                        break;
                                }
                                break;
                            case "activeStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.ActiveStatus).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.ActiveStatus).ToList();
                                        break;

                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        EmployeeList = EmployeeList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        EmployeeList = EmployeeList.OrderByDescending(o => o.SLNo).ToList();
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
                        EmployeeList = EmployeeList.Where(e => e.EmployeeCode.Contains(searchValue) || e.FullName.Contains(searchValue) || e.GenderName.Contains(searchValue) || e.DisplayDateofBirth.Contains(searchValue) || e.MobileNo.Contains(searchValue) || e.EmailID.Contains(searchValue) || e.EmployeeType.Contains(searchValue) || e.CountryName.ToString().Contains(searchValue) || e.StateName.Contains(searchValue) || e.DisplayHireDate.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = EmployeeList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = EmployeeList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = EmployeeList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.GetEmployeeList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Create Employee Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public IActionResult CreateEmployee()
        {
            try
            {
                bool IsAuthorize = CommonHelper.CheckPageAccess(HttpContext.Session, this.ControllerContext);
                if (IsAuthorize == false)
                {
                    return PartialView("_AccessDenied");
                }
                SetDropDownDataInViewBag();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.CreateEmployee()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Save Employee Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    allViewModel.EmployeeMaster.EmployeeID = 0;
                    string Message = await SetAndSaveUpdateEmployeeDetails(allViewModel.EmployeeMaster.EmployeeID, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Employee Details Already Exists.")
                        {
                            TempData[Helper.TEMP_DATA_EXIST_MESSAGE_KEY] = Message;
                            SetDropDownDataInViewBag();
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
                    SetDropDownDataInViewBag();
                    return View(allViewModel);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.CreateEmployee()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateEmployee");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Edit Employee Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> EditEmployee(string id, AllViewModel allViewModel)
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
                    var EmployeeDetails = await IEmployeeSvc.GetEmployeeDetails(DecryptedID);
                    allViewModel.EmployeeMaster = EmployeeDetails;
                    EmployeeDetails.EncryptedId = protector.Protect(id.ToString());
                    allViewModel.EmployeeMaster = EmployeeDetails;
                    ViewBag.IsVisible = true;
                    ViewBag.EmployeeID = Convert.ToInt32(DecryptedID);
                    SetDropDownDataInViewBag();
                    if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
                else
                {
                    return RedirectToAction("GetAllEmployeeList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.EditEmployee()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Update Employee Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(AllViewModel allViewModel)
        {
            if (allViewModel.EmployeeMaster.EmployeeID == 0)
            {
                return PartialView("_Error500");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = await SetAndSaveUpdateEmployeeDetails(allViewModel.EmployeeMaster.EmployeeID, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The Employee Details Already Exists.")
                        {
                            TempData[Helper.TEMP_DATA_EXIST_MESSAGE_KEY] = Message;
                            SetDropDownDataInViewBag();
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
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.EditEmployee()", ex.Message, false);
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
            return RedirectToAction("CreateEmployee");
        }

        #region [Custom Methods]

        /// <summary>
		/// This Method Is Used To Set DropDown Data In View Bag.
		/// </summary>
		/// <returns></returns>
		public void SetDropDownDataInViewBag()
        {
            try
            {
                ViewBag.GenderList = DropdownHelper.FillGenderMasterList(db);
                ViewBag.EmployeeTypeList = DropdownHelper.FillEmployeeTypeMasterList(db);
                ViewBag.CountryList = DropdownHelper.FillCountryMasterList(db);
                ViewBag.StateList = DropdownHelper.FillStateMasterList(db, 1);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.SetDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
		/// This Method Is Used To Get Gender Name.
		/// </summary>
		/// <returns></returns>
		public string GetGenderName(int GenderID)
        {
            string GenderName = string.Empty;
            try
            {
                var GenderData = (from c in db.GenderMasterList
                                  where c.Id == GenderID && c.IsDeleted == false
                                  select new { c.Gender }).ToList();
                if (GenderData != null && GenderData.Count > 0)
                {
                    GenderName = GenderData[0].Gender.ToUpper();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.GetGenderName()", ex.Message, false);
                throw;
            }
            return GenderName;
        }

        /// <summary>
		/// This Method Is Used To Save Or Update Employee Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateEmployeeDetails(int ID, AllViewModel allViewModel)
        {
            string Message = string.Empty;
            try
            {
                allViewModel.EmployeeMaster.EmployeeID = ID;
                var Files = Request.Form.Files;
                string ImageFileName = string.Empty;
                byte[] _myData = null;
                if (Files.Count > 0)
                {
                    ImageFileName = allViewModel.EmployeeMaster.ProfileUserImage.FileName;
                    string ImagePath = CommonHelper.GetActualPath(webHostEnvironment, "UploadedImages");

                    if (System.IO.File.Exists(ImagePath))
                    {
                        System.IO.File.Delete(ImagePath);
                    }
                    string filePath = Path.Combine(ImagePath, ImageFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await allViewModel.EmployeeMaster.ProfileUserImage.CopyToAsync(fileStream);
                    }
                    string webRootPath = webHostEnvironment.WebRootPath;
                    var fullPath = ImagePath + "\\" + ImageFileName;
                    _myData = CommonHelper.ConvertToBytes(allViewModel.EmployeeMaster.ProfileUserImage);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    allViewModel.EmployeeMaster.EmployeeImageFileName = ImageFileName;
                    allViewModel.EmployeeMaster.EmployeeProfileUserImage = _myData;
                }
                else
                {
                    if (allViewModel.EmployeeMaster.EmployeeID == 0)
                    {
                        string DefaultUserImageFileName = string.Empty;
                        string strGenderName = GetGenderName(allViewModel.EmployeeMaster.GenderID);
                        if (strGenderName == "MALE")
                        {
                            DefaultUserImageFileName = "Default_Male.png";
                        }
                        else if (strGenderName == "FEMALE")
                        {
                            DefaultUserImageFileName = "Default_Female.png";
                        }
                        string webRootPath = webHostEnvironment.WebRootPath;
                        var fullPath = webRootPath + "\\Images\\" + DefaultUserImageFileName;
                        _myData = System.IO.File.ReadAllBytes(fullPath);
                        allViewModel.EmployeeMaster.EmployeeImageFileName = DefaultUserImageFileName;
                        allViewModel.EmployeeMaster.EmployeeProfileUserImage = _myData;
                    }
                }
                allViewModel.EmployeeMaster.IsDeleted = (allViewModel.EmployeeMaster.IsDeleted == true) ? true : false;
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                allViewModel.EmployeeMaster.CreatedBy = SessionDetails.Id;
                allViewModel.EmployeeMaster.UpdatedBy = SessionDetails.Id;
                allViewModel.EmployeeMaster.Status = "ADD";
                Message = await IEmployeeSvc.SaveUpdateEmployeeDetails(allViewModel.EmployeeMaster);
                List<EmployeeM> employeeList = new List<EmployeeM>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmployeeList", employeeList);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "EmployeeController.SetAndSaveUpdateEmployeeDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        #endregion [Custom Methods]
    }
}
