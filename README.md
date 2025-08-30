# College Football Top 25 Calendar

Generates an iCal (.ics) with upcoming Top-25 NCAA Football matchups for the next 4 weeks using ESPN's scoreboard API.
- Subscribe to the iCal: `https://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf-next4weeks.ics`
- API: `https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard`
- Community reference: [pseudo-r/Public-ESPN-API](https://github.com/pseudo-r/Public-ESPN-API)

## Prerequisites

- .NET SDK (tested with .NET 9)

## Setup

```bash
cd /Users/kevin.bowling/Projects/ncaaf-top25-calendar
 dotnet restore
```

## Run

```bash
 dotnet run
```

Output: `docs/top25-ncaaf-next4weeks.ics` (GitHub Pages ready).

## Notes

- Filters games where either team has `curatedRank.current` between 1 and 25.
- Title format: `🏈 #6 Washington at #9 Auburn` (ranks included when available; >25 hidden).
- Location format: `Venue, City, ST, Country` (comma-separated).
- Description includes TV when available, plus:
  - `Calendar by Kevin Bowling`
  - `http://kevinbowling.me`
  - `Find a mistake? Email me: hello@kevinbowling.me`
- Calendar name: "College Football Top 25" (via X-WR-CALNAME).
- Uses Ical.Net for calendar generation.

## Sources

- ESPN Scoreboard API: [`https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard`](https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard)
- Community docs: [`pseudo-r/Public-ESPN-API`](https://github.com/pseudo-r/Public-ESPN-API)
