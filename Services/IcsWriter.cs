using System.IO;
using System.Collections.Generic;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using NcaafTop25Calendar.Models;
using System;

namespace NcaafTop25Calendar.Services
{
    public static class IcsWriter
    {
        public static void Write(string filePath, IEnumerable<Game> games)
        {
            Write(filePath, games, "College Football Top 25");
        }

        private static string BuildDescription(Game g)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(g.TvProvider))
            {
                lines.Add($"TV: {g.TvProvider}");
                lines.Add(string.Empty);
            }
            
            // Add game status and score information
            string gameStatus = g.BuildGameStatusDescription();
            if (!string.IsNullOrWhiteSpace(gameStatus))
            {
                lines.Add(gameStatus);
                lines.Add(string.Empty);
            }
            
            lines.Add("Calendar by Kevin Bowling");
            lines.Add("http://kevinbowling.me");
            lines.Add(string.Empty);
            lines.Add("Find a mistake? Email me: hello@kevinbowling.me");

            // Use CRLF; the full VCALENDAR string is normalized later as well
            return string.Join("\r\n", lines);
        }

        public static void Write(string filePath, IEnumerable<Game> games, string calendarName)
        {
            var calendar = new Calendar
            {
                Method = "PUBLISH",
                Scale = "GREGORIAN",
                Version = "2.0",
                ProductId = "-//ncaaf-top25-calendar//EN"
            };
            
            // Set appropriate description based on calendar type
            string calendarDescription;
            if (calendarName.Contains("H2H", StringComparison.OrdinalIgnoreCase))
            {
                calendarDescription = "Top 25 NCAA Football head-to-head matchups only (past 24 hours + next 3 weeks).";
            }
            else
            {
                calendarDescription = "Top 25 NCAA Football games (past 24 hours + next 3 weeks).";
            }
            
            // Friendly calendar name and description for clients like Google/Apple
            calendar.Properties.Add(new CalendarProperty("X-WR-CALNAME", calendarName));
            calendar.Properties.Add(new CalendarProperty("X-WR-CALDESC", calendarDescription));
            foreach (var g in games)
            {
                var ev = new CalendarEvent
                {
                    Uid = g.Id,
                    Summary = g.BuildTitle(),
                    DtStart = new CalDateTime(g.StartUtc.UtcDateTime, "UTC"),
                    DtEnd = new CalDateTime(g.EndUtc.UtcDateTime, "UTC"),
                    Location = g.Location ?? string.Empty,
                    Description = BuildDescription(g),
                    Url = g.Url,
                    Status = "CONFIRMED",
                    Transparency = TransparencyType.Opaque
                };
                calendar.Events.Add(ev);
            }

            var serializer = new CalendarSerializer();
            string ical = serializer.SerializeToString(calendar) ?? string.Empty;
            // Normalize to CRLF per RFC 5545 for maximum client compatibility
            ical = ical.Replace("\r\n", "\n").Replace("\n", "\r\n");
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, ical);
        }
    }
}


