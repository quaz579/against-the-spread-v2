# Against The Spread - Playwright E2E Tests

This directory contains end-to-end (E2E) tests for the Against The Spread application using Playwright and TypeScript.

## Overview

The E2E tests validate the complete user flow:
1. **Admin Login**: Uses SWA CLI mock authentication to login as admin
2. **Data Upload**: Uploads Week 11 and Week 12 lines via the admin UI
3. **User Flow**: Simulates a user making picks for both weeks
4. **Validation**: Downloads and validates the generated Excel files

**Key architectural decision**: Services must be started before running tests using `./start-e2e.sh`. This approach:
- Makes debugging easier
- Avoids port conflicts and timing issues
- Allows running tests multiple times without restarting services
- Keeps Playwright tests focused on browser automation

**Note**: E2E tests require `start-e2e.sh` (not `start-local.sh`) because they need:
- SWA CLI running for mock authentication on admin routes
- The Blazor app configured to route API calls through SWA CLI (port 4280)

## Prerequisites

- **Node.js** (v18 or later)
- **npm** (comes with Node.js)
- **.NET 8 SDK**
- **Azure Functions Core Tools** (v4)
- **Azurite** (install globally: `npm install -g azurite`)
- **SWA CLI** (installed automatically via npx)

## Installation

1. Install test dependencies:
   ```bash
   cd tests
   npm install
   ```

2. Install Playwright browsers:
   ```bash
   npx playwright install chromium
   ```

## Starting Services

Before running tests, start all E2E services from the repository root:

```bash
./start-e2e.sh
```

This starts:
- **Azurite** (port 10000) - Storage emulator
- **Azure Functions** (port 7071) - Backend API  
- **Blazor Web App** (port 5158) - Frontend
- **SWA CLI** (port 4280) - Authentication proxy with mock auth

Wait for services to initialize (~15 seconds). The SWA CLI proxy at port 4280 is the main entry point.

## Running Tests

### Run all tests
```bash
cd tests
npm test
```

### Run with headed browser (visible UI)
```bash
npm run test:headed
```

### Debug tests interactively
```bash
npm run test:debug
```

### Run with Playwright UI mode
```bash
npm run test:ui
```

### View HTML test report
```bash
npm run test:report
```

## Stopping Services

After testing, stop all E2E services:

```bash
./stop-e2e.sh
```

## Project Structure

```
tests/
├── helpers/
│   ├── test-environment.ts    # Test environment configuration
│   ├── excel-validator.ts     # Validates Excel file structure
│   ├── download-helper.ts     # Handles file downloads
│   └── index.ts               # Exports all helpers
├── pages/
│   ├── admin-page.ts          # Page Object Model for admin page
│   └── picks-page.ts          # Page Object Model for picks page
├── specs/
│   └── full-flow.spec.ts      # Complete user flow tests (Week 11 & 12)
├── playwright.config.ts       # Playwright configuration
├── package.json               # Dependencies
├── tsconfig.json              # TypeScript configuration
└── README.md                  # This file
```

## Test Flow

Each test follows this flow:

1. **Login** to admin page using SWA CLI mock authentication
2. **Upload lines** Excel file via the admin UI
3. **Navigate** to the picks page
4. **Enter** user name
5. **Select** year (2025) and week (11 or 12)
6. **Select** 6 games by clicking team buttons
7. **Download** the Excel file
8. **Validate** Excel structure matches expected format

## How Authentication Works

The tests use SWA CLI's mock authentication feature:

1. **SWA CLI** proxies requests and provides mock auth at `/.auth/login/google`
2. **Mock auth page** allows entering any email (tests use `test-admin@example.com`)
3. **SWA CLI** creates an auth cookie and injects `X-MS-CLIENT-PRINCIPAL` header
4. **Azure Functions** read the header to authenticate/authorize requests
5. **CookieHandler** in Blazor ensures cookies are sent with API requests

The admin email `test-admin@example.com` is configured in `local.settings.json` as an allowed admin.

## Configuration

Tests are configured in `playwright.config.ts`:

- **Base URL**: `http://localhost:4280` (SWA CLI proxy)
- **Browser**: Chromium
- **Retries**: 2 on CI, 0 locally
- **Workers**: 1 (sequential execution)
- **Traces**: On first retry
- **Screenshots**: On failure
- **Video**: On failure
- **Timeouts**: 15s for actions, 30s for navigation

## CI/CD Integration

Tests run automatically on pull requests via GitHub Actions:

- Workflow: `.github/workflows/e2e-tests.yml`
- Installs dependencies (Azure Functions Core Tools, Azurite, Playwright)
- Starts services using `start-local.sh`
- Runs tests with `npm test`
- Uploads test results as artifacts on failure

## Troubleshooting

### Services not running

Ensure services are started before running tests:
```bash
./start-local.sh
```

Check all 4 ports are listening:
```bash
lsof -i :10000 -i :7071 -i :5158 -i :4280 | grep LISTEN
```

### Port conflicts

Stop any conflicting processes:
```bash
./stop-local.sh
```

### Authentication fails (403 errors)

- Verify SWA CLI is running on port 4280
- Check that `test-admin@example.com` is in `ADMIN_EMAILS` in `local.settings.json`
- Ensure the Blazor app is configured to use port 4280 (`appsettings.Development.json`)

### Tests timeout

- Run in headed mode to see what's happening: `npm run test:headed`
- Check browser console for JavaScript errors
- Verify all services are responding

### Excel validation fails

- Check downloaded file exists in `/tmp/playwright-downloads`
- Verify Excel structure matches `reference-docs/Weekly Picks Example.xlsx`
- Review test output for specific validation errors

## Reference Files

Test data in `reference-docs/`:
- `Week 11 Lines.xlsx` - Week 11 game lines
- `Week 12 Lines.xlsx` - Week 12 game lines  
- `Weekly Picks Example.xlsx` - Expected output format

## Adding New Tests

1. Create test file in `specs/` directory
2. Use existing page objects from `pages/`
3. Follow patterns in `full-flow.spec.ts`
4. Run locally before pushing

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [SWA CLI Documentation](https://azure.github.io/static-web-apps-cli/)
- [Project Repository](https://github.com/quaz579/against-the-spread)
