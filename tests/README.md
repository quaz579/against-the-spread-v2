# Against The Spread - Playwright Smoke Tests

This directory contains end-to-end smoke tests for the Against The Spread application using Playwright and TypeScript.

## Overview

The smoke tests validate the complete user flow:
1. **Environment Setup**: Starts Azurite, Azure Functions API, and Blazor Web App locally
2. **Data Upload**: Uploads Week 11 and Week 12 test data to Azurite blob storage
3. **User Flow**: Simulates a user making picks for both weeks
4. **Validation**: Downloads and validates the generated Excel files

## Prerequisites

- **Node.js** (v18 or later)
- **npm** (comes with Node.js)
- **.NET 8 SDK**
- **Azure Functions Core Tools** (v4)
- **Azurite** (installed via npm)

## Installation

1. Install dependencies:
   ```bash
   cd tests
   npm install
   ```

2. Install Playwright browsers:
   ```bash
   npx playwright install chromium
   ```

## Running Tests

### Run all tests
```bash
npm test
```

### Run with headed browser (visible UI)
```bash
npm run test:headed
```

### Debug tests
```bash
npm run test:debug
```

### Run with UI mode (interactive)
```bash
npm run test:ui
```

### View test report
```bash
npm run test:report
```

## Project Structure

```
tests/
├── helpers/
│   ├── test-environment.ts    # Manages Azurite, Functions, Web App processes
│   └── excel-validator.ts     # Validates Excel file structure
├── specs/
│   ├── week11.spec.ts         # Week 11 smoke test
│   └── week12.spec.ts         # Week 12 smoke test
├── global-setup.ts            # Global setup/teardown
├── playwright.config.ts       # Playwright configuration
├── package.json               # Dependencies
├── tsconfig.json              # TypeScript configuration
└── README.md                  # This file
```

## Test Flow

Each smoke test follows this flow:

1. **Navigate** to the application
2. **Enter** user name
3. **Select** year (2025) and week (11 or 12)
4. **Click** Continue to load games
5. **Select** 6 games by clicking team buttons
6. **Download** the Excel file
7. **Validate** Excel structure:
   - Row 1: Empty
   - Row 2: Empty
   - Row 3: Headers (Name, Pick 1-6)
   - Row 4: Data (user name and 6 picks)

## Configuration

The tests are configured in `playwright.config.ts`:

- **Base URL**: `http://localhost:5158`
- **Browser**: Chromium
- **Retries**: 2 on CI, 0 locally
- **Workers**: 1 (sequential execution)
- **Traces**: On first retry
- **Screenshots**: On failure
- **Video**: On failure

## CI/CD Integration

The tests run automatically on every pull request via GitHub Actions:

- Workflow file: `.github/workflows/smoke-tests.yml`
- Installs all dependencies (Azure Functions Core Tools, Azurite, Playwright)
- Runs tests in CI environment
- Uploads test results and traces as artifacts

## Troubleshooting

### Tests fail to start services

- Ensure ports 7071 (Functions), 5158 (Web), and 10000 (Azurite) are available
- Check that .NET 8 SDK and Azure Functions Core Tools are installed
- Try running services manually first to verify they work

### Tests timeout waiting for services

- Increase timeout in `test-environment.ts` `waitForService` method
- Check service logs for startup errors
- Ensure all dependencies are built (`dotnet build`)

### Excel validation fails

- Check that the downloaded file exists in `/tmp`
- Verify the Excel structure matches the expected format
- Look at the test output for specific validation errors

## Development

### Adding New Tests

1. Create a new file in `specs/` directory (e.g., `week13.spec.ts`)
2. Import test utilities from `@playwright/test`
3. Use `test.describe` and `test` to structure your tests
4. Follow the existing test patterns

### Modifying Test Environment

Edit `helpers/test-environment.ts` to:
- Change service startup logic
- Modify upload behavior
- Adjust timeouts and retries

### Custom Assertions

Edit `helpers/excel-validator.ts` to add custom Excel validation logic.

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Playwright TypeScript Guide](https://playwright.dev/docs/test-typescript)
- [Against The Spread Repository](https://github.com/quaz579/against-the-spread)
