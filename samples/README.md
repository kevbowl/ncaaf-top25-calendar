# Sample JSON Files for Testing

This directory contains sample game data from the ESPN API for testing and debugging the calendar generation logic.

## File Descriptions

### Top 25 Calendar Samples

1. **`top25-completed-game.json`** - LSU Tigers at Clemson Tigers (LSU #9, Clemson #4)
   - Status: `STATUS_FINAL`
   - Both teams ranked in Top 25
   - Shows final scores and completed game structure

2. **`top25-inprogress-game.json`** - Arkansas-Pine Bluff at Texas Tech Red Raiders (Texas Tech #23)
   - Status: `STATUS_IN_PROGRESS`
   - Only one team ranked (Texas Tech #23)
   - Shows live game structure with period, clock, scores

3. **`top25-upcoming-game.json`** - Virginia Tech Hokies at South Carolina Gamecocks
   - Status: `STATUS_SCHEDULED`
   - Only one team ranked (South Carolina)
   - Shows upcoming game structure

### Top 25 H2H (Head-to-Head) Calendar Samples

4. **`top25-h2h-completed-game.json`** - LSU Tigers at Clemson Tigers (LSU #9, Clemson #4)
   - Status: `STATUS_FINAL`
   - Both teams ranked in Top 25 (H2H matchup)
   - Shows final scores and completed game structure

5. **`top25-h2h-upcoming-game.json`** - Virginia Tech Hokies at South Carolina Gamecocks
   - Status: `STATUS_SCHEDULED`
   - Both teams ranked in Top 25 (H2H matchup)
   - Shows upcoming game structure

## Usage

These files can be used to:
- Test game status detection logic
- Debug score extraction
- Verify rank filtering
- Test time/quarter parsing
- Validate calendar generation

## Data Structure

Each file contains the complete ESPN API response for a single game, including:
- Game metadata (ID, name, date, venue)
- Team information and rankings
- Game status and scores
- Broadcast information
- Competition details

## Testing Scenarios

- **Completed Games**: Test final score display and status detection
- **In-Progress Games**: Test live score display, quarter/time parsing
- **Upcoming Games**: Test scheduled game handling and no-score display
- **H2H Games**: Test Top 25 vs Top 25 matchup filtering
