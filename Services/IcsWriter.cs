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
            var calendar = new Calendar();
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
                    Url = g.Url
                };
                calendar.Events.Add(ev);
            }

            var serializer = new CalendarSerializer();
            string ical = serializer.SerializeToString(calendar) ?? string.Empty;
            File.WriteAllText(filePath, ical);
        }
    }
}


