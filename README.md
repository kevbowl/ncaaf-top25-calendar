# College Football Top 25 Calendar

Generates iCal (.ics) files with Top-25 NCAA Football matchups for the past 24 hours and next 3 weeks using ESPN's scoreboard API. Now includes live game status, scores, and a separate Head-to-Head calendar for Top 25 vs Top 25 matchups.

## Subscribe to Calendars

- **All Top 25 Games**: `https://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf.ics`
- **Top 25 Head-to-Head Only**: `https://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf-h2h.ics`

## API

- ESPN Scoreboard API: `https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard`
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

Output: 
- `docs/top25-ncaaf.ics` - All Top 25 games (past 24h + next 3 weeks)
- `docs/top25-ncaaf-h2h.ics` - Top 25 vs Top 25 matchups only (past 24h + next 3 weeks)

## Features

### Game Information
- Filters games where either team has `curatedRank.current` between 1 and 25
- **Time Window**: Includes games from past 24 hours and next 3 weeks (not just upcoming)
- **Live Game Status**: Shows current quarter, time remaining, and scores for games in progress
- **Final Scores**: Displays final scores for completed games
- **Upcoming Games**: Clean display for games that haven't started yet (no scores shown)

### Calendar Details
- **Title format**: `🏈 #6 Washington at #9 Auburn` (ranks included when available; >25 hidden)
- **Location format**: `Venue, City, ST, Country` (comma-separated)
- **Description includes**:
  - TV broadcast information when available
  - Live game status and scores (e.g., "3rd 12:34: 24-31")
  - Final scores (e.g., "Final: 14-42")
  - Calendar attribution and contact information

### Calendar Names & Descriptions
- **Main Calendar**: "College Football Top 25" - "Top 25 NCAA Football games (past 24 hours + next 3 weeks)."
- **H2H Calendar**: "College Football Top25 H2H" - "Top 25 NCAA Football head-to-head matchups only (past 24 hours + next 3 weeks)."

## Automated Updates

The calendar automatically refreshes with an optimized schedule:

### During Game Times (Every Hour)
- **Friday**: 6am-12pm SGT
- **Saturday**: 6am-12pm SGT  
- **Sunday**: 12am-1pm SGT
- **Monday**: 6am-12pm SGT

### During Off-Hours (Once Daily)
- **Tuesday-Wednesday**: Once daily at 6:00 UTC (2:00 PM SGT)
- **Thursday**: Once daily at 6:00 UTC (2:00 PM SGT) - before games start

This ensures real-time updates during actual games and efficient operation during off-hours.

## Technical Details

- Uses Ical.Net for calendar generation
- Implements Ical.Net for robust iCal compliance
- GitHub Actions workflow with optimized cron scheduling
- ESPN API integration for live data
- Score and game status extraction from API responses
- Enhanced game status detection for better ESPN data parsing

## Sources

- ESPN Scoreboard API: [`https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard`](https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard)
- Community docs: [`pseudo-r/Public-ESPN-API`](https://github.com/pseudo-r/Public-ESPN-API)
