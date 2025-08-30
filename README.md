# NCAAF Top-25 Calendar (.NET 9)

Generates an iCal (.ics) with upcoming Top-25 NCAA Football matchups for the next 4 weeks using ESPN's scoreboard API.

- API: `https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard`
- Community reference: [pseudo-r/Public-ESPN-API](https://github.com/pseudo-r/Public-ESPN-API)

## Prerequisites

- .NET 9 SDK

## Setup

```bash
cd /Users/kevin.bowling/Projects/ncaaf-top25-calendar
 dotnet restore
```

## Run

```bash
 dotnet run
```

Output: `top25-ncaaf-next4weeks.ics` in the project root.

## Notes

- Filters games where either team has `curatedRank.current` between 1 and 25.
- Title format: `#6 Washington at #9 Auburn` (ranks included when available).
- Includes venue (name, city/state/country) and TV broadcaster(s) if provided.
- Uses Ical.Net for calendar generation.

## Sources

- ESPN Scoreboard API: [`https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard`](https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard)
- Community docs: [`pseudo-r/Public-ESPN-API`](https://github.com/pseudo-r/Public-ESPN-API)
