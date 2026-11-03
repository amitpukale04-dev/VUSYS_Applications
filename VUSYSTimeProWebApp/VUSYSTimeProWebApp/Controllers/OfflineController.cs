using Microsoft.AspNetCore.Mvc;

namespace VUSYSTimeProWebApp.Controllers
{
    public class OfflineController : Controller
    {
        public IActionResult Offline()
        {
            return View();
        }
    }
}
