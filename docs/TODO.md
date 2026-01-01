# Against The Spread - TODO List

*Captured during CFBD sync testing - January 1, 2025*

---

## UI/UX Improvements

- [ ] Make Admin Upload page more mobile friendly / friendly to narrow viewports

---

## Testing Progress - 2025 Data Sync

| Week | Status | Games Synced | Notes |
|------|--------|--------------|-------|
| API Status | ✅ | - | Configured |
| Week 1 | ⏳ | | |
| Week 2 | | | |
| Week 3 | | | |
| Week 4 | | | |
| Week 5 | | | |
| Week 6 | | | |
| Week 7 | | | |
| Week 8 | | | |
| Week 9 | | | |
| Week 10 | | | |
| Week 11 | | | |
| Week 12 | | | |
| Week 13 | | | |
| Week 14 | | | |
| Week 15 | | | |
| Bowl Games | | | |

---

## Bugs Found

- [ ] Sync errors not visible on screen - failed network request shows no error message to user
  - Payload returned: `{"error": "Failed to sync games from external API"}`
  - Need to display error message in UI when sync fails
- [ ] Sync API returns generic error messages - need more detailed error info for debugging
  - `SportsDataSyncFunction.cs:118-123` catches all exceptions and returns generic message
  - Should return actual error message (at least in dev) for debugging
- [ ] Sync API returns HTTP 200 for errors - should return proper 4xx/5xx status codes
  - Got 200 with `{"error": "Failed to sync games from external API"}` in body
  - Makes it hard for UI to detect and display errors properly

## Critical Issues

- [x] **Prod database missing `Games` table** - EF migrations not applied
  - Error: `Invalid object name 'Games'`
  - Need to run database migrations on prod SQL server
  - Dev likely has the table, prod doesn't
  - **RESOLVED:** Added automated EF migrations to CI/CD pipeline

- [x] **E2E tests failing in CI** - ✅ FIXED
  - Root cause: Picks page showed "No weeks available for 2026" and hid year selector
  - Fix: Modified Picks.razor to show error message within week selection UI (year selector stays visible)
  - Full CI/CD pipeline restored and working

---

## Open Questions

*(none yet)*
