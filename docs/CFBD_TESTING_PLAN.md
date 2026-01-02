# CFBD Integration Testing Plan

## Overview

This document outlines the testing plan for validating the College Football Data (CFBD) API integration during the offseason. The goal is to ensure the sync functionality works correctly before the next football season begins.

---

## 1. API Connectivity & Configuration Tests

### 1.1 Verify API Key is Configured (Manual - Admin UI)
- [ ] Navigate to Admin page on **Dev**: https://blue-smoke-0b4410710.2.azurestaticapps.net/admin
- [x] Navigate to Admin page on **Prod**: https://ashy-pebble-07e5d0b10.2.azurestaticapps.net/admin ✅ (2026-01-01)
- [x] Click "Check API Status" button ✅ (2026-01-01)
- [x] Verify response shows: ✅ (2026-01-01)
  - Provider: `CollegeFootballData`
  - Status: `Configured ✓`

### 1.2 Verify API Key via Direct API Call (Can be automated)
```bash
# Dev environment
curl -s "https://blue-smoke-0b4410710.2.azurestaticapps.net/api/sync/status" | jq

# Prod environment - TESTED ✅ (2026-01-01)
curl -s "https://ashy-pebble-07e5d0b10.2.azurestaticapps.net/api/sync/status" | jq

# Expected response:
# {
#   "provider": "CollegeFootballData",
#   "isConfigured": true,
#   "message": "Sports data provider 'CollegeFootballData' is ready"
# }
```

### 1.3 Test API Key Validity with CFBD Directly
```bash
# Test the API key directly against CFBD
curl -s "https://api.collegefootballdata.com/games?year=2024&week=1&seasonType=regular" \
  -H "Authorization: Bearer YOUR_API_KEY" | jq length

# Should return number of games (e.g., 80+)
```

---

## 2. Weekly Games Sync Tests

### 2.1 Sync Historical Week (2024 Season Data)
Use 2024 data since it's complete and we can verify results.

| Test Case | Week | Year | Expected Behavior | Result (Prod 2026-01-01) |
|-----------|------|------|-------------------|--------------------------|
| Early season | 1 | 2024 | ~80-90 FBS games with spreads | ✅ **110 games** (includes FCS) |
| Mid season | 8 | 2024 | ~50-60 FBS games | Not tested |
| Rivalry week | 14 | 2024 | Conference championship games | Not tested |
| Bowl week | 1 (postseason) | 2024 | Bowl games (if supported) | Not tested |

**Steps:**
1. Go to Admin > CFBD Sync section ✅
2. Select Week 1, Year 2024 ✅
3. Click "Sync Week 1" ✅
4. Verify success message shows game count ✅ (110 games)
5. Navigate to "Make Picks" page ⚠️ **See Known Issue below**
6. Change week/year to Week 1, 2024
7. Verify games appear with spreads

> **⚠️ Known Issue**: Games synced via CFBD are stored in the **database**, but the Make Picks page uses `WeeksFunction` which reads from **blob storage**. This means synced games don't appear in the UI. Games can be verified via the `/api/games/{week}?year={year}` endpoint.

### 2.2 Verify Game Data Quality ✅ (2026-01-01)
After syncing Week 1 2024, manually verify a few games:

| Check | How to Verify | Result |
|-------|---------------|--------|
| Teams are correct | Compare with ESPN/official schedule | ✅ Georgia vs Clemson, LSU vs USC confirmed |
| Spreads are reasonable | Should be within typical range (-35 to +35) | ✅ Range: -2.0 to -50.5 |
| Game times are correct | Compare with historical records | ✅ Dates match Aug 24 - Sep 2, 2024 |
| Home/Away is correct | Home team should be second in matchup | ✅ Favorite/Underdog format used |

**Sample games verified:**
| Game | CFBD Spread | Notes |
|------|-------------|-------|
| Georgia vs Clemson | Georgia -10.5 | ✅ Georgia was heavy favorite |
| LSU vs USC | LSU -4.0 | ✅ Vegas had ~-6.5 |
| Texas A&M vs Notre Dame | A&M -3.0 | ✅ Home field advantage |
| Michigan vs Fresno State | Michigan -21.0 | ✅ Defending champs |
| Ohio State vs Akron | OSU -50.5 | ✅ Massive mismatch |

### 2.3 Sync Idempotency Test ✅ (2026-01-01)
1. Sync Week 1, 2024 ✅
2. Note the game count: **110 games**
3. Sync Week 1, 2024 again ✅
4. Verify no duplicate games are created ✅ **Still 110 games**
5. Verify spreads are updated (not duplicated) ✅

---

## 3. Bowl Games Sync Tests

### 3.1 Sync Historical Bowl Season ✅ (2026-01-01)
| Test Case | Year | Expected Behavior | Result (Prod 2026-01-01) |
|-----------|------|-------------------|--------------------------|
| 2024 Bowl Season | 2024 | ~40-45 bowl games | ✅ **50 bowl games** |
| 2023 Bowl Season | 2023 | ~40-45 bowl games | Not tested |

**Steps:**
1. Go to Admin > CFBD Sync section ✅
2. Enter Bowl Season Year: 2024 ✅
3. Click "Sync Bowl Games" ✅
4. Verify success message ✅ (50 bowl games synced)
5. Navigate to "Bowl Picks" page - Not tested (same blob storage issue)
6. Verify bowl games appear with:
   - Bowl names ⚠️ Uses team matchup format, not traditional bowl names
   - Teams ✅
   - Spreads ✅
   - Dates ✅

**Bowl games verified via `/api/bowl-games?year=2024`:**
| Bowl | Matchup | Spread | Date |
|------|---------|--------|------|
| Game 1 | SC State vs Jackson State | -3.5 | 2024-12-14 |
| Game 2 | South Alabama vs W. Michigan | -6.0 | 2024-12-15 |
| Game 3 | Memphis vs West Virginia | -5.0 | 2024-12-18 |

### 3.2 Verify Bowl Data Quality ✅ (2026-01-01)
| Check | How to Verify | Result |
|-------|---------------|--------|
| Bowl names are correct | Compare with official bowl schedule | ⚠️ Uses matchup format, not bowl names |
| Matchups are correct | Verify teams played in each bowl | ✅ Teams correct |
| Spreads are present | Most bowls should have spreads | ✅ All have spreads |
| Dates are correct | Compare with historical dates | ✅ Dec 14, 2024 onwards |

---

## 4. Error Handling Tests

### 4.1 Invalid Week/Year Combinations ✅ (2026-01-01)
| Test | Input | Expected Result | Actual Result (Prod) |
|------|-------|-----------------|----------------------|
| Future year | Week 1, 2030 | Empty result or error message | ✅ "Synced 0 games" - graceful |
| Invalid week | Week 20, 2024 | Empty result or error message | Not tested (UI limits to 17) |
| Very old year | Week 1, 2000 | May work (CFBD has historical data) | ✅ "Synced 0 games" - no crash |

> **Note**: Error handling is graceful (no crashes), but showing "Success! Synced 0 games" might be misleading. Consider showing a warning instead.

### 4.2 API Rate Limiting
- CFBD free tier: 1,000 requests/month
- Test behavior when approaching limit
- Verify graceful error handling

### 4.3 Network Error Handling
- Test with API key temporarily removed
- Verify appropriate error message shown to user

---

## 5. Data Comparison Tests

### 5.1 Compare CFBD Data with Known Source
Pick 5 games from Week 1, 2024 and compare:

| Game | CFBD Spread | ESPN/Vegas Spread | Match? |
|------|-------------|-------------------|--------|
| Game 1 | | | |
| Game 2 | | | |
| Game 3 | | | |
| Game 4 | | | |
| Game 5 | | | |

### 5.2 Spot Check Bowl Games
Pick 3 bowl games from 2024 season:

| Bowl | CFBD Data | Actual Result | Spread Accurate? |
|------|-----------|---------------|------------------|
| Bowl 1 | | | |
| Bowl 2 | | | |
| Bowl 3 | | | |

---

## 6. Integration Tests (Automated)

### 6.1 Potential E2E Test Cases
Consider adding these to the Playwright test suite:

```typescript
// tests/specs/cfbd-sync.spec.ts (proposed)

test('Admin can check CFBD API status', async ({ page }) => {
  // Login as admin
  // Navigate to admin page
  // Click "Check API Status"
  // Verify success message appears
});

test('Admin can sync weekly games', async ({ page }) => {
  // Login as admin
  // Navigate to admin page
  // Select Week 1, 2024
  // Click sync button
  // Verify success message with game count
  // Navigate to picks page
  // Verify games are visible
});

test('Admin can sync bowl games', async ({ page }) => {
  // Login as admin
  // Navigate to admin page
  // Enter year 2024
  // Click sync bowl games
  // Verify success message
  // Navigate to bowl picks page
  // Verify bowl games are visible
});
```

### 6.2 API Integration Test (Unit Test Level)
```csharp
// Proposed: tests/AgainstTheSpread.Functions.Tests/SportsDataSyncTests.cs

[Fact]
public async Task SyncStatus_ReturnsConfigured_WhenApiKeySet()
{
    // Arrange - set CFBD_API_KEY env var
    // Act - call sync/status endpoint
    // Assert - isConfigured = true
}

[Fact]
public async Task SyncWeeklyGames_ReturnsGames_ForValidWeek()
{
    // Arrange - set up test with Week 1, 2024
    // Act - call sync endpoint
    // Assert - games returned > 0
}
```

---

## 7. Pre-Season Checklist

Before the 2025 season starts:

- [ ] Verify API key is still valid (keys may expire)
- [ ] Test sync with Week 1, 2025 data (when available)
- [ ] Verify spreads are populating correctly
- [ ] Check CFBD API usage (stay under 1,000/month limit)
- [ ] Confirm bowl game sync works for upcoming season
- [ ] Review any CFBD API changes/deprecations

---

## Open Questions

### Data Quality
1. **Spread Source**: Does CFBD aggregate spreads from multiple sources? Which sportsbook(s)?
2. **Update Frequency**: How often does CFBD update spreads? Are they live or snapshot?
3. **Line Movement**: Do we want to track line movement, or just use the spread at sync time?

### Feature Gaps
4. **Postseason Weeks**: Does CFBD handle playoff/championship games differently than bowl games?
5. **FCS Games**: Do we want to include FCS games, or filter to FBS only?
6. **Conference Games Only**: Should we add a filter for conference-only games?

### Operational
7. **Sync Schedule**: Should we auto-sync on a schedule, or keep it manual?
8. **Notifications**: Should admins be notified when spreads change significantly?
9. **Backup Plan**: What's the fallback if CFBD is down or API key expires mid-season?

### Testing
10. **Test Data Cleanup**: Should synced test data be cleaned up, or kept for reference?
11. **Staging Environment**: Do we need a separate staging env for testing?
12. **API Mock**: Should we mock CFBD responses in E2E tests to avoid API calls?

---

## Appendix: CFBD API Reference

### Endpoints Used
- `GET /games` - Weekly games with spreads
- `GET /games` with `seasonType=postseason` - Bowl games
- `GET /lines` - Betting lines (alternative endpoint)

### Documentation
- CFBD API Docs: https://api.collegefootballdata.com/api/docs
- CFBD Portal: https://collegefootballdata.com

### Rate Limits
- Free tier: 1,000 requests/month
- Patreon supporters get higher limits

---

## Known Issues & Architecture Notes

### Issue 1: Database vs Blob Storage Mismatch
**Discovered**: 2026-01-01 during Prod testing

The CFBD sync stores games to the **database** (via `IGameService` and `IBowlGameService`), but the frontend Make Picks page reads available weeks from **blob storage** (via `IStorageService.GetAvailableWeeksAsync()`).

**Impact**: Games synced from CFBD don't appear in the Make Picks UI dropdown.

**Workaround**: Games can be verified via direct API calls:
- Weekly games: `GET /api/games/{week}?year={year}`
- Bowl games: `GET /api/bowl-games?year={year}`

**Resolution Options**:
1. Update `WeeksFunction` to also check database for available weeks
2. Have CFBD sync write to blob storage in addition to database
3. Migrate Make Picks page to use database-backed endpoints

### Issue 2: Bowl Names Use Matchup Format
Bowl games synced from CFBD use the team matchup as the bowl name (e.g., "Memphis vs West Virginia") instead of traditional bowl names (e.g., "Frisco Bowl").

**Impact**: Users won't see the familiar bowl names in the UI.

**Resolution Options**:
1. Map CFBD game data to official bowl names
2. Accept matchup format as sufficient

---

*Last Updated: January 1, 2026*
*Next Review: Before 2025 Season (August 2025)*
