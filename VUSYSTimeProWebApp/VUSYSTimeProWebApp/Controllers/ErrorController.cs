using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VUSYSTimeProWebApp.Models;
using CommonLib;

namespace VUSYSTimeProWebApp.Controllers
{
	public class ErrorController : Controller
	{
		/// <summary>
		/// This Get Action Method is Used to Invoke Error 404 Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        [Route("Error/Error404")]
        public IActionResult Error404()
		{
			return PartialView("_Error404");
		}

		/// <summary>
		/// This Get Action Method is Used to Invoke Error 500 Screen.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        [Route("Error/Error500")]
        public IActionResult Error500()
		{
			return PartialView("_Error500");
		}

        /// <summary>
        /// This Get Action Method is Used to Invoke Error 302 Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Error/Error302")]
        public IActionResult Error302()
        {
            return PartialView("_Error500");
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke Common Error Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CommonError()
        {
            var serializedData = TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] as string;
            if (serializedData != null)
            {
                var CommonErrorData = JsonConvert.DeserializeObject<CommonError>(serializedData);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = null;
                return PartialView("_CommonError", CommonErrorData);
            }
            // Fallback logic in case of missing data
            //return PartialView("_CommonError", new CommonError());
            return RedirectToAction("Login", "Login");
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke UnExpected Error Screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
		public IActionResult UnExpectedError()
		{
			return PartialView("_UnExpectedError");
		}

		[Route("Error2/{code}")]
		[HttpGet]
		public IActionResult HandleError(int code)
		{
			if (code == 404)
			{
                return RedirectToAction("Error404");
            }
			else if(code == 500)
			{
                return RedirectToAction("Error500");
            }
			else
			{
                return RedirectToAction("UnExpectedError");
            }
		}
	}
}
