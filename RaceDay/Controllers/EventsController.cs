using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;
using RaceDay.Services;

namespace RaceDay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureBlobService _blobService;

        public EventsController(AppDbContext context, AzureBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] string? search, [FromQuery] string? eventType, [FromQuery] string? location)
        {
            var query = _context.Events
                .Include(e => e.Categories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Name.Contains(search) || e.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(e => e.Location.Contains(location));
            }

            var events = await query.OrderBy(e => e.Date).ToListAsync();
            return Ok(events);
        }

        // GET: api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            return Ok(ev);
        }

        // POST: api/events (Organiser only)
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] UpdateEventDto dto)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only authenticated Organisers can create events." });
            }

            var newEvent = new Event
            {
                Name = dto.Name,
                Description = dto.Description,
                Date = dto.Date,
                Location = dto.Location,
                Distance = dto.Distance,
                EventType = dto.EventType,
                OrganiserId = userId.Value
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event created successfully!", eventId = newEvent.Id });
        }

        // PUT: api/events/{id} (Owning Organiser only)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only authenticated Organisers can update events." });
            }

            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != userId.Value)
            {
                return Forbid();
            }

            ev.Name = dto.Name;
            ev.Description = dto.Description;
            ev.Date = dto.Date;
            ev.Location = dto.Location;
            ev.Distance = dto.Distance;
            ev.EventType = dto.EventType;
            if (!string.IsNullOrWhiteSpace(dto.BannerImageUrl))
            {
                ev.BannerImageUrl = dto.BannerImageUrl;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated successfully!", ev });
        }

        // DELETE: api/events/{id} (Owning Organiser only)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only authenticated Organisers can delete events." });
            }

            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != userId.Value)
            {
                return Forbid();
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Event deleted successfully." });
        }

        // POST: api/events/{eventId}/categories (Owning Organiser only)
        [HttpPost("{eventId}/categories")]
        public async Task<IActionResult> AddCategory(int eventId, [FromBody] CategoryDto dto)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only authenticated Organisers can add categories." });
            }

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != userId.Value)
            {
                return Forbid();
            }

            var category = new Category
            {
                Name = dto.Name,
                EventId = eventId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category added successfully!", categoryId = category.Id });
        }

        // POST: api/events/{eventId}/banner (Owning Organiser uploads banner to Azure Blob Storage via API)
        [HttpPost("{eventId}/banner")]
        public async Task<IActionResult> UploadBanner(int eventId, IFormFile file)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only authenticated Organisers can upload event banners." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Please select a valid image file." });
            }

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != userId.Value)
            {
                return Forbid();
            }

            var imageUrl = await _blobService.UploadAsync(file, "event-banners");
            ev.BannerImageUrl = imageUrl;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Banner uploaded successfully.", bannerImageUrl = imageUrl });
        }
    }
}