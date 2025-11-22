using System.Diagnostics;
using CMCSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMCSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            // Check if user is actually logged in
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["UserName"] = userName;
            ViewData["UserRole"] = userRole;
            return View();
        }


        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
