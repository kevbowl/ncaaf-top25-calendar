using System.IO;
using System.Collections.Generic;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using NcaafTop25Calendar.Models;

namespace NcaafTop25Calendar.Services
{
    public static class IcsWriter
    {
        public static void Write(string filePath, IEnumerable<Game> games)
        {
            var calendar = new Calendar
            {
                Method = "PUBLISH",
                Scale = "GREGORIAN",
                Version = "2.0",
                ProductId = "-//ncaaf-top25-calendar//EN"
            };
            // Friendly calendar name and description for clients like Google/Apple
            calendar.Properties.Add(new CalendarProperty("X-WR-CALNAME", "College Football Top 25"));
            calendar.Properties.Add(new CalendarProperty("X-WR-CALDESC", "Upcoming Top 25 NCAA Football games (next 4 weeks)."));
            foreach (var g in games)
            {
                var ev = new CalendarEvent
                {
                    Uid = g.Id,
                    Summary = g.BuildTitle(),
                    DtStart = new CalDateTime(g.StartUtc.UtcDateTime, "UTC"),
                    DtEnd = new CalDateTime(g.EndUtc.UtcDateTime, "UTC"),
                    Location = g.Location ?? string.Empty,
                    Description = string.IsNullOrWhiteSpace(g.TvProvider) ? string.Empty : $"TV: {g.TvProvider}",
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


