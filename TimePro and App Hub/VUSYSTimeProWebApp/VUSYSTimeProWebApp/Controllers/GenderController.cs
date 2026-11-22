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
	public class GenderController : Controller
	{
		private readonly IGender IGendervc = null;
		private readonly IDataProtector protector;

		public GenderController(IGender Gender, IDataProtectionProvider dataProtectionProvider,
								DataProtectionPurposeString dataProtectionPurposeString)
		{
			this.IGendervc = Gender;
			protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
        }

		/// <summary>
		/// This Get Action Method is Used to Invoke Gender List Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult GetAllGenderList()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "GenderController.GetAllGenderList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			return View();
		}

		/// <summary>
		/// This Method Is Used To Get Gender List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetGenderList()
		{
			string draw = string.Empty;
			string length = string.Empty;
			string start = string.Empty;
			int recordsTotal = 0;
			List<GenderM> actualdata = null;
			try
			{
				int ID = 0;
				List<GenderM> rList = null;
				var value = HttpContext.Session.GetString("GenderList");
				if (value == null || value == "[]")
				{
					rList = await IGendervc.GetGenderList(ID, "GetAllGenderList");
					CommonHelper.SetJsonSessionObject(HttpContext.Session, "GenderList", rList);
				}
				else
				{
					rList = JsonConvert.DeserializeObject<List<GenderM>>(value);
				}
				if (rList != null)
				{
					List<GenderM> GenderList = rList.ToList();
					GenderList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.Id.ToString()));

					#region Sorting

					var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

					var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
					//Sorting
					if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
					{
						switch (sortColumn)
						{
							case "GenderName":
								switch (sortColumnDirection)
								{
									case "asc":
										GenderList = GenderList.OrderBy(o => o.Gender).ToList();
										break;
									case "desc":
										GenderList = GenderList.OrderByDescending(o => o.Gender).ToList();
										break;
								}
								break;
							case "activeStatus":
								switch (sortColumnDirection)
								{
									case "asc":
										GenderList = GenderList.OrderBy(o => o.ActiveStatus).ToList();
										break;
									case "desc":
										GenderList = GenderList.OrderByDescending(o => o.ActiveStatus).ToList();
										break;

								}
								break;
							case "slNo":
								switch (sortColumnDirection)
								{
									case "asc":
										GenderList = GenderList.OrderBy(o => o.SLNo).ToList();
										break;
									case "desc":
										GenderList = GenderList.OrderByDescending(o => o.SLNo).ToList();
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
						GenderList = GenderList.Where(e => e.Gender.Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
					}
					#endregion


					#region Paging Functionality
					draw = HttpContext.Request.Form["draw"].FirstOrDefault();
					length = Request.Form["length"].FirstOrDefault();
					start = Request.Form["start"].FirstOrDefault();

					int pageSize = length != null ? Convert.ToInt32(length) : 0;
					int skip = start != null ? Convert.ToInt32(start) : 0;
					actualdata = GenderList.ToList();
					recordsTotal = 0;
					recordsTotal = actualdata.Count();

					if (pageSize != -1)
					{
						actualdata = GenderList.Skip(skip).Take(pageSize).ToList();
					}
					else
					{
						actualdata = GenderList.Skip(skip).Take(recordsTotal).ToList();
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "GenderController.GetGenderList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
		}

		/// <summary>
		/// This Get Action Method is Used to Invoke Create Gender Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult CreateGender()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "GenderController.CreateGender()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			return View();
		}

		/// <summary>
		/// This Post Action Method is Used to Save Gender Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateGender(AllViewModel allViewModel)
		{
			try
			{
				if (ModelState.IsValid)
				{
					allViewModel.GenderMaster.Id = 0;
					string Message = await SetAndSaveUpdateGenderDetails(allViewModel.GenderMaster.Id, allViewModel);
					if (Message != string.Empty)
					{
						if (Message == "The Gender Details Already Exists.")
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "GenderController.CreateGender()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			ModelState.Clear();
            return RedirectToAction("CreateGender");
        }

		/// <summary>
		/// This Get Action Method is Used to Invoke Edit Gender Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> EditGender(string id, AllViewModel allViewModel)
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
					var GenderDetails = await IGendervc.GetGenderDetails(DecryptedID);
					allViewModel.GenderMaster = GenderDetails;
					GenderDetails.EncryptedId = protector.Protect(id.ToString());
					allViewModel.GenderMaster = GenderDetails;
					ViewBag.IsVisible = true;
					ViewBag.GenderID = Convert.ToInt32(DecryptedID);
					if (allViewModel != null)
					{
						return View(allViewModel);
					}
				}
				else
				{
					return RedirectToAction("GetAllGenderList");
				}
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "GenderController.EditGender()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			return View();
		}

		/// <summary>
		/// This Post Action Method is Used to Update Gender Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditGender(AllViewModel allViewModel)
		{
			if (allViewModel.GenderMaster.Id == 0)
			{
				return PartialView("_Error500");
			}
			if (ModelState.IsValid)
			{
				try
				{
					string Message = await SetAndSaveUpdateGenderDetails(allViewModel.GenderMaster.Id, allViewModel);
					if (Message != string.Empty)
					{
                        if (Message == "The Gender Details Already Exists.")
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
					Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "GenderController.EditGender()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
			}
			else
			{
				return View(allViewModel);
			}
			ModelState.Clear();
			return RedirectToAction("CreateGender");
		}

		#region [Custom Methods]

		/// <summary>
		/// This Method Is Used To Save Or Update Gender Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateGenderDetails(int ID, AllViewModel allViewModel)
		{
			string Message = string.Empty;
			try
			{
				allViewModel.GenderMaster.Id = ID;
				allViewModel.GenderMaster.IsDeleted = (allViewModel.GenderMaster.IsDeleted == true) ? true : false;
				var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
				allViewModel.GenderMaster.CreatedBy = SessionDetails.Id;
				allViewModel.GenderMaster.UpdatedBy = SessionDetails.Id;
				allViewModel.GenderMaster.Status = "ADD";
				Message = await IGendervc.SaveUpdateGenderDetails(allViewModel.GenderMaster);
				List<GenderM> GenderList = new List<GenderM>();
				CommonHelper.SetJsonSessionObject(HttpContext.Session, "GenderList", GenderList);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "GenderController.SetAndSaveUpdateGenderDetails()", ex.Message, false);
				throw;
			}
			return Message;
		}

		#endregion [Custom Methods]
	}
}
