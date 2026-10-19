namespace RaceDay.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty; // Run, Walk, or Cycle

        public int OrganiserId { get; set; }
        public User? Organiser { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}