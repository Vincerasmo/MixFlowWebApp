using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.DTOs.MatchDTOs;

namespace MixFlowWebApp.DTOs.PublicDTOs
{
    // Everything the public /report/:sessionId page needs in one call — session info,
    // totals, full final standings (same data GetSessionLeaderboardAsync already
    // produces), the #1-ranked player as MVP, and the largest-margin completed match
    // as a highlight.
    public class SessionReportDto
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int NumberOfCourts { get; set; }
        public string Status { get; set; } = string.Empty;

        public int TotalMatchesPlayed { get; set; }
        public int TotalDistinctPlayers { get; set; }

        // Reuses the exact same ranked list GetSessionLeaderboardAsync already
        // computes — no separate standings logic to keep in sync.
        public List<LeaderboardPlayerDto> FinalStandings { get; set; } = new();

        // The Rank == 1 entry from FinalStandings, pulled out separately so the report
        // page can style it as a callout without re-deriving "rank 1 means index 0".
        // Null if no matches were played.
        public LeaderboardPlayerDto? Mvp { get; set; }

        // The completed match with the largest |Team1Score - Team2Score|. Null if no
        // matches were played. Reuses MatchDto so the report page can render it with
        // the same team/score shape used everywhere else in the app.
        public MatchDto? BiggestWin { get; set; }
        public int BiggestWinMargin { get; set; }
    }
}