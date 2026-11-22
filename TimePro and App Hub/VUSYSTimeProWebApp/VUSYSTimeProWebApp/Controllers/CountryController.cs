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
    public class CountryController : Controller
    {
		private readonly ICountry ICountrySvc = null;
		private readonly IDataProtector protector;

		public CountryController(ICountry Country, IDataProtectionProvider dataProtectionProvider,
								DataProtectionPurposeString dataProtectionPurposeString)
		{
			this.ICountrySvc = Country;
			protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
        }
		public IActionResult GetAllCountryList()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "CountryController.GetAllCountryList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			return View();
		}

		/// <summary>
		/// This Method Is Used To Get Country List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetCountryList()
		{
			string draw = string.Empty;
			string length = string.Empty;
			string start = string.Empty;
			int recordsTotal = 0;
			List<CountryM> actualdata = null;
			try
			{
				int ID = 0;
				List<CountryM> rList = null;
				var value = HttpContext.Session.GetString("CountryList");
				if (value == null || value == "[]")
				{
					rList = await ICountrySvc.GetCountryList(ID, "GetAllCountryList");
					CommonHelper.SetJsonSessionObject(HttpContext.Session, "CountryList", rList);
				}
				else
				{
					rList = JsonConvert.DeserializeObject<List<CountryM>>(value);
				}
				if (rList != null)
				{
					List<CountryM> CountryList = rList.ToList();
					CountryList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.Id.ToString()));

					#region Sorting

					var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

					var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
					//Sorting
					if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
					{
						switch (sortColumn)
						{
							case "CountryName":
								switch (sortColumnDirection)
								{
									case "asc":
										CountryList = CountryList.OrderBy(o => o.CountryName).ToList();
										break;
									case "desc":
										CountryList = CountryList.OrderByDescending(o => o.CountryName).ToList();
										break;
								}
								break;
							case "activeStatus":
								switch (sortColumnDirection)
								{
									case "asc":
										CountryList = CountryList.OrderBy(o => o.ActiveStatus).ToList();
										break;
									case "desc":
										CountryList = CountryList.OrderByDescending(o => o.ActiveStatus).ToList();
										break;

								}
								break;
							case "slNo":
								switch (sortColumnDirection)
								{
									case "asc":
										CountryList = CountryList.OrderBy(o => o.SLNo).ToList();
										break;
									case "desc":
										CountryList = CountryList.OrderByDescending(o => o.SLNo).ToList();
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
						CountryList = CountryList.Where(e => e.CountryName.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
					}
					#endregion


					#region Paging Functionality
					draw = HttpContext.Request.Form["draw"].FirstOrDefault();
					length = Request.Form["length"].FirstOrDefault();
					start = Request.Form["start"].FirstOrDefault();

					int pageSize = length != null ? Convert.ToInt32(length) : 0;
					int skip = start != null ? Convert.ToInt32(start) : 0;
					actualdata = CountryList.ToList();
					recordsTotal = 0;
					recordsTotal = actualdata.Count();

					if (pageSize != -1)
					{
						actualdata = CountryList.Skip(skip).Take(pageSize).ToList();
					}
					else
					{
						actualdata = CountryList.Skip(skip).Take(recordsTotal).ToList();
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "CountryController.GetCountryList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
		}
		[HttpGet]
		public IActionResult CreateCountry()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "CountryController.CreateCountry()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			return View();
		}
		/// <summary>
		/// This Post Action Method is Used to Save Country Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateCountry(AllViewModel allViewModel)
		{
			try
			{
				if (ModelState.IsValid)
				{
					allViewModel.CountryMaster.Id = 0;
					string Message = await SetAndSaveUpdateCountryDetails(allViewModel.CountryMaster.Id, allViewModel);
					if (Message != string.Empty)
					{
						if (Message == "The Country Details Already Exists.")
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "CountryController.CreateCountry()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			ModelState.Clear();
            return RedirectToAction("CreateCountry");
        }
		/// <summary>
		/// This Get Action Method is Used to Invoke Edit Country Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> EditCountry(string id, AllViewModel allViewModel)
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
					var CountryDetails = await ICountrySvc.GetCountryDetails(DecryptedID);
					allViewModel.CountryMaster = CountryDetails;
					CountryDetails.EncryptedId = protector.Protect(id.ToString());
					allViewModel.CountryMaster = CountryDetails;
					ViewBag.IsVisible = true;
					ViewBag.CountryID = Convert.ToInt32(DecryptedID);
					if (allViewModel != null)
					{
						return View(allViewModel);
					}
				}
				else
				{
					return RedirectToAction("GetAllCountryList");
				}
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "CountryController.EditCountry()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			return View();
		}

		/// <summary>
		/// This Post Action Method is Used to Update Country Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditCountry(AllViewModel allViewModel)
		{
			if (allViewModel.CountryMaster.Id == 0)
			{
				return PartialView("_Error500");
			}
			if (ModelState.IsValid)
			{
				try
				{
					string Message = await SetAndSaveUpdateCountryDetails(allViewModel.CountryMaster.Id, allViewModel);
					if (Message != string.Empty)
					{
                        if (Message == "The Country Details Already Exists.")
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
					Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "CountryController.EditCountry()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
				}
			}
			else
			{
				return View(allViewModel);
			}
			ModelState.Clear();
			return RedirectToAction("CreateCountry");
		}

		

		#region [Custom Methods]

		/// <summary>
		/// This Method Is Used To Save Or Update Country Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateCountryDetails(int ID, AllViewModel allViewModel)
		{
			string Message = string.Empty;
			try
			{
				allViewModel.CountryMaster.Id = ID;
				allViewModel.CountryMaster.IsDeleted = (allViewModel.CountryMaster.IsDeleted == true) ? true : false;
				var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
				allViewModel.CountryMaster.CreatedBy = SessionDetails.Id;
				allViewModel.CountryMaster.UpdatedBy = SessionDetails.Id;
				allViewModel.CountryMaster.Status = "ADD";
				Message = await ICountrySvc.SaveUpdateCountryDetails(allViewModel.CountryMaster);
				List<CountryM> CountryList = new List<CountryM>();
				CommonHelper.SetJsonSessionObject(HttpContext.Session, "CountryList", CountryList);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "CountryController.SetAndSaveUpdateCountryDetails()", ex.Message, false);
				throw;
			}
			return Message;
		}

		#endregion [Custom Methods]

	}
}
