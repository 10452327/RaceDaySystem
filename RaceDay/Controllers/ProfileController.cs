using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Services;

namespace RaceDay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureBlobService _blobService;

        public ProfileController(AppDbContext context, AzureBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "Not authenticated." });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.ProfilePictureUrl
            });
        }

        // PUT: api/profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "Not authenticated." });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != user.Id);
            if (emailTaken)
            {
                return BadRequest(new { message = "Email is already in use by another account." });
            }

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            HttpContext.Session.SetString("UserEmail", user.Email);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully.", user.FullName, user.Email, user.ProfilePictureUrl });
        }

        // POST: api/profile/picture
        [HttpPost("picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "Not authenticated." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Please select a valid image file." });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var imageUrl = await _blobService.UploadAsync(file, "profile-pictures");
            user.ProfilePictureUrl = imageUrl;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile picture updated.", profilePictureUrl = imageUrl });
        }
    }
}