# College Football Top 25 Calendar

Two live iCal feeds of NCAA football games involving Top 25 teams (past 24 hours plus the next three weeks), with ranks, TV, and scores when a game is live or final.

You do not need a GitHub account or anything installed. Subscribe in your calendar app; the feeds update on their own.

## Calendars

- **All Top 25 games:** [https://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf.ics](https://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf.ics)
- **Head-to-head only** (ranked vs ranked): [https://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf-h2h.ics](https://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf-h2h.ics)

You can subscribe to both. They are separate calendars, so Head-to-head stays visible in Google if you hide the full Top 25 calendar.

On Apple Calendar you can also use `webcal://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf.ics` and `webcal://kevbowl.github.io/ncaaf-top25-calendar/top25-ncaaf-h2h.ics`.

### Google Calendar

1. Open [Google Calendar](https://calendar.google.com) on the web.
2. Next to **Other calendars**, open the **+** menu and choose **From URL**.
3. Paste one of the `https://` links above and click **Add calendar**.
4. Repeat for the other feed if you want both.

### Apple Calendar

1. **File → New Calendar Subscription…** (macOS) or **Calendars → Add Calendar → Add Subscription Calendar** (iOS).
2. Paste an `https://` or `webcal://` link above and subscribe.
3. Repeat for the other feed if you want both.

### Outlook

1. Open Outlook on the web (or the desktop calendar).
2. **Add calendar → Subscribe from web**.
3. Paste an `https://` link above and subscribe.
4. Repeat for the other feed if you want both.

## Run it yourself

Clone this repo, restore, and run (requires the .NET 9 SDK):

```bash
git clone https://github.com/kevbowl/ncaaf-top25-calendar.git
cd ncaaf-top25-calendar
dotnet restore
dotnet run
```

Writes:

- `docs/top25-ncaaf.ics` — all Top 25 games
- `docs/top25-ncaaf-h2h.ics` — ranked vs ranked only

### What’s in an event

- Either team has AP rank 1–25 (`curatedRank.current`).
- Window: past 24 hours plus the next three weeks.
- Title like `🏈 #6 Washington at #9 Auburn` (ranks above 25 omitted).
- Location: venue, city, state, country.
- Description: TV when known, live quarter/clock and score, or final score.

Calendar names: **College Football Top 25** and **College Football Top25 H2H**.

### Refresh schedule

GitHub Actions rebuilds the feeds. Empty offseason fetches do not wipe a populated calendar. A keep-alive job keeps Actions from going to sleep. Daily runs from mid-August through mid-September pick up the next season.

*Note: [+1] is the following day. DST = Daylight Saving Time.*

#### During game times (every hour)

| **Day** | **USA ET** | **UTC** | **SGT** |
|:--------|:-----------|:--------|:--------|
| **Thu** | **6pm-2am**[*+1*] (*DST*)<br/>**5pm-1am**[*+1*] (*Standard*) | **10pm-6am**[*+1*] | **6am-2pm**[*+1*] |
| **Fri** | **6pm-2am**[*+1*] (*DST*)<br/>**5pm-1am**[*+1*] (*Standard*) | **10pm-6am**[*+1*] | **6am-2pm**[*+1*] |
| **Sat** | **12pm-2am**[*+1*] (*DST*)<br/>**11am-1am**[*+1*] (*Standard*) | **4pm-6am**[*+1*] | **12am-2pm**[*+1*] |

#### Off-hours (once daily)

| **Day** | **USA ET** | **UTC** | **SGT** |
|:--------|:-----------|:--------|:--------|
| **Sun** | **3pm** (*DST*)<br/>**2pm** (*Standard*) | **7pm** | **3am**[*+1*] |
| **Mon** | **3pm** (*DST*)<br/>**2pm** (*Standard*) | **7pm** | **3am**[*+1*] |
| **Tue** | **3pm** (*DST*)<br/>**2pm** (*Standard*) | **7pm** | **3am**[*+1*] |
| **Wed** | **3pm** (*DST*)<br/>**2pm** (*Standard*) | **7pm** | **3am**[*+1*] |

Built with Ical.Net against ESPN’s scoreboard API.

## Sources

- ESPN Scoreboard API: [`https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard`](https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard)
- Community docs: [`pseudo-r/Public-ESPN-API`](https://github.com/pseudo-r/Public-ESPN-API)
