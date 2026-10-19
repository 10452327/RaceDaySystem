using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;
using RaceDay.DTOs;

namespace RaceDay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/events (Both roles can view)
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Categories)
                .ToListAsync();
            return Ok(events);
        }

        // POST: api/events (Organiser only)
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] EventDto dto)
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

        // POST: api/events/{eventId}/categories (Organiser adds categories)
        [HttpPost("{eventId}/categories")]
        public async Task<IActionResult> AddCategory(int eventId, [FromBody] CategoryDto dto)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Organiser")
            {
                return Unauthorized(new { message = "Only Organisers can add categories." });
            }

            var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var category = new Category
            {
                Name = dto.Name,
                EventId = eventId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category added successfully!", categoryId = category.Id });
        }
    }
}