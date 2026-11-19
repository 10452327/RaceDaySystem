using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public class RegisterViewModel
    {
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Participant";
    }

    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class CategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int EventId { get; set; }
    }

    public class EventItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public int OrganiserId { get; set; }
        public List<CategoryItem> Categories { get; set; } = new();
    }

    public class EventFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today.AddDays(14);

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required, Range(0.1, 1000)]
        public decimal Distance { get; set; } = 10;

        [Required, Display(Name = "Event Type")]
        public string EventType { get; set; } = "Run";

        public string? BannerImageUrl { get; set; }
        public IFormFile? BannerFile { get; set; }
    }

    public class EnrolmentItem
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed";
        public DateTime EnrolledAt { get; set; }
        public bool HasResult { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
    }

    public class ResultItem
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
        public int TotalFinishers { get; set; }
    }

    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }
    }

    public class LoginApiResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? UserId { get; set; }
    }
}