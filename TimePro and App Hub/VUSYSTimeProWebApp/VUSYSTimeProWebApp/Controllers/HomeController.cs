using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using CommonLib;
using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class HomeController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HomeController(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Home Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Home()
        {
            try
            {
                ViewBag.BaseURL = CommonHelper.GetBaseURL(_httpContextAccessor);
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                if (SessionDetails != null)
                {
                    var UserRole = SessionDetails.UserRole;
                    ViewBag.DisplayButtons = (UserRole == "Employee" ? true : false);
                    GetGreetingsBasedOnTime(SessionDetails.FullName);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "HomeController.Home()", ex.Message, false);
                return RedirectToAction("Error500", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Method Is Used To Get Greetings Based On Time.
        /// </summary>
        /// <param name="UserName"></param>
        private void GetGreetingsBasedOnTime(string UserName)
        {
            try
            {
                var hour = DateTime.Now.Hour;
                string greeting = hour switch
                {
                    >= 0 and < 12 => "Good Morning",
                    >= 12 and < 18 => "Good Afternoon",
                    _ => "Good Evening"
                };
                TempData["WelcomeGreetings"] = $"Welcome, {UserName} {greeting}";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "HomeController.GetGreetingsBasedOnTime()", ex.Message, false);
            }
        }
    }
}
