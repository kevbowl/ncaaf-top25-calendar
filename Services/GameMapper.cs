using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NcaafTop25Calendar.Models;

namespace NcaafTop25Calendar.Services
{
    public static class GameMapper
    {
        public static IEnumerable<Game> MapTop25Upcoming(JsonDocument weekDoc, DateTimeOffset nowUtc)
        {
            if (!weekDoc.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            var seen = new HashSet<string>();
            foreach (var ev in events.EnumerateArray())
            {
                string id = ev.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;

                DateTimeOffset? start = ev.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(d.GetString(), out var dto) ? dto : null;
                if (start == null) continue;
                
                // Include games from past 24 hours and future games (instead of excluding past games entirely)
                if (start.Value < nowUtc.AddHours(-24)) continue;

                if (!ev.TryGetProperty("competitions", out var comps) || comps.ValueKind != JsonValueKind.Array || comps.GetArrayLength() == 0) continue;
                var comp = comps[0];

                if (!comp.TryGetProperty("competitors", out var competitors) || competitors.ValueKind != JsonValueKind.Array || competitors.GetArrayLength() < 2) continue;

                var home = FindByHomeAway(competitors, "home") ?? competitors[0];
                var away = FindByHomeAway(competitors, "away") ?? competitors[1];

                int? homeRank = NormalizeRank(TryGetRank(home));
                int? awayRank = NormalizeRank(TryGetRank(away));
                bool involvesTop25 = (homeRank is >= 1 and <= 25) || (awayRank is >= 1 and <= 25);
                if (!involvesTop25) continue;

                string homeName = GetTeamName(home) ?? "Home";
                string awayName = GetTeamName(away) ?? "Away";

                var location = BuildLocation(comp);
                var tv = GetTv(comp);
                var url = TryGetSummaryUrl(ev);

                // Extract score and game status information
                var homeScore = TryGetScore(home);
                var awayScore = TryGetScore(away);
                var gameStatus = GetGameStatus(ev);
                var quarter = GetQuarter(ev);
                var timeRemaining = GetTimeRemaining(ev);

                yield return new Game
                {
                    Id = id,
                    StartUtc = start.Value,
                    EndUtc = start.Value.AddHours(3),
                    HomeTeam = homeName,
                    HomeRank = homeRank,
                    HomeScore = homeScore,
                    AwayTeam = awayName,
                    AwayRank = awayRank,
                    AwayScore = awayScore,
                    Location = location,
                    TvProvider = tv,
                    Url = url,
                    Status = gameStatus,
                    Quarter = quarter,
                    TimeRemaining = timeRemaining
                };
            }
        }

        public static IEnumerable<Game> MapTop25HeadToHead(JsonDocument weekDoc, DateTimeOffset nowUtc)
        {
            if (!weekDoc.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            var seen = new HashSet<string>();
            foreach (var ev in events.EnumerateArray())
            {
                string id = ev.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;

                DateTimeOffset? start = ev.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(d.GetString(), out var dto) ? dto : null;
                if (start == null) continue;
                
                // Include games from past 24 hours and future games (instead of excluding past games entirely)
                if (start.Value < nowUtc.AddHours(-24)) continue;

                if (!ev.TryGetProperty("competitions", out var comps) || comps.ValueKind != JsonValueKind.Array || comps.GetArrayLength() == 0) continue;
                var comp = comps[0];

                if (!comp.TryGetProperty("competitors", out var competitors) || competitors.ValueKind != JsonValueKind.Array || competitors.GetArrayLength() < 2) continue;

                var home = FindByHomeAway(competitors, "home") ?? competitors[0];
                var away = FindByHomeAway(competitors, "away") ?? competitors[1];

                int? homeRank = NormalizeRank(TryGetRank(home));
                int? awayRank = NormalizeRank(TryGetRank(away));
                
                // Both teams must be ranked in top 25 for H2H
                bool isHeadToHead = (homeRank is >= 1 and <= 25) && (awayRank is >= 1 and <= 25);
                if (!isHeadToHead) continue;

                string homeName = GetTeamName(home) ?? "Home";
                string awayName = GetTeamName(away) ?? "Away";

                var location = BuildLocation(comp);
                var tv = GetTv(comp);
                var url = TryGetSummaryUrl(ev);

                // Extract score and game status information
                var homeScore = TryGetScore(home);
                var awayScore = TryGetScore(away);
                var gameStatus = GetGameStatus(ev);
                var quarter = GetQuarter(ev);
                var timeRemaining = GetTimeRemaining(ev);

                yield return new Game
                {
                    Id = id,
                    StartUtc = start.Value,
                    EndUtc = start.Value.AddHours(3),
                    HomeTeam = homeName,
                    HomeRank = homeRank,
                    HomeScore = homeScore,
                    AwayTeam = awayName,
                    AwayRank = awayRank,
                    AwayScore = awayScore,
                    Location = location,
                    TvProvider = tv,
                    Url = url,
                    Status = gameStatus,
                    Quarter = quarter,
                    TimeRemaining = timeRemaining
                };
            }
        }

        private static int? NormalizeRank(int? rank)
        {
            if (rank is null) return null;
            return (rank.Value >= 1 && rank.Value <= 25) ? rank : null;
        }

        private static JsonElement? FindByHomeAway(JsonElement competitors, string homeAway)
        {
            foreach (var c in competitors.EnumerateArray())
            {
                if (c.TryGetProperty("homeAway", out var ha) && string.Equals(ha.GetString(), homeAway, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }
            return null;
        }

        private static int? TryGetRank(JsonElement competitor)
        {
            try
            {
                JsonElement curatedRank;
                if (competitor.TryGetProperty("curatedRank", out curatedRank) || (competitor.TryGetProperty("team", out var team) && team.TryGetProperty("curatedRank", out curatedRank)))
                {
                    if (curatedRank.TryGetProperty("current", out var cur))
                    {
                        if (cur.ValueKind == JsonValueKind.Number) return cur.GetInt32();
                        if (cur.ValueKind == JsonValueKind.String && int.TryParse(cur.GetString(), out var v)) return v;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string? GetTeamName(JsonElement competitor)
        {
            try
            {
                if (competitor.TryGetProperty("team", out var team))
                {
                    if (team.TryGetProperty("shortDisplayName", out var sdn)) return sdn.GetString();
                    if (team.TryGetProperty("displayName", out var dn)) return dn.GetString();
                    if (team.TryGetProperty("name", out var n)) return n.GetString();
                }
            }
            catch { }
            return null;
        }

        private static string? BuildLocation(JsonElement competition)
        {
            try
            {
                if (!competition.TryGetProperty("venue", out var venue)) return null;
                string? full = venue.TryGetProperty("fullName", out var fn) ? fn.GetString() : null;
                string city = venue.TryGetProperty("address", out var addr) && addr.TryGetProperty("city", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                string state = venue.TryGetProperty("address", out var addr2) && addr2.TryGetProperty("state", out var s) ? s.GetString() ?? string.Empty : string.Empty;
                string country = venue.TryGetProperty("address", out var addr3) && addr3.TryGetProperty("country", out var co) ? co.GetString() ?? string.Empty : string.Empty;
                string parts = string.Join(", ", new[] { city, state, country }.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (!string.IsNullOrWhiteSpace(parts))
                {
                    return string.IsNullOrWhiteSpace(full) ? parts : $"{full}, {parts}";
                }
                return full;
            }
            catch { }
            return null;
        }

        private static string? GetTv(JsonElement competition)
        {
            try
            {
                if (!competition.TryGetProperty("broadcasts", out var bcasts) || bcasts.ValueKind != JsonValueKind.Array) return null;
                var names = new List<string>();
                foreach (var b in bcasts.EnumerateArray())
                {
                    if (b.TryGetProperty("names", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        names.AddRange(arr.EnumerateArray().Select(x => x.GetString()).Where(s => !string.IsNullOrWhiteSpace(s))!.Cast<string>());
                    }
                    else if (b.TryGetProperty("name", out var single))
                    {
                        var s = single.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) names.Add(s);
                    }
                }
                return names.Count > 0 ? string.Join("; ", names.Distinct()) : null;
            }
            catch { }
            return null;
        }

        private static Uri? TryGetSummaryUrl(JsonElement ev)
        {
            try
            {
                if (ev.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Array)
                {
                    foreach (var l in links.EnumerateArray())
                    {
                        if (l.TryGetProperty("rel", out var rel) && rel.ValueKind == JsonValueKind.Array)
                        {
                            if (rel.EnumerateArray().Any(x => string.Equals(x.GetString(), "summary", StringComparison.OrdinalIgnoreCase)))
                            {
                                if (l.TryGetProperty("href", out var href))
                                {
                                    string? u = href.GetString();
                                    if (!string.IsNullOrWhiteSpace(u)) return new Uri(u);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static GameStatus GetGameStatus(JsonElement ev)
        {
            try
            {
                if (ev.TryGetProperty("status", out var status))
                {
                    // Check for the completed boolean - it's nested inside the type object
                    if (status.TryGetProperty("type", out var typeEl))
                    {
                        if (typeEl.TryGetProperty("completed", out var completedEl) && completedEl.GetBoolean())
                        {
                            return GameStatus.Final;
                        }
                    }

                    // Check status type name and state
                    var typeName = string.Empty;
                    var typeState = string.Empty;
                    
                    if (status.TryGetProperty("type", out var typeEl2))
                    {
                        typeName = typeEl2.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                        typeState = typeEl2.TryGetProperty("state", out var stateEl) ? stateEl.GetString() ?? string.Empty : string.Empty;
                    }

                    // Check for completed games using various status indicators
                    if (string.Equals(typeName, "STATUS_FINAL", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeName, "final", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeState, "post", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeState, "postgame", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeState, "final", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeState, "ended", StringComparison.OrdinalIgnoreCase))
                    {
                        return GameStatus.Final;
                    }

                    // Check for live games
                    var period = status.TryGetProperty("period", out var periodEl) ? periodEl.GetInt32() : 0;
                    var hasClock = status.TryGetProperty("clock", out var _);
                    
                    if (string.Equals(typeName, "STATUS_IN_PROGRESS", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeName, "in", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeState, "in", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(typeState, "live", StringComparison.OrdinalIgnoreCase) ||
                        (period > 0 && hasClock))
                    {
                        return GameStatus.Live;
                    }
                }
            }
            catch { }
            return GameStatus.Upcoming;
        }

        private static int? TryGetScore(JsonElement competitor)
        {
            try
            {
                if (competitor.TryGetProperty("score", out var score))
                {
                    if (score.ValueKind == JsonValueKind.Number) return score.GetInt32();
                    if (score.ValueKind == JsonValueKind.String && int.TryParse(score.GetString(), out var v)) return v;
                }
            }
            catch { }
            return null;
        }

        private static string? GetQuarter(JsonElement ev)
        {
            try
            {
                if (ev.TryGetProperty("status", out var status))
                {
                    var period = status.TryGetProperty("period", out var periodEl) ? periodEl.GetInt32() : 0;
                    if (period > 0 && period <= 4)
                    {
                        return period switch
                        {
                            1 => "1st",
                            2 => "2nd", 
                            3 => "3rd",
                            4 => "4th",
                            _ => $"{period}Q"
                        };
                    }
                    else if (period > 4)
                    {
                        return "OT";
                    }
                }
            }
            catch { }
            return null;
        }

        private static string? GetTimeRemaining(JsonElement ev)
        {
            try
            {
                if (ev.TryGetProperty("status", out var status))
                {
                    // First try displayClock (already formatted)
                    if (status.TryGetProperty("displayClock", out var displayClockEl) && displayClockEl.ValueKind == JsonValueKind.String)
                    {
                        var displayClock = displayClockEl.GetString();
                        if (!string.IsNullOrWhiteSpace(displayClock))
                        {
                            return displayClock;
                        }
                    }
                    
                    // Fall back to converting numeric clock
                    if (status.TryGetProperty("clock", out var clockEl))
                    {
                        if (clockEl.ValueKind == JsonValueKind.Number)
                        {
                            // Convert seconds to MM:SS format
                            var seconds = clockEl.GetInt32();
                            var minutes = seconds / 60;
                            var remainingSeconds = seconds % 60;
                            return $"{minutes}:{remainingSeconds:D2}";
                        }
                        else if (clockEl.ValueKind == JsonValueKind.String)
                        {
                            var clockStr = clockEl.GetString();
                            if (!string.IsNullOrWhiteSpace(clockStr))
                            {
                                return clockStr;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}


