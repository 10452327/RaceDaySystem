using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResultsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/results (Owning Organiser records/publishes finish time & position)
        [HttpPost]
        public async Task<IActionResult> RecordResult([FromBody] ResultCreateDto dto)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Organiser" || userId == null)
            {
                return Unauthorized(new { message = "Only Organisers can publish race results." });
            }

            if (dto.Position <= 0)
            {
                return BadRequest(new { message = "Finishing position must be greater than zero." });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .Include(e => e.Result)
                .FirstOrDefaultAsync(e => e.Id == dto.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.Event?.OrganiserId != userId.Value)
            {
                return Forbid();
            }

            if (enrolment.Result != null)
            {
                enrolment.Result.FinishTime = dto.FinishTime;
                enrolment.Result.Position = dto.Position;
            }
            else
            {
                var result = new Result
                {
                    EnrolmentId = enrolment.Id,
                    FinishTime = dto.FinishTime,
                    Position = dto.Position
                };
                _context.Results.Add(result);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Result saved successfully." });
        }

        // GET: api/results/my (Participant views personal race history & finishing rank)
        [HttpGet("my")]
        public async Task<IActionResult> GetMyResults()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Participant" || userId == null)
            {
                return Unauthorized(new { message = "Only Participants can view personal race results." });
            }

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Event)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                .Where(r => r.Enrolment != null && r.Enrolment.ParticipantId == userId.Value)
                .Select(r => new
                {
                    r.Id,
                    EventId = r.Enrolment!.EventId,
                    EventName = r.Enrolment.Event != null ? r.Enrolment.Event.Name : string.Empty,
                    EventDate = r.Enrolment.Event != null ? r.Enrolment.Event.Date : DateTime.MinValue,
                    CategoryName = r.Enrolment.Category != null ? r.Enrolment.Category.Name : string.Empty,
                    r.FinishTime,
                    r.Position,
                    TotalFinishers = _context.Results.Count(x => x.Enrolment != null && x.Enrolment.EventId == r.Enrolment.EventId)
                })
                .OrderByDescending(r => r.EventDate)
                .ToListAsync();

            return Ok(results);
        }
    }
}