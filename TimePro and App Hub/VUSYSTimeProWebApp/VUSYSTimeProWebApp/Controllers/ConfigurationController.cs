using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebApp.Contracts;
using Newtonsoft.Json;
using VUSYSTimeProWebApp.Security;
using CommonLib;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class ConfigurationController : Controller
    {
		private AppDbContext db = null;
		private readonly IEmailConfiguration IEmailConfigurationSvc = null;
        private readonly IDataProtector protector;
        public ConfigurationController(AppDbContext appDbContext, IEmailConfiguration emailConfiguration, IDataProtectionProvider dataProtectionProvider,
                                DataProtectionPurposeString dataProtectionPurposeString)
        {
			db = appDbContext;
			this.IEmailConfigurationSvc = emailConfiguration;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
        }

		/// <summary>
		/// This Get Action Method is Used to Invoke Email Configuration List Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult GetAllEmailConfigurationList()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.GetAllEmailConfigurationList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			return View();
		}

		/// <summary>
		/// This Method Is Used To Get Email Configuration List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetEmailConfigurationList()
		{
			string draw = string.Empty;
			string length = string.Empty;
			string start = string.Empty;
			int recordsTotal = 0;
			List<ConfigureEmailM> actualdata = null;
			try
			{
				int ID = 0;

				List<ConfigureEmailM> rList = null;
				var value = HttpContext.Session.GetString("EmailConfigurationList");
				if (value == null || value == "[]")
				{
					rList = await IEmailConfigurationSvc.GetEmailConfigurationList(ID, "GetAllEmailConfigurationList");
					CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmailConfigurationList", rList);
				}
				else
				{
					rList = JsonConvert.DeserializeObject<List<ConfigureEmailM>>(value);
				}
				if (rList != null)
				{
					List<ConfigureEmailM> EmailConfigurationList = rList.ToList();
					EmailConfigurationList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.Id.ToString()));

					#region Sorting
					var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

					var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
					//Sorting
					if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
					{
						switch (sortColumn)
						{
							case "fromEmail":
								switch (sortColumnDirection)
								{
									case "asc":
										EmailConfigurationList = EmailConfigurationList.OrderBy(o => o.FromEmail).ToList();
										break;
									case "desc":
										EmailConfigurationList = EmailConfigurationList.OrderByDescending(o => o.FromEmail).ToList();
										break;
								}
								break;
							case "password":
								switch (sortColumnDirection)
								{
									case "asc":
										EmailConfigurationList = EmailConfigurationList.OrderBy(o => o.Password).ToList();
										break;
									case "desc":
										EmailConfigurationList = EmailConfigurationList.OrderByDescending(o => o.Password).ToList();
										break;
								}
								break;
							case "smtpHost":
								switch (sortColumnDirection)
								{
									case "asc":
										EmailConfigurationList = EmailConfigurationList.OrderBy(o => o.SmtpHost).ToList();
										break;
									case "desc":
										EmailConfigurationList = EmailConfigurationList.OrderByDescending(o => o.SmtpHost).ToList();
										break;
								}
								break;
							case "portNo":
								switch (sortColumnDirection)
								{
									case "asc":
										EmailConfigurationList = EmailConfigurationList.OrderBy(o => o.PortNo).ToList();
										break;
									case "desc":
										EmailConfigurationList = EmailConfigurationList.OrderByDescending(o => o.PortNo).ToList();
										break;
								}
								break;
							case "activeStatus":
								switch (sortColumnDirection)
								{
									case "asc":
										EmailConfigurationList = EmailConfigurationList.OrderBy(o => o.ActiveStatus).ToList();
										break;
									case "desc":
										EmailConfigurationList = EmailConfigurationList.OrderByDescending(o => o.ActiveStatus).ToList();
										break;

								}

								break;
							case "slNo":
								switch (sortColumnDirection)
								{
									case "asc":
										EmailConfigurationList = EmailConfigurationList.OrderBy(o => o.SLNo).ToList();
										break;
									case "desc":
										EmailConfigurationList = EmailConfigurationList.OrderByDescending(o => o.SLNo).ToList();
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
						EmailConfigurationList = EmailConfigurationList.Where(e => e.FromEmail.Contains(searchValue) || e.Password.Contains(searchValue) || e.SmtpHost.Contains(searchValue) || e.PortNo.ToString().Contains(searchValue) || e.ActiveStatus.Contains(searchValue)).ToList();
					}
					#endregion


					#region Paging Functionality
					draw = HttpContext.Request.Form["draw"].FirstOrDefault();
					length = Request.Form["length"].FirstOrDefault();
					start = Request.Form["start"].FirstOrDefault();

					int pageSize = length != null ? Convert.ToInt32(length) : 0;
					int skip = start != null ? Convert.ToInt32(start) : 0;
					actualdata = EmailConfigurationList.ToList();
					recordsTotal = 0;
					recordsTotal = actualdata.Count();

					if (pageSize != -1)
					{
						actualdata = EmailConfigurationList.Skip(skip).Take(pageSize).ToList();
					}
					else
					{
						actualdata = EmailConfigurationList.Skip(skip).Take(recordsTotal).ToList();
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.GetEmailConfigurationList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
		}

		/// <summary>
		/// This Get Action Method is Used to Invoke Create Email Configuration Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult CreateEmailConfiguration()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.CreateEmailConfiguration()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			return View();
		}

		/// <summary>
		/// This Post Action Method is Used to Save Email Configuration Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateEmailConfiguration(AllViewModel allViewModel)
		{
			try
			{
				if (ModelState.IsValid)
				{
					int Count = GetEmailConfigurationCount();
					if (Count == 0)
					{
						allViewModel.EmailConfigurationMaster.Id = 0;
						string Message = await SetAndSaveUpdateEmailConfigurationDetails(allViewModel.EmailConfigurationMaster.Id, allViewModel);
						if (Message != string.Empty)
						{
							if (Message != string.Empty)
							{
								if (Message == "The Email Configuration Details Already Exists.")
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
					else
					{
						TempData[Helper.TEMP_DATA_WARNING_MESSAGE_KEY] = "Only One Active Configuration Should Be There. Please Obsolete The Active Configuration and then Try Again. ";
						return View(allViewModel);
					}
				}
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.CreateEmailConfiguration()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			ModelState.Clear();
            return RedirectToAction("CreateEmailConfiguration");
        }

		/// <summary>
		/// This Get Action Method is Used to Invoke Edit Email Configuration Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> EditEmailConfiguration(string id, AllViewModel allViewModel)
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
					var EmailConfigurationDetails = await IEmailConfigurationSvc.GetEmailConfigurationDetails(DecryptedID);
					string strDecryptedPassword = AESEncrytDecry.DecryptAES256(EmailConfigurationDetails.Password, Helper.EncryptDecryptKey);
					ViewBag.EncryptedPassword = EmailConfigurationDetails.Password;
					CommonHelper.SetJsonSessionObject(HttpContext.Session, "CurrentEncryptedPassword", EmailConfigurationDetails.Password);
					allViewModel.EmailConfigurationMaster = EmailConfigurationDetails;
					EmailConfigurationDetails.EncryptedId = protector.Protect(id.ToString());
					allViewModel.EmailConfigurationMaster = EmailConfigurationDetails;
					ViewBag.IsVisible = true;
					if (allViewModel != null)
					{
						return View(allViewModel);
					}
				}
				else
				{
					return RedirectToAction("GetAllEmailConfigurationList");
				}
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.EditEmailConfiguration()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
			}
			return View();
		}

		/// <summary>
		/// This Post Action Method is Used to Update Email Configuration Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditEmailConfiguration(AllViewModel allViewModel)
		{
			if (allViewModel.EmailConfigurationMaster.Id == 0)
			{
				return PartialView("_Error500");
			}
			if (ModelState.IsValid)
			{
				try
				{
					string Message = await SetAndSaveUpdateEmailConfigurationDetails(allViewModel.EmailConfigurationMaster.Id, allViewModel);
					if (Message != string.Empty)
					{
						if (Message != string.Empty)
						{
							if (Message == "The Email Configuration Details Already Exists.")
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
					Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.EditEmailConfiguration()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
				}
			}
			ModelState.Clear();
			return RedirectToAction("CreateEmailConfiguration");
		}

		#region [Custom Methods]

		/// <summary>
		/// This Method Is Used To Get Email Configuration Count.
		/// </summary>
		/// <returns></returns>
		public int GetEmailConfigurationCount()
		{
			int TotalCount = 0;
			try
			{
				TotalCount = (from c in db.ConfigureEmailList
							  where c.IsDeleted == false
							  select new { c.Id }).Count();
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.GetEmailConfigurationCount()", ex.Message, false);
				throw;
			}
			return TotalCount;
		}

		/// <summary>
		/// This Method Is Used To Save Or Update Email Configuration Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateEmailConfigurationDetails(int ID, AllViewModel allViewModel)
		{
			string Message = string.Empty;
			try
			{
				allViewModel.EmailConfigurationMaster.Id = ID;
				var EncryptedPassword = CommonHelper.GetObjectFromJson<string>(HttpContext.Session, "CurrentEncryptedPassword");

				if (EncryptedPassword != null)
				{
					if (EncryptedPassword == allViewModel.EmailConfigurationMaster.Password)
					{
						allViewModel.EmailConfigurationMaster.Password = EncryptedPassword;
					}
					else
					{
						allViewModel.EmailConfigurationMaster.Password = AESEncrytDecry.EncryptAES256(allViewModel.EmailConfigurationMaster.Password, Helper.EncryptDecryptKey);
					}
				}
				else
				{
					allViewModel.EmailConfigurationMaster.Password = AESEncrytDecry.EncryptAES256(allViewModel.EmailConfigurationMaster.Password, Helper.EncryptDecryptKey);
				}
				allViewModel.EmailConfigurationMaster.IsDeleted = (allViewModel.EmailConfigurationMaster.IsDeleted == true) ? true : false;
				var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
				allViewModel.EmailConfigurationMaster.CreatedBy = SessionDetails.Id;
				allViewModel.EmailConfigurationMaster.UpdatedBy = SessionDetails.Id;
				allViewModel.EmailConfigurationMaster.Status = "ADD";
				Message = await IEmailConfigurationSvc.SaveAndUpdateEmailConfigurationDetails(allViewModel.EmailConfigurationMaster);
				List<ConfigureEmailM> configureEmailList = new List<ConfigureEmailM>();
				CommonHelper.SetJsonSessionObject(HttpContext.Session, "EmailConfigurationList", configureEmailList);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ConfigurationController.SetAndSaveUpdateEmailConfigurationDetails()", ex.Message, false);
				throw;
			}
			return Message;
		}
		#endregion
	}
}
