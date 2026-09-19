using NcaafTop25Calendar.Models;
using NcaafTop25Calendar.Services;
using Xunit;

namespace NcaafTop25Calendar.Tests;

public class EventUidTests
{
    [Fact]
    public void Full_and_h2h_uids_differ_for_the_same_espn_game_id()
    {
        const string espnId = "401856688";
        string full = IcsWriter.EventUid(espnId, headToHead: false);
        string h2h = IcsWriter.EventUid(espnId, headToHead: true);

        Assert.Equal("401856688@ncaaf-top25-calendar", full);
        Assert.Equal("401856688@h2h.ncaaf-top25-calendar", h2h);
        Assert.NotEqual(full, h2h);
    }

    [Fact]
    public void Uids_are_stable_across_calls()
    {
        Assert.Equal(
            IcsWriter.EventUid("401856688", headToHead: true),
            IcsWriter.EventUid("401856688", headToHead: true));
    }

    [Fact]
    public void Written_ics_uses_namespaced_uids()
    {
        var game = new Game
        {
            Id = "401856688",
            StartUtc = DateTimeOffset.Parse("2026-09-19T23:30:00Z"),
            EndUtc = DateTimeOffset.Parse("2026-09-20T02:30:00Z"),
            HomeTeam = "Ole Miss",
            HomeRank = 8,
            AwayTeam = "LSU",
            AwayRank = 7
        };

        string dir = Path.Combine(Path.GetTempPath(), "ncaaf-uid-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string fullPath = Path.Combine(dir, "full.ics");
            string h2hPath = Path.Combine(dir, "h2h.ics");
            IcsWriter.Write(fullPath, new[] { game }, "College Football Top 25");
            IcsWriter.Write(h2hPath, new[] { game }, "College Football Top25 H2H");

            string fullIcs = File.ReadAllText(fullPath);
            string h2hIcs = File.ReadAllText(h2hPath);
            Assert.Contains("UID:401856688@ncaaf-top25-calendar", fullIcs);
            Assert.Contains("UID:401856688@h2h.ncaaf-top25-calendar", h2hIcs);
            Assert.DoesNotContain("UID:401856688\r\n", fullIcs);
            Assert.DoesNotContain("UID:401856688\r\n", h2hIcs);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
