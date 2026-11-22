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
    public class StateController : Controller
    {
		private AppDbContext db = null;
        private readonly IState IStatevc = null;
        private readonly IDataProtector protector;

        public StateController(AppDbContext appDbContext, IState State, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString)
        {
			db = appDbContext;
            this.IStatevc = State;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke State List Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetAllStateList()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.GetAllStateList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Method Is Used To Get State List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetStateList()
        {
            string draw = string.Empty;
            string length = string.Empty;
            string start = string.Empty;
            int recordsTotal = 0;
            List<StateM> actualdata = null;
            try
            {
                int ID = 0;
                List<StateM> rList = null;
                var value = HttpContext.Session.GetString("StateList");
                if (value == null || value == "[]")
                {
                    rList = await IStatevc.GetStateList(ID, "GetAllStateList");
                    CommonHelper.SetJsonSessionObject(HttpContext.Session, "StateList", rList);
                }
                else
                {
                    rList = JsonConvert.DeserializeObject<List<StateM>>(value);
                }
                if (rList != null)
                {
                    List<StateM> StateList = rList.ToList();
                    StateList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.Id.ToString()));

                    #region Sorting

                    var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                    var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                    //Sorting
                    if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                    {
                        switch (sortColumn)
                        {
                            case "StateName":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        StateList = StateList.OrderBy(o => o.StateName).ToList();
                                        break;
                                    case "desc":
                                        StateList = StateList.OrderByDescending(o => o.StateName).ToList();
                                        break;
                                }
                                break;
							case "StateCode":
								switch (sortColumnDirection)
								{
									case "asc":
										StateList = StateList.OrderBy(o => o.StateCode).ToList();
										break;
									case "desc":
										StateList = StateList.OrderByDescending(o => o.StateCode).ToList();
										break;
								}
								break;
							case "activeStatus":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        StateList = StateList.OrderBy(o => o.ActiveStatus).ToList();
                                        break;
                                    case "desc":
                                        StateList = StateList.OrderByDescending(o => o.ActiveStatus).ToList();
                                        break;

                                }
                                break;
                            case "slNo":
                                switch (sortColumnDirection)
                                {
                                    case "asc":
                                        StateList = StateList.OrderBy(o => o.SLNo).ToList();
                                        break;
                                    case "desc":
                                        StateList = StateList.OrderByDescending(o => o.SLNo).ToList();
                                        break;
                                }
                                break;
							case "countryName":
								switch (sortColumnDirection)
								{
									case "asc":
										StateList = StateList.OrderBy(o => o.CountryName).ToList();
										break;
									case "desc":
										StateList = StateList.OrderByDescending(o => o.CountryName).ToList();
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
                        StateList = StateList.Where(e => e.StateName.Contains(searchValue) || e.StateCode.Contains(searchValue) || e.CountryName.ToString().Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
                    }
                    #endregion


                    #region Paging Functionality
                    draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                    length = Request.Form["length"].FirstOrDefault();
                    start = Request.Form["start"].FirstOrDefault();

                    int pageSize = length != null ? Convert.ToInt32(length) : 0;
                    int skip = start != null ? Convert.ToInt32(start) : 0;
                    actualdata = StateList.ToList();
                    recordsTotal = 0;
                    recordsTotal = actualdata.Count();

                    if (pageSize != -1)
                    {
                        actualdata = StateList.Skip(skip).Take(pageSize).ToList();
                    }
                    else
                    {
                        actualdata = StateList.Skip(skip).Take(recordsTotal).ToList();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.GetStateList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Create State Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public IActionResult CreateState()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.CreateState()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Save State Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateState(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    allViewModel.StateMaster.Id = 0;
                    string Message = await SetAndSaveUpdateStateDetails(allViewModel.StateMaster.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The State Details Already Exists.")
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
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.CreateState()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            ModelState.Clear();
            return RedirectToAction("CreateState");
        }

        /// <summary>
		/// This Get Action Method is Used to Invoke Edit State Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> EditState(string id, AllViewModel allViewModel)
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
                    var StateDetails = await IStatevc.GetStateDetails(DecryptedID);
                    allViewModel.StateMaster = StateDetails;
                    StateDetails.EncryptedId = protector.Protect(id.ToString());
                    allViewModel.StateMaster = StateDetails;
                    ViewBag.IsVisible = true;
                    ViewBag.StateID = Convert.ToInt32(DecryptedID);
					SetDropDownDataInViewBag();
					if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
                else
                {
                    return RedirectToAction("GetAllStateList");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.EditState()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
		/// This Post Action Method is Used to Update State Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditState(AllViewModel allViewModel)
        {
            if (allViewModel.StateMaster.Id == 0)
            {
                return PartialView("_Error500");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = await SetAndSaveUpdateStateDetails(allViewModel.StateMaster.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The State Details Already Exists.")
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
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.EditState()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            else
            {
                return View(allViewModel);
            }
            ModelState.Clear();
            return RedirectToAction("CreateState");
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
				ViewBag.CountryList = DropdownHelper.FillCountryMasterList(db);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.SetDropDownDataInViewBag()", ex.Message, false);
				throw;
			}
		}

		/// <summary>
		/// This Method Is Used To Save Or Update State Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateStateDetails(int ID, AllViewModel allViewModel)
        {
            string Message = string.Empty;
            try
            {
                allViewModel.StateMaster.Id = ID;
                allViewModel.StateMaster.IsDeleted = (allViewModel.StateMaster.IsDeleted == true) ? true : false;
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                allViewModel.StateMaster.CreatedBy = SessionDetails.Id;
                allViewModel.StateMaster.UpdatedBy = SessionDetails.Id;
                allViewModel.StateMaster.Status = "ADD";
                Message = await IStatevc.SaveUpdateStateDetails(allViewModel.StateMaster);
                List<StateM> StateList = new List<StateM>();
                CommonHelper.SetJsonSessionObject(HttpContext.Session, "StateList", StateList);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "StateController.SetAndSaveUpdateStateDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        #endregion [Custom Methods]
    }
}
