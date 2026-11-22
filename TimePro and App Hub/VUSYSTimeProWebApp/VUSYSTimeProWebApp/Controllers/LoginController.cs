using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebApp.Contracts;
using Newtonsoft.Json;
using CommonLib;

namespace VUSYSTimeProWebApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogin ILoginSvc = null;
        private readonly CookieOptions cookieOptions = null;
        private readonly IHttpContextAccessor httpContextAccessor;
        private AppDbContext db = null;
        private readonly IWebHostEnvironment webHostEnvironment;
        
        /// <summary>
        /// Login Controller Class Constructor
        /// </summary>
        /// <param name="ServiceRepository"></param>
        /// <param name="db"></param>
        /// <param name="cOptions"></param>
        /// <param name="configuration"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="_webHostEnvironment"></param>
        public LoginController(ILogin ServiceRepository, AppDbContext db, CookieOptions cOptions, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment _webHostEnvironment)
        {
            ILoginSvc = ServiceRepository;
            this.db = db;
            this.cookieOptions = cOptions;
            this.httpContextAccessor = httpContextAccessor;
            webHostEnvironment = _webHostEnvironment;
        }
        /// <summary>
        /// This Get Action Method is Used to Invoke Login Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Login()
        {
            try
            {
                ClearSession();
                if (Request.Cookies["RememberUserName"] != null)
                {
                    ViewBag.UserName = Request.Cookies["RememberUserName"];
                }
                if (Request.Cookies["RememberPwd"] != null)
                {
                    ViewBag.Password = Request.Cookies["RememberPwd"];
                }
                ViewBag.SplashScreenCss = "splash-screen splash-screen-body";
                ViewBag.MainDivCss = "hold-transition login-page mainDisplay customLoginChangePasswordBackgroundImage";
			}
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "LoginController.Login()", ex.Message, false);
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Login Into Application.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(AllViewModel allViewModel)
		{
			try
			{
                if (ModelState.IsValid)
                {
                    string Message = await ILoginSvc.GetUserLoginDetails(webHostEnvironment, httpContextAccessor, HttpContext.Session, string.Empty, allViewModel.UserLogin.UserEmailID, allViewModel.UserLogin.Password, "GetAll");

                    if (Message == "Success")
                    {
                        var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");

                        if (SessionDetails != null)
                        {
                            bool IsTwoFactorAuthenticationEnabled = SessionDetails.IsEnableTwoFactorAuthentication;
                            bool IsReset = SessionDetails.IsReset;

                            if (IsReset)
                            {
                                return RedirectToAction("ChangePasswordOnReset", "Login", new {EmailID = SessionDetails.UserEmailID, Reset = AESEncrytDecry.EncryptAES256("0", Helper.EncryptDecryptKey) });
                            }
                            else
                            {
                                if (allViewModel.UserLogin.RememberMe == true)
                                {
                                    cookieOptions.Expires = DateTime.Now.AddDays(30);
									Response.Cookies.Append("RememberUserName", allViewModel.UserLogin.UserEmailID, cookieOptions);
									Response.Cookies.Append("RememberPwd", allViewModel.UserLogin.Password, cookieOptions);
                                }
                                else
                                {
                                    cookieOptions.Expires = DateTime.Now.AddDays(-1);
                                    Response.Cookies.Append("RememberUserName", allViewModel.UserLogin.UserEmailID, cookieOptions);
                                    Response.Cookies.Append("RememberPwd", allViewModel.UserLogin.Password, cookieOptions);
                                }
                                if (IsTwoFactorAuthenticationEnabled)
                                {
                                    return RedirectToAction("OTPAuthentication", "Login");
                                }
                                return RedirectToAction("Home", "Home");
                            }
                        }
                    }
                    else
                    {
                        ViewBag.LoginError = Message;
                        ViewBag.SplashScreenCss = "splash-screen splash-screen-body splashScreenDisplay";
                        ViewBag.MainDivCss = "hold-transition login-page customLoginChangePasswordBackgroundImage";
                        return View();
                    }
                }
            }
			catch (Exception ex)
			{
                ViewBag.LoginError = "Oops An Error Occurred...";
                ViewBag.SplashScreenCss = "splash-screen splash-screen-body splashScreenDisplay";
                ViewBag.MainDivCss = "hold-transition login-page customLoginChangePasswordBackgroundImage";
                return View();
			}
			return View(allViewModel);
		}

        /// <summary>
        /// This Method Is Used To Clear The Session
        /// </summary>
        private async void ClearSession()
        {
            try
            {
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                if (SessionDetails != null)
                {
					bool Result = await ILoginSvc.UpdateUserLoginOrLogoutDate(Convert.ToInt32(SessionDetails.Id), "UpdateUserLogout");
					CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "UserSession");
					CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "MainMenuMasterList");
					CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "SubMenuMasterList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "EmailConfigurationList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "CountryList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "EmployeeList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "EmployeeTypeList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "GenderList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "ProjectList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "ProjectActivityList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "EmployeeProjectMappingList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "RoleList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "RoleMenuSession");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "StateList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "TimesheetApproveRejectList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "TimesheetList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "TempTimesheetList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "ProjectAllocatedConsumedReportList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "TimesheetReportList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "TimesheetSubmissionList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "UserList");
                    CommonHelper.ClearAndRemoveSessionObject(HttpContext.Session, "LeaveReportList");
                }
			}
            catch (Exception ex)
            {
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "LoginController.ClearSession()", ex.Message, false);
			}
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Logout Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
		public IActionResult Logout()
		{
			try
			{
				ClearSession();
				return RedirectToAction("Login", "Login");
			}
			catch (Exception ex)
			{
				Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "LoginController.Logout()", ex.Message, false);
				return RedirectToAction("Login", "Login");
			}
		}

        /// <summary>
        /// This Method Is Used To Invoke OTP Authentication Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> OTPAuthentication()
        {
            try
            {
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                if (SessionDetails != null)
                {
                    ViewBag.ResendTimeoutSeconds = CommonHelper.KeyOption.ResendOTPInMinutes * 60;
                    string Message = await ILoginSvc.OTPAuthentication(HttpContext.Session, db, webHostEnvironment, SessionDetails);
                    if (Message != Helper.EmailFailed && Message != Helper.ErrorOccurred)
                    {
                        ViewBag.OTPAuthenticationSuccess = Message;
                        ViewBag.OTPAuthenticationError = null;
                    }
                    else
                    {
                        ViewBag.OTPAuthenticationError = Message;
                        ViewBag.OTPAuthenticationSuccess = null;
                    }
                }
                else
                {
                    return RedirectToAction("Login", "Login");
                }
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "LoginController.OTPAuthentication()", ex.Message, false);
                return RedirectToAction("Login", "Login");
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Verify The OTP.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OTPAuthentication(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    ViewBag.ResendTimeoutSeconds = CommonHelper.KeyOption.ResendOTPInMinutes * 60;
                    var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                    if(SessionDetails != null)
                    {
                        var GeneratedOTPSession = HttpContext.Session.GetString($"OTP_User_" + SessionDetails.Id);
                        var OTPExpiryTimeSession = HttpContext.Session.GetString($"OTP_User_" + SessionDetails.Id + "_ExpiryTime");
                        if (GeneratedOTPSession != null && OTPExpiryTimeSession != null)
                        {
                            var GeneratedOTP = JsonConvert.DeserializeObject<string>(GeneratedOTPSession);
                            var OTPExpiryTime = JsonConvert.DeserializeObject<DateTime>(OTPExpiryTimeSession);
                            if (OTPExpiryTime >= DateTime.Now)
                            {
                                if(GeneratedOTP == allViewModel.otpAuth.OTPDigits)
                                {
                                    CommonHelper.RemoveSessionObject(HttpContext.Session, $"OTP_User_" + SessionDetails.Id);
                                    CommonHelper.RemoveSessionObject(HttpContext.Session, $"OTP_User_" + SessionDetails.Id + "_ExpiryTime");
                                    return RedirectToAction("Home", "Home");
                                }
                                else
                                {
                                    ViewBag.OTPAuthenticationError = "Invalid OTP. Please Enter Valid OTP !!!";
                                    ViewBag.OTPAuthenticationSuccess = null;
                                }
                            }
                            else
                            {
                                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("ExpiredLink.png", "Login", "Login", "Click Here To Login", "OTP Has Been Expired !!!", false));
                                return RedirectToAction("CommonError", "Error");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "LoginController.OTPAuthentication()", ex.Message, false);
                return RedirectToAction("Login", "Login");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Post Action Method is Used to Resends The OTP.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOTP()
        {
            try
            {
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                if (SessionDetails != null)
                {
                    string Message = await ILoginSvc.OTPAuthentication(HttpContext.Session, db, webHostEnvironment, SessionDetails);
                    if (Message != Helper.EmailFailed && Message != Helper.ErrorOccurred)
                    {
                        return Json(new { Success = true, Message });
                    }
                    else
                    {
                        return Json(new { Success = false, Message });
                    }
                }
                return Json(new { Success = false, Message = "Session Expired." });
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "LoginController.ResendOTP()", ex.Message, false);
                return RedirectToAction("Login", "Login");
            }
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Forgot Password Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Send Request To Reset The Password.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string BaseURL = CommonHelper.GetBaseURL(httpContextAccessor);
                    allViewModel.forgotPassword.LinkURL = BaseURL + "/Login/ChangePasswordOnReset?Reset=" + AESEncrytDecry.EncryptAES256("1", Helper.EncryptDecryptKey) + "&EmailID=" + AESEncrytDecry.EncryptAES256(allViewModel.forgotPassword.UserEmailID, Helper.EncryptDecryptKey);
                    string Message = await ILoginSvc.ResetPassword(db, webHostEnvironment, allViewModel.forgotPassword);
                    if (Message == "Success")
                    {
                        return RedirectToAction("Login", "Login");
                    }
                    else
                    {
                        ViewBag.ForgotPasswordError = Message;
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "LoginController.ForgotPassword()", ex.Message, false);
                return RedirectToAction("Login", "Login");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Change Password Screen After Reset.
        /// </summary>
        /// <param name="Reset"></param>
        /// <param name="EmailID"></param>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ChangePasswordOnReset(string Reset, string EmailID, AllViewModel allViewModel)
        {
            if (Reset != null && EmailID != null)
            {
                string DecryptedReset = AESEncrytDecry.DecryptAES256(Reset, Helper.EncryptDecryptKey);
                if (EmailID != null && DecryptedReset == "1")
                {
                    string DecryptedEmailID = AESEncrytDecry.DecryptAES256(EmailID, Helper.EncryptDecryptKey);
                    var UserDetails = db.UserList.Where(u => u.EmailID == DecryptedEmailID).Select(u => new UsersM
                    {
                        UserEmailID = u.EmailID,
                        IsPasswordExpiryLinkAvailable = u.IsPasswordExpiryLinkAvailable,
                        PasswordExpiryLinkDateTime = u.PasswordExpiryLinkDateTime
                    }).ToList();

                    if (UserDetails != null && UserDetails.Count > 0)
                    {
                        bool IsPasswordLinkExpired = UserDetails[0].IsPasswordExpiryLinkAvailable;
                        DateTime PasswordLinkExpiredDateTime = UserDetails[0].PasswordExpiryLinkDateTime;
                        if (IsPasswordLinkExpired && (PasswordLinkExpiredDateTime >= DateTime.Now))
                        {
                            ChangePasswordOnReset _objChangePasswordOnReset = new ChangePasswordOnReset();
                            _objChangePasswordOnReset.EmailID = DecryptedEmailID;
                            allViewModel.changePasswordOnReset = _objChangePasswordOnReset;
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("ExpiredLink.png", "Login", "Login", "Click Here To Login", "Password Reset Link Has Been Expired !!!", false));
                            return RedirectToAction("CommonError", "Error");
                        }
                    }
                }
                else if (EmailID != null && DecryptedReset == "0")
                {
                    ChangePasswordOnReset _objChangePasswordOnReset = new ChangePasswordOnReset();
                    _objChangePasswordOnReset.EmailID = EmailID;
                    allViewModel.changePasswordOnReset = _objChangePasswordOnReset;
                }
                else
                {
                    return RedirectToAction("Login", "Login");
                }
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
            return View(allViewModel);
        }

        /// <summary>
        /// This Post Action Method is Used to Set New Password.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordOnReset(AllViewModel allViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string Message = await ILoginSvc.ChangePasswordOnReset(allViewModel.changePasswordOnReset.EmailID, allViewModel.changePasswordOnReset.NewPassword);
                    return RedirectToAction("Login", "Login");
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "LoginController.ChangePasswordOnReset()", ex.Message, false);
                return RedirectToAction("Login", "Login");
            }
            return View(allViewModel);
        }
    }
}
