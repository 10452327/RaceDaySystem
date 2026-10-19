using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;
using RaceDay.DTOs;

namespace RaceDay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnrolmentsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/enrolments (Participant enters an event)
        [HttpPost]
        public async Task<IActionResult> Enrol([FromBody] EnrolmentDto dto)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Participant" || userId == null)
            {
                return Unauthorized(new { message = "Only logged-in Participants can enrol in events." });
            }

            // Verify event and category exist
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.EventId == dto.EventId);
            if (category == null)
            {
                return BadRequest(new { message = "Invalid category or event combination." });
            }

            // Check duplicate enrolment
            var existing = await _context.Enrolments
                .AnyAsync(e => e.ParticipantId == userId.Value && e.EventId == dto.EventId);
            if (existing)
            {
                return BadRequest(new { message = "You are already enrolled in this event." });
            }

            var enrolment = new Enrolment
            {
                ParticipantId = userId.Value,
                EventId = dto.EventId,
                CategoryId = dto.CategoryId
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrolment successful!", enrolmentId = enrolment.Id });
        }

        // GET: api/enrolments/event/{eventId} (Organiser views event participants)
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Organiser")
            {
                return Unauthorized(new { message = "Only Organisers can view event enrolments." });
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.EventId == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}