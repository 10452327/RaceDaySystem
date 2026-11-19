using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly ApiClientService _api;

        public OrganiserController(ApiClientService api)
        {
            _api = api;
        }

        private IActionResult? CheckOrganiserSession()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
            }

            if (!string.Equals(role, "Organiser", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return null;
        }

        public async Task<IActionResult> Dashboard()
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var allEvents = await _api.GetEventsAsync();
            var myEvents = allEvents.Where(e => e.OrganiserId == userId).OrderBy(e => e.Date).ToList();

            var enrolmentCounts = new Dictionary<int, int>();
            foreach (var ev in myEvents)
            {
                var enrolments = await _api.GetEventEnrolmentsAsync(ev.Id);
                enrolmentCounts[ev.Id] = enrolments.Count;
            }

            ViewBag.EnrolmentCounts = enrolmentCounts;
            return View(myEvents);
        }

        public async Task<IActionResult> Events()
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var allEvents = await _api.GetEventsAsync();
            var myEvents = allEvents.Where(e => e.OrganiserId == userId).OrderBy(e => e.Date).ToList();
            return View(myEvents);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            return View(new EventFormViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(EventFormViewModel model)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (ok, message, eventId) = await _api.CreateEventAsync(model);
            if (!ok || eventId == null)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            if (model.BannerFile != null && model.BannerFile.Length > 0)
            {
                var (bannerOk, bannerMsg) = await _api.UploadEventBannerAsync(eventId.Value, model.BannerFile);
                if (!bannerOk)
                {
                    TempData["WarningMessage"] = $"Event created, but banner upload failed: {bannerMsg}";
                    return RedirectToAction(nameof(Events));
                }
            }

            TempData["SuccessMessage"] = "Event created successfully.";
            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var ev = await _api.GetEventByIdAsync(id);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Events));
            }

            var form = new EventFormViewModel
            {
                Id = ev.Id,
                Name = ev.Name,
                Description = ev.Description,
                Date = ev.Date,
                Location = ev.Location,
                Distance = ev.Distance,
                EventType = ev.EventType,
                BannerImageUrl = ev.BannerImageUrl
            };

            return View(form);
        }

        [HttpPost]
        public async Task<IActionResult> EditEvent(EventFormViewModel model)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            if (!ModelState.IsValid || model.Id == null)
            {
                return View(model);
            }

            var (ok, message) = await _api.UpdateEventAsync(model.Id.Value, model);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            if (model.BannerFile != null && model.BannerFile.Length > 0)
            {
                var (bannerOk, bannerMsg) = await _api.UploadEventBannerAsync(model.Id.Value, model.BannerFile);
                if (!bannerOk)
                {
                    TempData["WarningMessage"] = $"Event updated, but banner upload failed: {bannerMsg}";
                    return RedirectToAction(nameof(Events));
                }
            }

            TempData["SuccessMessage"] = "Event updated successfully.";
            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var (ok, message) = await _api.DeleteEventAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> Categories(int eventId)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var ev = await _api.GetEventByIdAsync(eventId);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Events));
            }

            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(int eventId, string categoryName)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["ErrorMessage"] = "Category name is required.";
                return RedirectToAction(nameof(Categories), new { eventId });
            }

            var (ok, message) = await _api.AddCategoryAsync(eventId, categoryName.Trim());
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        [HttpGet]
        public async Task<IActionResult> Enrolments(int eventId)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var ev = await _api.GetEventByIdAsync(eventId);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Events));
            }

            var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
            ViewBag.Event = ev;
            return View(enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEnrolmentStatus(int enrolmentId, int eventId, string status)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var (ok, message) = await _api.UpdateEnrolmentStatusAsync(enrolmentId, status);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Enrolments), new { eventId });
        }

        [HttpGet]
        public async Task<IActionResult> Results(int eventId)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            var ev = await _api.GetEventByIdAsync(eventId);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Events));
            }

            var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
            ViewBag.Event = ev;
            return View(enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> SaveResult(int enrolmentId, int eventId, string finishTime, int position)
        {
            var guard = CheckOrganiserSession();
            if (guard != null) return guard;

            if (!TimeSpan.TryParse(finishTime, out var parsedTime) || parsedTime <= TimeSpan.Zero)
            {
                TempData["ErrorMessage"] = "Enter a valid finish time (e.g. 00:42:15 or 01:15:30).";
                return RedirectToAction(nameof(Results), new { eventId });
            }

            if (position <= 0)
            {
                TempData["ErrorMessage"] = "Finishing position must be at least 1.";
                return RedirectToAction(nameof(Results), new { eventId });
            }

            var (ok, message) = await _api.RecordResultAsync(enrolmentId, parsedTime, position);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Results), new { eventId });
        }
    }
}