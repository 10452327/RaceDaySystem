using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiClientService _api;

        public ParticipantController(ApiClientService api)
        {
            _api = api;
        }

        private IActionResult? CheckParticipantSession()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
            }

            if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return null;
        }

        public async Task<IActionResult> Index(string? search, string? eventType, string? location)
        {
            var guard = CheckParticipantSession();
            if (guard != null) return guard;

            ViewBag.Search = search;
            ViewBag.EventType = eventType;
            ViewBag.Location = location;

            var events = await _api.GetEventsAsync(search, eventType, location);
            return View(events);
        }

        public async Task<IActionResult> EventDetails(int id)
        {
            var guard = CheckParticipantSession();
            if (guard != null) return guard;

            var ev = await _api.GetEventByIdAsync(id);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            var guard = CheckParticipantSession();
            if (guard != null) return guard;

            if (categoryId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a category before enrolling.";
                return RedirectToAction(nameof(EventDetails), new { id = eventId });
            }

            var (ok, message) = await _api.EnrolAsync(eventId, categoryId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = message;
            return ok ? RedirectToAction(nameof(MyEvents)) : RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        public async Task<IActionResult> MyEvents()
        {
            var guard = CheckParticipantSession();
            if (guard != null) return guard;

            var enrolments = await _api.GetMyEnrolmentsAsync();
            return View(enrolments);
        }

        public async Task<IActionResult> MyResults()
        {
            var guard = CheckParticipantSession();
            if (guard != null) return guard;

            var results = await _api.GetMyResultsAsync();
            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var guard = CheckParticipantSession();
            if (guard != null) return guard;

            var profile = await _api.GetProfileAsync();
            if (profile == null)
            {
                TempData["ErrorMessage"] = "Could not load your profile.";
                return RedirectToAction(nameof(Index));
            }

            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var guard = CheckParticipantSession();
            if (guard != null) return guard;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (ok, message) = await _api.UpdateProfileAsync(model.FullName, model.Email);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                var (picOk, picMsg) = await _api.UploadProfilePictureAsync(model.ProfilePictureFile);
                if (!picOk)
                {
                    TempData["WarningMessage"] = $"Profile updated, but picture upload failed: {picMsg}";
                    return RedirectToAction(nameof(Profile));
                }
            }

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }
}