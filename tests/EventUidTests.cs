using NcaafTop25Calendar.Models;
using NcaafTop25Calendar.Services;
using Xunit;

namespace NcaafTop25Calendar.Tests;

public class EventUidTests
{
    [Fact]
    public void Full_and_h2h_uids_are_distinct_and_use_github_io_fqdn()
    {
        const string espnId = "401856688";
        string full = IcsWriter.EventUid(espnId, headToHead: false);
        string h2h = IcsWriter.EventUid(espnId, headToHead: true);

        Assert.Equal("401856688@kevbowl.github.io", full);
        Assert.Equal("401856688-h2h@kevbowl.github.io", h2h);
        Assert.NotEqual(full, h2h);
        Assert.Contains("@kevbowl.github.io", full);
        Assert.Contains("@kevbowl.github.io", h2h);
        Assert.DoesNotContain("@ncaaf-top25-calendar", full);
        Assert.DoesNotContain("@ncaaf-top25-calendar", h2h);
    }

    [Fact]
    public void Uids_are_stable_across_calls()
    {
        Assert.Equal(
            IcsWriter.EventUid("401856688", headToHead: true),
            IcsWriter.EventUid("401856688", headToHead: true));
        Assert.Equal(
            IcsWriter.EventUid("401856688", headToHead: false),
            IcsWriter.EventUid("401856688", headToHead: false));
    }

    [Fact]
    public void Written_ics_uses_fqdn_uids_so_google_can_toggle_calendars()
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
            Assert.Contains("UID:401856688@kevbowl.github.io\r\n", fullIcs);
            Assert.Contains("UID:401856688-h2h@kevbowl.github.io\r\n", h2hIcs);
            Assert.DoesNotContain("UID:401856688\r\n", fullIcs);
            Assert.DoesNotContain("UID:401856688\r\n", h2hIcs);
            Assert.DoesNotContain("@ncaaf-top25-calendar", fullIcs + h2hIcs);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
