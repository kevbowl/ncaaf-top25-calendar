using NcaafTop25Calendar.Models;
using NcaafTop25Calendar.Services;

try
{
    using var espn = new EspnClient();
    var initial = await espn.GetInitialScoreboardAsync();
    int seasonYear = EspnClient.TryGetSeasonYear(initial);
    int currentWeek = EspnClient.TryGetCurrentWeek(initial);
    var weeks = EspnClient.GetNextWeeks(initial, DateTimeOffset.UtcNow, 4, currentWeek);

    var games = new List<Game>();
    foreach (var wk in weeks)
    {
        var weekDoc = await espn.GetWeekScoreboardAsync(seasonYear, wk);
        games.AddRange(GameMapper.MapTop25Upcoming(weekDoc, DateTimeOffset.UtcNow));
    }

    string output = Path.Combine(Directory.GetCurrentDirectory(), "top25-ncaaf-next4weeks.ics");
    IcsWriter.Write(output, games);
    Console.WriteLine($"Generated {output} with {games.Count} events.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
