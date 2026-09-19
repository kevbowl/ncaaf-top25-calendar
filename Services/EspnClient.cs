using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NcaafTop25Calendar.Services
{
    public sealed record WeekQuery(int SeasonYear, int SeasonType, IReadOnlyList<int> Weeks);

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
        /// Picks the ESPN calendar that actually contains <paramref name="nowUtc"/> (regular, postseason, or offseason)
        /// and returns that season type plus the previous week (for the 24h lookback) and the next
        /// <paramref name="upcomingWeekCount"/> weeks including the current week.
        /// </summary>
        public static WeekQuery BuildWeekQuery(JsonDocument doc, DateTimeOffset nowUtc, int upcomingWeekCount, int fallbackStartWeek)
        {
            int year = TryGetSeasonYear(doc);
            int seasonType = TryGetSeasonType(doc);
            var weeks = new List<int>();

            try
            {
                if (doc.RootElement.TryGetProperty("leagues", out var leagues) && leagues.ValueKind == JsonValueKind.Array && leagues.GetArrayLength() > 0)
                {
                    var league = leagues[0];
                    if (league.TryGetProperty("calendar", out var calendars) && calendars.ValueKind == JsonValueKind.Array)
                    {
                        JsonElement? matchingCal = null;
                        foreach (var cal in calendars.EnumerateArray())
                        {
                            DateTimeOffset? calStart = cal.TryGetProperty("startDate", out var csd) ? TryParseDate(csd) : null;
                            DateTimeOffset? calEnd = cal.TryGetProperty("endDate", out var ced) ? TryParseDate(ced) : null;
                            if (calStart != null && calEnd != null && nowUtc >= calStart && nowUtc < calEnd)
                            {
                                matchingCal = cal;
                                break;
                            }
                        }

                        // Fall back to the first calendar that has week entries (regular season).
                        if (matchingCal == null)
                        {
                            foreach (var cal in calendars.EnumerateArray())
                            {
                                if (cal.TryGetProperty("entries", out var ents) && ents.ValueKind == JsonValueKind.Array && ents.GetArrayLength() > 0)
                                {
                                    matchingCal = cal;
                                    break;
                                }
                            }
                        }

                        if (matchingCal is JsonElement calEl)
                        {
                            if (calEl.TryGetProperty("value", out var valEl))
                            {
                                if (valEl.ValueKind == JsonValueKind.Number)
                                {
                                    seasonType = valEl.GetInt32();
                                }
                                else if (valEl.ValueKind == JsonValueKind.String && int.TryParse(valEl.GetString(), out var parsedType))
                                {
                                    seasonType = parsedType;
                                }
                            }

                            weeks.AddRange(SelectWeeksFromCalendar(calEl, nowUtc, upcomingWeekCount, fallbackStartWeek));
                        }
                    }
                }
            }
            catch { }

            if (weeks.Count == 0)
            {
                int start = Math.Max(1, fallbackStartWeek - 1);
                for (int i = start; i < fallbackStartWeek + upcomingWeekCount; i++)
                {
                    if (i > 0) weeks.Add(i);
                }
            }

            return new WeekQuery(year, seasonType, weeks);
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
