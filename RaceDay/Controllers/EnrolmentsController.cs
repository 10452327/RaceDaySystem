using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

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

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.EventId == dto.EventId);
            if (category == null)
            {
                return BadRequest(new { message = "Invalid category or event combination." });
            }

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
                CategoryId = dto.CategoryId,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrolment successful!", enrolmentId = enrolment.Id, status = enrolment.Status });
        }

        // GET: api/enrolments/my (Participant views their own enrolments)
        [HttpGet("my")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Participant" || userId == null)
            {
                return Unauthorized(new { message = "Only logged-in Participants can view their enrolments." });
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == userId.Value)
                .Include(e => e.Event)
                .Include(e => e.Category)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new
                {
                    e.Id,
                    e.EventId,
                    EventName = e.Event != null ? e.Event.Name : string.Empty,
                    EventDate = e.Event != null ? e.Event.Date : DateTime.MinValue,
                    Location = e.Event != null ? e.Event.Location : string.Empty,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category != null ? e.Category.Name : string.Empty,
                    e.Status,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET: api/enrolments/event/{eventId} (Owning Organiser views event participants)
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only Organisers can view event enrolments." });
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

            var enrolments = await _context.Enrolments
                .Where(e => e.EventId == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .Include(e => e.Result)
                .Select(e => new
                {
                    e.Id,
                    e.ParticipantId,
                    ParticipantName = e.Participant != null ? e.Participant.FullName : string.Empty,
                    ParticipantEmail = e.Participant != null ? e.Participant.Email : string.Empty,
                    e.CategoryId,
                    CategoryName = e.Category != null ? e.Category.Name : string.Empty,
                    e.Status,
                    e.EnrolledAt,
                    HasResult = e.Result != null,
                    FinishTime = e.Result != null ? e.Result.FinishTime : (TimeSpan?)null,
                    Position = e.Result != null ? e.Result.Position : (int?)null
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // PATCH: api/enrolments/{id}/status (Owning Organiser updates enrolment status)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only Organisers can update enrolment status." });
            }

            if (status != "Confirmed" && status != "Pending" && status != "Cancelled")
            {
                return BadRequest(new { message = "Invalid status." });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.Event?.OrganiserId != userId.Value)
            {
                return Forbid();
            }

            enrolment.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated.", id = enrolment.Id, status = enrolment.Status });
        }
    }
}