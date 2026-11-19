using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClientService _api;

        public HomeController(ApiClientService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.Equals(role, "Organiser", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Organiser");
            }
            if (string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Participant");
            }

            var upcoming = await _api.GetEventsAsync();
            return View(upcoming);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}