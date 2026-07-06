using System.Text.RegularExpressions;

namespace MixFlowWebApp.Models
{
    public class Session
    {
        public int SessionId { get; set; }
        public int OrganizerId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int NumberOfCourts { get; set; } = 4;
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Organizer Organizer { get; set; } = null!;
        public ICollection<SessionPlayer> SessionPlayers { get; set; } = new List<SessionPlayer>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
