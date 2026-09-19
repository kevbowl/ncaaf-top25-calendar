using NcaafTop25Calendar.Models;
using NcaafTop25Calendar.Services;

try
{
    using var espn = new EspnClient();
    var initial = await espn.GetInitialScoreboardAsync();
    int currentWeek = EspnClient.TryGetCurrentWeek(initial);
    var query = EspnClient.BuildWeekQuery(initial, DateTimeOffset.UtcNow, upcomingWeekCount: 3, currentWeek);

    Console.WriteLine(
        $"Season {query.SeasonYear} type {query.SeasonType}; weeks {string.Join(", ", query.Weeks)}" +
        (query.LookaheadKickoff ? " (kickoff lookahead)" : string.Empty));

    var allGames = new List<Game>();
    var h2hGames = new List<Game>();
    var seen = new HashSet<string>();
    var seenH2H = new HashSet<string>();

    foreach (var wk in query.Weeks)
    {
        var weekDoc = await espn.GetWeekScoreboardAsync(query.SeasonYear, query.SeasonType, wk);
        foreach (var game in GameMapper.MapTop25Upcoming(weekDoc, DateTimeOffset.UtcNow))
        {
            if (seen.Add(game.Id)) allGames.Add(game);
        }

        foreach (var game in GameMapper.MapTop25HeadToHead(weekDoc, DateTimeOffset.UtcNow))
        {
            if (seenH2H.Add(game.Id)) h2hGames.Add(game);
        }
    }

    string output = Path.Combine(Directory.GetCurrentDirectory(), "docs", "top25-ncaaf.ics");
    string h2hOutput = Path.Combine(Directory.GetCurrentDirectory(), "docs", "top25-ncaaf-h2h.ics");

    // Only skip writes when the whole slate is empty (offseason / ESPN gap). A real week with
    // Top 25 games but no H2H must still replace last year's H2H file.
    if (allGames.Count == 0)
    {
        SkipEmptyWipe(output);
        SkipEmptyWipe(h2hOutput);
    }
    else
    {
        IcsWriter.Write(output, allGames, "College Football Top 25");
        IcsWriter.Write(h2hOutput, h2hGames, "College Football Top25 H2H");
        Console.WriteLine($"Generated {output} with {allGames.Count} events.");
        Console.WriteLine($"Generated {h2hOutput} with {h2hGames.Count} H2H events.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

static void SkipEmptyWipe(string path)
{
    if (File.Exists(path) && File.ReadAllText(path).Contains("BEGIN:VEVENT", StringComparison.Ordinal))
    {
        Console.WriteLine($"Skipping {path}: ESPN returned 0 events; leaving the existing calendar in place.");
        return;
    }

    Console.WriteLine($"No events for {path} (file is already empty or missing).");
}
