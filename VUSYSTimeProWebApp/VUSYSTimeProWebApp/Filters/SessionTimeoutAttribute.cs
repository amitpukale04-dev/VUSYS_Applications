using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Models;

namespace VUSYSTimeProWebApp.Filters
{
    public class SessionTimeoutAttribute:ActionFilterAttribute
    {
        public SessionTimeoutAttribute() { }

        /// <summary>
        /// This Method Is Used To Check Whether User Session Is Active Or Not.
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var UserSessionDetail = filterContext.HttpContext.Session.GetObjectFromJson<UsersM>("UserSession");

            var BaseURLSession = filterContext.HttpContext.Session.GetObjectFromJson<string>("BaseURL");

            if (BaseURLSession == null)
            {
                var strBaseURL = filterContext.HttpContext.Request.Scheme + "://" + filterContext.HttpContext.Request.Host + "/";

                CommonHelper.SetJsonSessionObject(filterContext.HttpContext.Session, "VUSYSTimeProBaseURL", strBaseURL);
            }

            if (UserSessionDetail == null)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { action = "Login", controller = "Login" }));
                return;
            }
            base.OnActionExecuting(filterContext);
        }
    }
}
