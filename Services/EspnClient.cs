using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace NcaafTop25Calendar.Services
{
    public sealed class EspnClient : IDisposable
    {
        private const string BaseScoreboardUrl = "https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard";
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public EspnClient(HttpMessageHandler? handler = null)
        {
            _httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ncaaf-top25-calendar", "1.0"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<JsonDocument> GetInitialScoreboardAsync()
        {
            return await FetchJsonAsync(BaseScoreboardUrl);
        }

        public async Task<JsonDocument> GetWeekScoreboardAsync(int seasonYear, int week)
        {
            string url = $"{BaseScoreboardUrl}?year={seasonYear}&seasontype=2&week={week}";
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

        public static IReadOnlyList<int> GetNextWeeks(JsonDocument doc, DateTimeOffset nowUtc, int count, int fallbackStartWeek)
        {
            var result = new List<int>();
            try
            {
                if (doc.RootElement.TryGetProperty("leagues", out var leagues) && leagues.ValueKind == JsonValueKind.Array && leagues.GetArrayLength() > 0)
                {
                    var league = leagues[0];
                    if (league.TryGetProperty("calendar", out var calendars) && calendars.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var cal in calendars.EnumerateArray())
                        {
                            if (!cal.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
                            {
                                continue;
                            }

                            int currentIndex = -1;
                            var weekNumbers = new List<int>();
                            int idx = 0;
                            foreach (var entry in entries.EnumerateArray())
                            {
                                int weekNum = entry.TryGetProperty("value", out var v) ? v.GetInt32() : -1;
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
                                currentIndex = Math.Max(0, weekNumbers.IndexOf(fallbackStartWeek));
                            }

                            for (int i = currentIndex; i < weekNumbers.Count && result.Count < count; i++)
                            {
                                if (weekNumbers[i] > 0)
                                {
                                    result.Add(weekNumbers[i]);
                                }
                            }

                            if (result.Count > 0)
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            catch { }

            for (int i = 0; i < count; i++)
            {
                result.Add(fallbackStartWeek + i);
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


