using System;

namespace NcaafTop25Calendar.Models
{
    public sealed class Game
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset StartUtc { get; set; }
        public DateTimeOffset EndUtc { get; set; }

        public string HomeTeam { get; set; } = string.Empty;
        public int? HomeRank { get; set; }
        public int? HomeScore { get; set; }

        public string AwayTeam { get; set; } = string.Empty;
        public int? AwayRank { get; set; }
        public int? AwayScore { get; set; }

        public string? Location { get; set; }
        public string? TvProvider { get; set; }
        public Uri? Url { get; set; }

        // Game status properties
        public GameStatus Status { get; set; } = GameStatus.Upcoming;
        public string? Quarter { get; set; }
        public string? TimeRemaining { get; set; }

        public string BuildTitle()
        {
            string home = (HomeRank.HasValue && HomeRank.Value >= 1 && HomeRank.Value <= 25)
                ? $"#{HomeRank.Value} {HomeTeam}"
                : HomeTeam;
            string away = (AwayRank.HasValue && AwayRank.Value >= 1 && AwayRank.Value <= 25)
                ? $"#{AwayRank.Value} {AwayTeam}"
                : AwayTeam;
            return $"🏈 {away} at {home}";
        }

        public string BuildGameStatusDescription()
        {
            // Only show scores for games that are actually in progress or completed
            if (HomeScore.HasValue && AwayScore.HasValue)
            {
                if (Status == GameStatus.Final)
                {
                    return $"Final: {AwayScore}-{HomeScore}";
                }
                else if (Status == GameStatus.Live && !string.IsNullOrEmpty(Quarter) && !string.IsNullOrEmpty(TimeRemaining))
                {
                    return $"{Quarter} {TimeRemaining}: {AwayScore}-{HomeScore}";
                }
                else if (Status == GameStatus.Live)
                {
                    return $"Live: {AwayScore}-{HomeScore}";
                }
                // Don't show scores for upcoming games
            }
            
            // If no scores but we have status info, show that
            if (Status == GameStatus.Live && !string.IsNullOrEmpty(Quarter) && !string.IsNullOrEmpty(TimeRemaining))
            {
                return $"Live: {Quarter} {TimeRemaining}";
            }
            else if (Status == GameStatus.Live)
            {
                return "Live";
            }
            else if (Status == GameStatus.Final)
            {
                return "Final";
            }
            
            return string.Empty;
        }
    }

    public enum GameStatus
    {
        Upcoming,
        Live,
        Final
    }
}


