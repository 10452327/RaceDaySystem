using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiClientService _api;

        public AccountController(ApiClientService api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (ok, message) = await _api.RegisterAsync(model);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (ok, message, data) = await _api.LoginAsync(model);
            if (!ok || data == null)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            HttpContext.Session.SetString("UserRole", data.Role);
            HttpContext.Session.SetString("UserEmail", model.Email);
            if (data.UserId.HasValue)
            {
                HttpContext.Session.SetInt32("UserId", data.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return string.Equals(data.Role, "Organiser", StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Participant");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _api.LogoutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}