using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.ViewModels;
using CommonLib;
using Newtonsoft.Json;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class ChangePasswordController : Controller
    {
        private readonly ILogin ILoginSvc = null;
        /// <summary>
        /// Change Password Controller Class Constructor
        /// </summary>
        /// <param name="ServiceRepository"></param>
        public ChangePasswordController(ILogin ServiceRepository)
        {
            ILoginSvc = ServiceRepository;
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Change Password Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ChangePassword()
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ChangePasswordController.ChangePassword()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Change User Password.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ChangePassword(AllViewModel allViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");

                    if (SessionDetails != null)
                    {
                        allViewModel.changePassword.UserName = SessionDetails.UserName;
                        allViewModel.changePassword.EmailID = SessionDetails.UserEmailID;
                        string Message = await ILoginSvc.ChangePassword(allViewModel.changePassword, "Single");
                        if (Message != "Old Password Is Incorrect.")
                        {
                            TempData[Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY] = Message;
                        }
                        else
                        {
                            TempData[Helper.TEMP_DATA_ERROR_MESSAGE_KEY] = Message;
                            return View(allViewModel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "ChangePasswordController.ChangePassword()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            return View(allViewModel);
        }
    }
}
