using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SimpleCrypto;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.Security;
using VUSYSTimeProWebApp.ViewModels;
using CommonLib;

namespace VUSYSTimeProWebApp.Controllers
{
	public class UsersController : Controller
	{
		private AppDbContext db = null;
		private readonly IUser IUserSvc = null;
		private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICryptoService _cryptoService = new PBKDF2();
        private const int SALT_SIZE = 16;
        private const int HASH_ITERATIONS = 100;
        private readonly IDataProtector protector;

		public UsersController(AppDbContext appDbContext, IUser user, IHttpContextAccessor httpContextAccessor,
			                    IDataProtectionProvider dataProtectionProvider,
								DataProtectionPurposeString dataProtectionPurposeString, IWebHostEnvironment _webHostEnvironment)
		{
			db = appDbContext;
			this.IUserSvc = user;
            this.httpContextAccessor = httpContextAccessor;
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeString.IDRouteValue);
			webHostEnvironment = _webHostEnvironment;
		}

		/// <summary>
		/// This Get Action Method is Used to Invoke User List Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult GetAllUserList()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.GetAllUserList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			return View();
		}

		/// <summary>
		/// This Method Is Used To Get User List and Display In The Grid.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> GetUserList()
		{
			string draw = string.Empty;
			string length = string.Empty;
			string start = string.Empty;
			int recordsTotal = 0;
			List<UsersM> actualdata = null;
			try
			{
				int ID = 0;
				List<UsersM> rList = null;
				var value = HttpContext.Session.GetString("UserList");
				if (value == null || value == "[]")
				{
					rList = await IUserSvc.GetUserList(ID, "GetAllUserList");
					CommonHelper.SetJsonSessionObject(HttpContext.Session, "UserList", rList);
				}
				else
				{
					rList = JsonConvert.DeserializeObject<List<UsersM>>(value);
				}
				if (rList != null)
				{
					List<UsersM> UserList = rList.ToList();
					UserList.ToList().ForEach(u => u.EncryptedId = protector.Protect(u.Id.ToString()));

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
										UserList = UserList.OrderBy(o => o.FullName).ToList();
										break;
									case "desc":
										UserList = UserList.OrderByDescending(o => o.FullName).ToList();
										break;
								}
								break;
							case "userRole":
								switch (sortColumnDirection)
								{
									case "asc":
										UserList = UserList.OrderBy(o => o.UserRole).ToList();
										break;
									case "desc":
										UserList = UserList.OrderByDescending(o => o.UserRole).ToList();
										break;
								}
								break;
							case "userName":
								switch (sortColumnDirection)
								{
									case "asc":
										UserList = UserList.OrderBy(o => o.UserName).ToList();
										break;
									case "desc":
										UserList = UserList.OrderByDescending(o => o.UserName).ToList();
										break;
								}
								break;
							case "userEmailID":
								switch (sortColumnDirection)
								{
									case "asc":
										UserList = UserList.OrderBy(o => o.UserEmailID).ToList();
										break;
									case "desc":
										UserList = UserList.OrderByDescending(o => o.UserEmailID).ToList();
										break;
								}
								break;
							case "mobileNo":
								switch (sortColumnDirection)
								{
									case "asc":
										UserList = UserList.OrderBy(o => o.MobileNo).ToList();
										break;
									case "desc":
										UserList = UserList.OrderByDescending(o => o.MobileNo).ToList();
										break;
								}
								break;
							case "activeStatus":
								switch (sortColumnDirection)
								{
									case "asc":
										UserList = UserList.OrderBy(o => o.ActiveStatus).ToList();
										break;
									case "desc":
										UserList = UserList.OrderByDescending(o => o.ActiveStatus).ToList();
										break;

								}
								break;
							case "slNo":
								switch (sortColumnDirection)
								{
									case "asc":
										UserList = UserList.OrderBy(o => o.SLNo).ToList();
										break;
									case "desc":
										UserList = UserList.OrderByDescending(o => o.SLNo).ToList();
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
						UserList = UserList.Where(u => u.FullName.Contains(searchValue) || u.UserRole.Contains(searchValue)|| u.UserName.Contains(searchValue) || u.UserEmailID.Contains(searchValue) || u.MobileNo.ToString().Contains(searchValue) || u.ActiveStatus.Contains(searchValue)).ToList();
					}

					#endregion


					#region Paging Functionality
					draw = HttpContext.Request.Form["draw"].FirstOrDefault();
					length = Request.Form["length"].FirstOrDefault();
					start = Request.Form["start"].FirstOrDefault();

					int pageSize = length != null ? Convert.ToInt32(length) : 0;
					int skip = start != null ? Convert.ToInt32(start) : 0;
					actualdata = UserList.ToList();
					recordsTotal = 0;
					recordsTotal = actualdata.Count();

					if (pageSize != -1)
					{
						actualdata = UserList.Skip(skip).Take(pageSize).ToList();
					}
					else
					{
						actualdata = UserList.Skip(skip).Take(recordsTotal).ToList();
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.GetUserList()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
		}
		/// <summary>
		/// This Get Action Method is Used to Invoke Create User Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult CreateUser()
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.CreateUser()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			return View();
		}

		/// <summary>
		/// This Post Action Method is Used to Save User Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateUser(AllViewModel allViewModel)
		{
			try
			{
				if (ModelState.IsValid)
				{
                    allViewModel.Users.Id = 0;
                    string Message = await SetAndSaveUpdateUserDetails(allViewModel.Users.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        if (Message == "The User Details Already Exists.")
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
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.CreateUser()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			ModelState.Clear();
            return RedirectToAction("CreateUser");
        }

		/// <summary>
		/// This Get Action Method is Used to Invoke Edit User Screen.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> EditUser(string id, AllViewModel allViewModel)
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
					var UserDetails = await IUserSvc.GetUserDetails(DecryptedID);
					allViewModel.Users = UserDetails;
					UserDetails.EncryptedId = protector.Protect(id.ToString());
					allViewModel.Users = UserDetails;
					ViewBag.IsVisible = true;
					ViewBag.Id = Convert.ToInt32(DecryptedID);
                    SetDropDownDataInViewBag();
                    if (allViewModel != null)
					{
						return View(allViewModel);
					}
				}
				else
				{
					return RedirectToAction("GetAllUserList");
				}
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.EditUser()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
			return View();
		}

		/// <summary>
		/// This Post Action Method is Used to Update User Details.
		/// </summary>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditUser(AllViewModel allViewModel)
		{
			if (allViewModel.Users.Id == 0)
			{
				return PartialView("_Error500");
			}
			if (ModelState.IsValid)
			{
				try
				{
					string Message = await SetAndSaveUpdateUserDetails(allViewModel.Users.Id, allViewModel);
					if (Message != string.Empty)
					{
                        if (Message == "The User Details Already Exists.")
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
					Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.EditUser()", ex.Message, false);
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
			return RedirectToAction("CreateUser");
		}

        #region [Custom Methods]

        /// <summary>
		/// This Method Is Used To Set DropDown Data In View Bag.
		/// </summary>
        public void SetDropDownDataInViewBag()
        {
            try
            {
                ViewBag.Role = DropdownHelper.FillRoleList(db);
                ViewBag.Employee = DropdownHelper.FillEmployeeList(db);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.SetDropDownDataInViewBag()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
		/// This Method Is Used To Get Gender Name.
		/// </summary>
		/// <param name="EmployeeCode"></param>
		/// <returns></returns>
        public string GetGenderName(string EmployeeCode)
        {
            string GenderName = string.Empty;
            try
            {
				var GenderData = (from em in db.EmployeeList
							 join gm in db.GenderMasterList on em.GenderID equals gm.Id
							 where em.EmployeeCode == EmployeeCode && em.IsDeleted == false
							 select new { gm.Gender }).ToList();
				if (GenderData != null && GenderData.Count > 0)
				{
					GenderName = GenderData[0].Gender.ToUpper();
				}
			}
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.GetGenderName()", ex.Message, false);
                throw;
            }
            return GenderName;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="EmployeeCode"></param>
		/// <returns></returns>
		public JsonResult GetEmployeeDetails(string EmployeeCode)
		{
			List<EmployeeUserInfo> _lstEmployeeUserInfo = new List<EmployeeUserInfo>();
            try
			{
				var EmployeeData = (from c in db.EmployeeList
									where c.EmployeeCode == EmployeeCode && c.IsDeleted == false
									select new {c.FullName, c.EmailID, c.MobileNo }).ToList();

				if(EmployeeData != null && EmployeeData.Count > 0)
				{
					foreach(var obj in EmployeeData)
					{
						_lstEmployeeUserInfo.Add(new EmployeeUserInfo { FullName = obj.FullName, MobileNo = obj.MobileNo, UserEmailID = obj.EmailID });
					}
				}
					 
			}
			catch (Exception ex)
			{
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.GetEmployeeDetails()", ex.Message, false);
                throw;
            }
			return Json(_lstEmployeeUserInfo);
		}

		/// <summary>
		/// This Method Is Used To Save Or Update User Details.
		/// </summary>
		/// <param name="ID"></param>
		/// <param name="allViewModel"></param>
		/// <returns></returns>
		public async Task<string> SetAndSaveUpdateUserDetails(int ID, AllViewModel allViewModel)
		{
			string Message = string.Empty;
			try
			{
				allViewModel.Users.Id = ID;
				var Files = Request.Form.Files;
				string ImageFileName = string.Empty;
				byte[] _myData = null;
                if (allViewModel.Users.Id == 0)
                {
                    string BaseURL = CommonHelper.GetBaseURL(httpContextAccessor);
                    string PlainPassword = CommonHelper.GenerateNewPassword(13);
                    allViewModel.Users.Password = _cryptoService.Compute(PlainPassword, SALT_SIZE, HASH_ITERATIONS);
                    allViewModel.Users.PasswordSalt = _cryptoService.Salt;
					allViewModel.Users.PlainPassword = PlainPassword;
                    allViewModel.Users.IsEmailVerified = true;
                    allViewModel.Users.IsReset = true;
					allViewModel.Users.LinkURL = BaseURL;
                }
                if (Files.Count > 0)
				{
					ImageFileName = allViewModel.Users.ProfileUserImage.FileName;
					string ImagePath = CommonHelper.GetActualPath(webHostEnvironment, "UploadedImages");

					if (System.IO.File.Exists(ImagePath))
					{
						System.IO.File.Delete(ImagePath);
					}
					string filePath = Path.Combine(ImagePath, ImageFileName);

					using (var fileStream = new FileStream(filePath, FileMode.Create))
					{
						await allViewModel.Users.ProfileUserImage.CopyToAsync(fileStream);
					}
					string webRootPath = webHostEnvironment.WebRootPath;
					var fullPath = ImagePath + "\\" + ImageFileName;
					_myData = CommonHelper.ConvertToBytes(allViewModel.Users.ProfileUserImage);
					if (System.IO.File.Exists(fullPath))
					{
						System.IO.File.Delete(fullPath);
					}
					allViewModel.Users.UserImageFileName = ImageFileName;
					allViewModel.Users.UserImage = _myData;
				}
				else
				{
                    if (allViewModel.Users.Id == 0)
					{
                        string DefaultUserImageFileName = string.Empty;
                        string strGenderName = GetGenderName(allViewModel.Users.EmployeeCode);
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
                        allViewModel.Users.UserImageFileName = DefaultUserImageFileName;
                        allViewModel.Users.UserImage = _myData;
                    }
                }
                allViewModel.Users.IsEnableTwoFactorAuthentication = (allViewModel.Users.IsEnableTwoFactorAuthentication == true) ? true : false;
                allViewModel.Users.IsDeleted = (allViewModel.Users.IsDeleted == true) ? true : false;
                allViewModel.Users.IsApprover = (allViewModel.Users.IsApprover == true) ? true : false;
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
				allViewModel.Users.CreatedBy = SessionDetails.Id;
				allViewModel.Users.UpdatedBy = SessionDetails.Id;
				allViewModel.Users.Status = "ADD";
				Message = await IUserSvc.SaveUpdateUserDetails(db, webHostEnvironment, allViewModel.Users);
				List<UsersM> userList = new List<UsersM>();
				CommonHelper.SetJsonSessionObject(HttpContext.Session, "UserList", userList);
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UsersController.SetAndSaveUpdateUserDetails()", ex.Message, false);
				throw;
			}
			return Message;
		}

		#endregion [Custom Methods]
	}
}
