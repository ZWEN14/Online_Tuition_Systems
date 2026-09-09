using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnywhereEdureach.Controllers
{
    public class HomeController : Controller
    {
        // Public landing page.
        public IActionResult Welcome()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
            return View();
        }

        // Logged-in dashboard.
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
