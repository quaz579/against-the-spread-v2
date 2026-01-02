# Against The Spread - TODO List

*Captured during CFBD sync testing - January 1, 2025*

---

## UI/UX Improvements

- [x] Make Admin Upload page more mobile friendly / friendly to narrow viewports
  - **RESOLVED:** Added `g-3` gutter spacing and `h-100` equal height to sync cards

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

- [x] Sync errors not visible on screen - failed network request shows no error message to user
  - **RESOLVED:** Added `ExtractErrorMessage` helper to parse error responses and display actual messages
  - Backend now returns `{"success": false, "message": "..."}` format
- [x] Sync API returns generic error messages - need more detailed error info for debugging
  - **RESOLVED:** Changed `CreateErrorResponse` to include actual error message in response body
- [x] Sync API returns HTTP 200 for errors - should return proper 4xx/5xx status codes
  - **ALREADY FIXED:** Backend was already returning proper status codes (BadGateway, InternalServerError)
  - Issue was frontend not parsing error response body - now fixed

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
