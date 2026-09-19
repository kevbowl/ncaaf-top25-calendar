using System.Text.Json;
using NcaafTop25Calendar.Services;
using Xunit;

namespace NcaafTop25Calendar.Tests;

public class WeekQueryTests
{
    private const string Calendar2026 = """
        {
          "season": { "year": 2026, "type": 2 },
          "week": { "number": 3 },
          "leagues": [{
            "calendar": [
              {
                "label": "Regular Season",
                "value": "2",
                "startDate": "2026-08-22T07:00Z",
                "endDate": "2026-12-13T07:59Z",
                "entries": [
                  { "label": "Week 1", "value": "1", "startDate": "2026-08-22T07:00Z", "endDate": "2026-09-08T06:59Z" },
                  { "label": "Week 2", "value": "2", "startDate": "2026-09-08T07:00Z", "endDate": "2026-09-14T06:59Z" },
                  { "label": "Week 3", "value": "3", "startDate": "2026-09-14T07:00Z", "endDate": "2026-09-21T06:59Z" },
                  { "label": "Week 4", "value": "4", "startDate": "2026-09-21T07:00Z", "endDate": "2026-09-28T06:59Z" },
                  { "label": "Week 5", "value": "5", "startDate": "2026-09-28T07:00Z", "endDate": "2026-10-05T06:59Z" }
                ]
              },
              {
                "label": "Postseason",
                "value": "3",
                "startDate": "2026-12-13T08:00Z",
                "endDate": "2027-01-28T07:59Z",
                "entries": [
                  { "label": "FBS Playoff", "value": "1", "startDate": "2026-12-13T08:00Z", "endDate": "2027-01-20T07:59Z" }
                ]
              },
              {
                "label": "Off Season",
                "value": "4",
                "startDate": "2027-01-28T08:00Z",
                "endDate": "2027-02-01T07:59Z",
                "entries": [
                  { "label": "Off Season", "value": "1", "startDate": "2027-01-28T08:00Z", "endDate": "2027-02-01T07:59Z" }
                ]
              }
            ]
          }]
        }
        """;

    [Fact]
    public void Midseason_uses_regular_season_weeks_including_upcoming()
    {
        using var doc = JsonDocument.Parse(Calendar2026);
        var q = EspnClient.BuildWeekQuery(doc, DateTimeOffset.Parse("2026-09-19T12:00:00Z"), 3, 3);
        Assert.Equal(2026, q.SeasonYear);
        Assert.Equal(2, q.SeasonType);
        Assert.False(q.LookaheadKickoff);
        Assert.Equal(new[] { 2, 3, 4, 5 }, q.Weeks);
    }

    [Fact]
    public void Postseason_stays_on_bowl_calendar()
    {
        using var doc = JsonDocument.Parse(Calendar2026);
        var q = EspnClient.BuildWeekQuery(doc, DateTimeOffset.Parse("2026-12-20T12:00:00Z"), 3, 1);
        Assert.Equal(2026, q.SeasonYear);
        Assert.Equal(3, q.SeasonType);
        Assert.False(q.LookaheadKickoff);
        Assert.Contains(1, q.Weeks);
    }

    [Fact]
    public void Offseason_looks_ahead_to_next_regular_season_week_1()
    {
        using var doc = JsonDocument.Parse(Calendar2026);
        var q = EspnClient.BuildWeekQuery(doc, DateTimeOffset.Parse("2027-01-30T12:00:00Z"), 3, 1);
        Assert.Equal(2027, q.SeasonYear);
        Assert.Equal(2, q.SeasonType);
        Assert.True(q.LookaheadKickoff);
        Assert.Equal(new[] { 1, 2, 3 }, q.Weeks);
    }

    [Fact]
    public void Midsummer_after_calendars_end_looks_ahead_to_next_season()
    {
        using var doc = JsonDocument.Parse(Calendar2026);
        var q = EspnClient.BuildWeekQuery(doc, DateTimeOffset.Parse("2027-06-15T12:00:00Z"), 3, 15);
        Assert.Equal(2027, q.SeasonYear);
        Assert.Equal(2, q.SeasonType);
        Assert.True(q.LookaheadKickoff);
        Assert.Equal(new[] { 1, 2, 3 }, q.Weeks);
    }

    [Fact]
    public void Pre_kickoff_same_year_polls_week_1_not_stale_week_index()
    {
        using var doc = JsonDocument.Parse(Calendar2026);
        var q = EspnClient.BuildWeekQuery(doc, DateTimeOffset.Parse("2026-07-01T12:00:00Z"), 3, 15);
        Assert.Equal(2026, q.SeasonYear);
        Assert.Equal(2, q.SeasonType);
        Assert.True(q.LookaheadKickoff);
        Assert.Equal(new[] { 1, 2, 3 }, q.Weeks);
    }

    [Fact]
    public void Week1_kickoff_window_is_regular_season()
    {
        using var doc = JsonDocument.Parse(Calendar2026);
        var q = EspnClient.BuildWeekQuery(doc, DateTimeOffset.Parse("2026-08-23T18:00:00Z"), 3, 1);
        Assert.Equal(2026, q.SeasonYear);
        Assert.Equal(2, q.SeasonType);
        Assert.False(q.LookaheadKickoff);
        Assert.Equal(new[] { 1, 2, 3 }, q.Weeks);
    }
}
