using NcaafTop25Calendar.Models;
using NcaafTop25Calendar.Services;

try
{
    using var espn = new EspnClient();
    var initial = await espn.GetInitialScoreboardAsync();
    int seasonYear = EspnClient.TryGetSeasonYear(initial);
    int currentWeek = EspnClient.TryGetCurrentWeek(initial);
    var weeks = EspnClient.GetNextWeeks(initial, DateTimeOffset.UtcNow, 3, currentWeek);

    var allGames = new List<Game>();
    var h2hGames = new List<Game>();
    
    foreach (var wk in weeks)
    {
        var weekDoc = await espn.GetWeekScoreboardAsync(seasonYear, wk);
        var weekGames = GameMapper.MapTop25Upcoming(weekDoc, DateTimeOffset.UtcNow).ToList();
        allGames.AddRange(weekGames);
        
        // Also collect H2H games (both teams must be Top 25)
        var weekH2H = GameMapper.MapTop25HeadToHead(weekDoc, DateTimeOffset.UtcNow).ToList();
        h2hGames.AddRange(weekH2H);
    }

    // Generate regular Top 25 calendar
    string output = Path.Combine(Directory.GetCurrentDirectory(), "docs", "top25-ncaaf.ics");
    IcsWriter.Write(output, allGames, "College Football Top 25");
    Console.WriteLine($"Generated {output} with {allGames.Count} events.");

    // Generate H2H calendar
    string h2hOutput = Path.Combine(Directory.GetCurrentDirectory(), "docs", "top25-ncaaf-h2h.ics");
    IcsWriter.Write(h2hOutput, h2hGames, "College Football Top25 H2H");
    Console.WriteLine($"Generated {h2hOutput} with {h2hGames.Count} H2H events.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
