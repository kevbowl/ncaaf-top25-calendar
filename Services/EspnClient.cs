using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NcaafTop25Calendar.Services
{
    public sealed record WeekQuery(int SeasonYear, int SeasonType, IReadOnlyList<int> Weeks, bool LookaheadKickoff = false);

    public sealed class EspnClient : IDisposable
    {
        private const string BaseScoreboardUrl = "https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard";
        // ESPN/Akamai 403s several UAs including "ncaaf-top25-calendar/1.0" and typical browser strings.
        private const string UserAgent = "ncaaf-top25-calendar/1.0 (https://github.com/kevbowl/ncaaf-top25-calendar)";
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public EspnClient(HttpMessageHandler? handler = null)
        {
            _httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<JsonDocument> GetInitialScoreboardAsync()
        {
            try
            {
                return await FetchJsonAsync(BaseScoreboardUrl);
            }
            catch (HttpRequestException)
            {
                // Bare scoreboard URL is sometimes blocked; dated query still works.
                string dates = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                string year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
                return await FetchJsonAsync($"{BaseScoreboardUrl}?year={year}&dates={dates}");
            }
        }

        public async Task<JsonDocument> GetWeekScoreboardAsync(int seasonYear, int seasonType, int week)
        {
            string url = $"{BaseScoreboardUrl}?year={seasonYear}&seasontype={seasonType}&week={week}";
            return await FetchJsonAsync(url);
        }

        public static int TryGetSeasonYear(JsonDocument doc)
        {
            try
            {
                if (doc.RootElement.TryGetProperty("season", out var season) && season.TryGetProperty("year", out var yearEl))
                {
                    return yearEl.GetInt32();
                }
            }
            catch { }
            return DateTime.UtcNow.Year;
        }

        public static int TryGetSeasonType(JsonDocument doc)
        {
            try
            {
                if (doc.RootElement.TryGetProperty("season", out var season) && season.TryGetProperty("type", out var typeEl))
                {
                    return typeEl.GetInt32();
                }
            }
            catch { }
            return 2;
        }

        public static int TryGetCurrentWeek(JsonDocument doc)
        {
            try
            {
                if (doc.RootElement.TryGetProperty("week", out var w) && w.TryGetProperty("number", out var n))
                {
                    return n.GetInt32();
                }
            }
            catch { }
            return 1;
        }

        /// <summary>
        /// In-season: the ESPN calendar that contains <paramref name="nowUtc"/> (regular or postseason),
        /// previous week + the next <paramref name="upcomingWeekCount"/> weeks including current.
        /// Offseason / gap after the last calendar / before Week 1: regular-season weeks 1..N of the
        /// upcoming FBS season so the feed restarts at kickoff without waiting for a manual run.
        /// </summary>
        public static WeekQuery BuildWeekQuery(JsonDocument doc, DateTimeOffset nowUtc, int upcomingWeekCount, int fallbackStartWeek)
        {
            int year = TryGetSeasonYear(doc);
            int seasonType = TryGetSeasonType(doc);
            upcomingWeekCount = Math.Max(1, upcomingWeekCount);

            try
            {
                if (doc.RootElement.TryGetProperty("leagues", out var leagues) && leagues.ValueKind == JsonValueKind.Array && leagues.GetArrayLength() > 0)
                {
                    var league = leagues[0];
                    if (league.TryGetProperty("calendar", out var calendars) && calendars.ValueKind == JsonValueKind.Array)
                    {
                        JsonElement? regular = null;
                        JsonElement? post = null;
                        int matchingType = 0;
                        DateTimeOffset? regularStart = null;
                        DateTimeOffset? regularEnd = null;

                        foreach (var cal in calendars.EnumerateArray())
                        {
                            int type = TryReadInt(cal, "value") ?? 0;
                            DateTimeOffset? calStart = cal.TryGetProperty("startDate", out var csd) ? TryParseDate(csd) : null;
                            DateTimeOffset? calEnd = cal.TryGetProperty("endDate", out var ced) ? TryParseDate(ced) : null;
                            if (type == 2)
                            {
                                regular = cal;
                                regularStart = calStart;
                                regularEnd = calEnd;
                            }
                            else if (type == 3)
                            {
                                post = cal;
                            }
                            if (calStart != null && calEnd != null && nowUtc >= calStart && nowUtc < calEnd)
                            {
                                matchingType = type;
                            }
                        }

                        if (matchingType == 2 && regular is JsonElement regularCal)
                        {
                            var weeks = SelectWeeksFromCalendar(regularCal, nowUtc, upcomingWeekCount, fallbackStartWeek);
                            return new WeekQuery(year, 2, weeks);
                        }

                        if (matchingType == 3 && post is JsonElement postCal)
                        {
                            var weeks = SelectWeeksFromCalendar(postCal, nowUtc, upcomingWeekCount, fallbackStartWeek);
                            return new WeekQuery(year, 3, weeks);
                        }

                        // Offseason, mid-winter gap, or the weeks before kickoff: poll Week 1 of the next FBS season.
                        int kickoffYear = (regularStart != null && nowUtc < regularStart) ? year : year + 1;
                        if (regularEnd != null && nowUtc < regularEnd && kickoffYear > year)
                        {
                            kickoffYear = year;
                        }
                        return new WeekQuery(kickoffYear, 2, KickoffWeeks(upcomingWeekCount), LookaheadKickoff: true);
                    }
                }
            }
            catch { }

            var fallback = new List<int>();
            int start = Math.Max(1, fallbackStartWeek - 1);
            for (int i = start; i < fallbackStartWeek + upcomingWeekCount; i++)
            {
                if (i > 0) fallback.Add(i);
            }
            return new WeekQuery(year, seasonType, fallback);
        }

        public static IReadOnlyList<int> KickoffWeeks(int upcomingWeekCount)
        {
            upcomingWeekCount = Math.Max(1, upcomingWeekCount);
            var weeks = new List<int>(upcomingWeekCount);
            for (int i = 1; i <= upcomingWeekCount; i++) weeks.Add(i);
            return weeks;
        }

        public static IReadOnlyList<int> GetNextWeeks(JsonDocument doc, DateTimeOffset nowUtc, int count, int fallbackStartWeek)
        {
            return BuildWeekQuery(doc, nowUtc, count, fallbackStartWeek).Weeks;
        }

        private static List<int> SelectWeeksFromCalendar(JsonElement cal, DateTimeOffset nowUtc, int upcomingWeekCount, int fallbackStartWeek)
        {
            var result = new List<int>();
            if (!cal.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            int currentIndex = -1;
            var weekNumbers = new List<int>();
            int idx = 0;
            foreach (var entry in entries.EnumerateArray())
            {
                int weekNum = -1;
                if (entry.TryGetProperty("value", out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number) weekNum = v.GetInt32();
                    else if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var parsed)) weekNum = parsed;
                }
                DateTimeOffset? start = entry.TryGetProperty("startDate", out var sd) ? TryParseDate(sd) : null;
                DateTimeOffset? end = entry.TryGetProperty("endDate", out var ed) ? TryParseDate(ed) : null;
                weekNumbers.Add(weekNum);
                if (start != null && end != null && nowUtc >= start && nowUtc < end)
                {
                    currentIndex = idx;
                }
                idx++;
            }

            if (currentIndex < 0)
            {
                int found = weekNumbers.IndexOf(fallbackStartWeek);
                currentIndex = found >= 0 ? found : 0;
            }

            int startIndex = Math.Max(0, currentIndex - 1);
            int endIndex = Math.Min(weekNumbers.Count - 1, currentIndex + Math.Max(0, upcomingWeekCount - 1));
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (weekNumbers[i] > 0 && !result.Contains(weekNumbers[i]))
                {
                    result.Add(weekNumbers[i]);
                }
            }

            return result;
        }

        private static int? TryReadInt(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var parsed)) return parsed;
            return null;
        }

        private static DateTimeOffset? TryParseDate(JsonElement el)
        {
            try
            {
                return el.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(el.GetString(), out var dto) ? dto : null;
            }
            catch { return null; }
        }

        private async Task<JsonDocument> FetchJsonAsync(string url)
        {
            using var resp = await _httpClient.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(stream);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
