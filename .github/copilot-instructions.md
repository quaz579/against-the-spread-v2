# Copilot Instructions for Against The Spread

## Project Overview

This is a Progressive Web Application (PWA) for managing a weekly college football pick'em game built with:
- **Frontend**: Blazor WebAssembly PWA
- **Backend**: Azure Functions (C# .NET 8)
- **Storage**: Azure Blob Storage
- **Infrastructure**: Terraform
- **Testing**: xUnit, bUnit, Moq, FluentAssertions, Playwright

## Development Guidelines

### Code Style & Standards

**C# Conventions:**
- Use PascalCase for public members and classes
- Use camelCase for private fields and local variables
- Interface prefix: `IServiceName`
- One class per file, matching the file name
- Use nullable reference types appropriately
- Add XML documentation for public APIs

**Naming:**
- Test methods: `MethodName_Scenario_ExpectedBehavior`
- Example: `GetLinesAsync_WithValidWeek_ReturnsWeeklyLines`
- Be descriptive and specific in names

### Testing Requirements

**Always follow Test-Driven Development (TDD) - Red-Green-Refactor:**

This project follows TDD principles inspired by Martin Fowler's teachings on software craftsmanship.

**The TDD Cycle:**
1. **RED** - Write a failing test that defines desired behavior
2. **GREEN** - Write the minimum code to make the test pass
3. **REFACTOR** - Clean up the code while keeping tests green

**Key Principles (Martin Fowler / Kent Beck style):**
- **Write tests first** - Tests are design tools, not just verification
- **Small steps** - Make one small change at a time, run tests frequently
- **Refactor mercilessly** - Clean code with confidence because tests protect you
- **Simple design** - Do the simplest thing that could possibly work
- **No speculation** - Don't add features "just in case" (YAGNI)
- **Express intent** - Code should read like well-written prose
- **Remove duplication** - DRY (Don't Repeat Yourself)

**Refactoring Guidelines:**
- Refactor only when tests are green
- Make small, reversible changes
- Run tests after each refactoring step
- Extract methods when code gets complex
- Rename for clarity - names should reveal intent
- Replace conditionals with polymorphism when appropriate

**Code Smells to Watch For:**
- Long methods (>20 lines is a warning sign)
- Large classes with too many responsibilities
- Duplicate code across methods/classes
- Comments explaining "what" instead of "why"
- Feature envy (method uses another class's data excessively)
- Primitive obsession (using primitives instead of small objects)

**CRITICAL: All tests must pass before any task is complete:**
- Unit tests: `dotnet test`
- E2E tests: `cd tests && npm test` (requires `./start-e2e.sh` running)
- Both test suites MUST pass before committing changes

**When to consider new E2E tests:**
- Adding new user-facing features
- Modifying existing user flows
- Changing API endpoints used by the UI
- Updating admin functionality

**Test Structure (AAA Pattern):**
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var service = new ExcelService();
    var stream = GetTestFileStream();
    
    // Act - Execute the method under test
    var result = service.ParseLinesFromExcel(stream);
    
    // Assert - Verify expected outcomes
    result.Should().NotBeNull();
    result.Week.Should().Be(1);
}
```

**Test Coverage Goals:**
- Core Library: >90%
- Functions: >80%
- Web Components: >70%

### End-to-End Testing with Playwright

**Playwright E2E tests validate the complete user flow including authentication:**
- Located in `tests/` directory with TypeScript
- Tests the full stack: Azurite → Functions → SWA CLI (mock auth) → Blazor Web App
- Uses SWA CLI mock authentication for admin routes
- Uploads test data via admin UI and validates Excel downloads
- Runs on every PR via GitHub Actions

**Key E2E Test Documentation:**
- `tests/README.md` - Complete E2E testing guide
- `tests/specs/full-flow.spec.ts` - Main test scenarios
- `tests/pages/admin-page.ts` - Admin page interactions
- `tests/pages/picks-page.ts` - Picks page interactions

**Running Playwright tests:**
```bash
# Start E2E environment (from repo root)
./start-e2e.sh

# Install dependencies (first time only)
cd tests
npm install
npx playwright install chromium

# Run all tests
npm test

# Run with visible browser
npm run test:headed

# Debug tests interactively
npm run test:debug

# View test report
npm run test:report

# Stop E2E environment when done
cd .. && ./stop-e2e.sh
```

**E2E Test Requirements:**
- Start services with `./start-e2e.sh` (NOT `./start-local.sh`)
- `start-e2e.sh` configures Blazor app to route API calls through SWA CLI (port 4280)
- SWA CLI provides mock authentication at `/.auth/login/google`
- Ports: 10000 (Azurite), 7071 (Functions), 5158 (Web), 4280 (SWA CLI)
- Tests validate Excel format matches `reference-docs/Weekly Picks Example.xlsx`
- See `tests/README.md` for detailed documentation

### Build & Validation

**After each meaningful change, run:**
```bash
# Compile and check for errors
dotnet build

# Run all unit tests (156+ tests)
dotnet test

# Start E2E environment and run Playwright tests
./start-e2e.sh
cd tests && npm test
./stop-e2e.sh  # When done

# For regular local development (no SWA CLI needed)
./start-local.sh
```

**IMPORTANT: A coding task is NOT complete until:**
1. ✅ Code compiles without errors (`dotnet build`)
2. ✅ All unit tests pass (`dotnet test`)
3. ✅ All E2E tests pass (`cd tests && npm test`)
4. ✅ **Manual validation in browser** - Run the app locally and verify UI changes work as expected
5. ✅ Documentation is updated if needed

**Manual Validation Requirements:**
- ALWAYS run `./start-e2e.sh` and open http://localhost:4280 in a browser
- For UI changes: Verify the feature works visually, not just that tests pass
- For API changes: Test the endpoint through the UI or use browser dev tools
- For error handling: Trigger the error condition and verify the message displays correctly
- Do NOT assume changes work just because tests pass - tests may not cover all scenarios

**Do NOT commit code that:**
- Doesn't compile
- Has failing unit tests
- Has failing E2E tests
- Contains hardcoded credentials or secrets
- Lacks necessary tests for new features

### Project Structure

```
src/
├── AgainstTheSpread.Core/       # Shared models and services
│   ├── Models/                  # Domain models
│   ├── Interfaces/              # Service contracts
│   └── Services/                # Business logic
├── AgainstTheSpread.Functions/  # Azure Functions API
│   └── Program.cs              # DI configuration
├── AgainstTheSpread.Web/        # Blazor WASM PWA
│   ├── Pages/                  # Razor pages
│   ├── Components/             # Reusable components
│   └── Services/               # API client
└── AgainstTheSpread.Tests/      # Unit & integration tests
    ├── Models/                 # Model tests
    ├── Services/               # Service tests
    ├── Functions/              # API tests
    └── Web/                    # Component tests

tests/                           # E2E Playwright tests
├── specs/                      # Test specifications
├── helpers/                    # Test utilities
└── playwright.config.ts        # Playwright configuration
```

### Dependencies

**When adding new NuGet packages:**
- Only add if absolutely necessary
- Verify compatibility with .NET 8
- Check for security vulnerabilities
- Update project documentation

**Common packages already in use:**
- EPPlus (Excel processing)
- Azure.Storage.Blobs
- Moq (mocking)
- FluentAssertions (test assertions)
- bUnit (Blazor component testing)

### API Design

**Endpoints (User-facing only):**
- `GET /api/weeks?year={year}` - List available weeks
- `GET /api/lines/{week}?year={year}` - Get games for specific week
- `POST /api/picks` - Submit picks and generate Excel download

**Response patterns:**
- Return appropriate HTTP status codes (200, 400, 404, 500)
- Include descriptive error messages
- Use consistent JSON structure
- Handle exceptions gracefully

### Excel Format Requirements

**Critical:** Generated Excel files must EXACTLY match the format in `reference-docs/Weekly Picks Example.xlsx`:
- Row 1: Empty
- Row 2: Empty
- Row 3: Headers (Name, Pick 1, Pick 2, ..., Pick 6)
- Row 4: User data (name and 6 team picks)

### Blazor Component Guidelines

**For Blazor components:**
- Keep components small and focused
- Use dependency injection for services
- Handle loading and error states
- Make UI mobile-first (responsive design)
- Test components with bUnit

### Commit Messages

Follow Conventional Commits format:
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:** `feat`, `fix`, `test`, `refactor`, `docs`, `chore`, `ci`

**Examples:**
```
feat(core): add Excel parsing service
test(functions): add unit tests for admin endpoints
fix(web): correct game selection validation
```

### Error Handling

**Always implement:**
- Input validation on all endpoints
- Null reference checks
- Try-catch blocks for external operations (blob storage, Excel processing)
- User-friendly error messages in UI
- Logging for debugging (Application Insights)

### Security Considerations

**MVP Security (No Authentication):**
- Validate all inputs
- Set appropriate CORS policies
- Enforce file size limits
- Rate limiting on API endpoints
- No sensitive data in responses or logs

**CRITICAL: Azure Static Web Apps Route Security**

The `staticwebapp.config.json` file controls route-level authentication at the SWA platform layer. This runs BEFORE requests reach Azure Functions code.

**Never add "anonymous" to allowedRoles in the production config file.** This makes the production environment completely insecure.

```json
// ❌ WRONG for production - This bypasses ALL authentication
{
  "route": "/api/*",
  "allowedRoles": ["authenticated", "anonymous"]
}

// ✅ CORRECT for production - Only authenticated users can access API routes
{
  "route": "/api/*",
  "allowedRoles": ["authenticated"]
}
```

**Environment-specific config files:**
- `staticwebapp.config.json` - Production config (requires authentication)
- `staticwebapp.config.dev.json` - Dev/preview config (allows anonymous for test auth bypass)

**How test auth bypass works (Dev/Preview environments only):**
1. The committed `staticwebapp.config.json` requires authentication for API routes (production)
2. The CI workflows (`deploy.yml` and `pr-preview.yml`) copy `staticwebapp.config.dev.json` over `staticwebapp.config.json` before deploying to dev/preview
3. Dev/preview gets `anonymous` in allowedRoles so test auth headers (X-Test-User-Email) can reach the Function code
4. Production deploys the original secure config - no anonymous access

**The test auth bypass pattern:**
- SWA platform layer: Allows requests through (in Dev/Preview via config file swap)
- Azure Function code: `AuthHelper.cs` checks `ENABLE_TEST_AUTH` env var + `X-Test-User-Email` header
- Production: Uses original `staticwebapp.config.json` (no anonymous), and `ENABLE_TEST_AUTH` is never set

**If E2E tests fail with 302 redirects in CI:** The SWA platform is blocking requests before they reach Functions. Check that the workflow is copying `staticwebapp.config.dev.json` before deployment.

### Documentation

**Update documentation when:**
- Adding new features
- Changing API contracts
- Modifying configuration requirements
- Updating deployment process

**Key files to maintain:**
- `README.md` - Project overview and setup
- `CONTRIBUTING.md` - Development workflow
- `TESTING.md` - Testing strategy
- `.agents.md` - Agent development guide
- `implementation-plan.md` - Development roadmap
- `docs/database-schema.mmd` - Database ER diagram (Mermaid format)

**Database Schema Documentation:**
When making changes to database models, migrations, or Entity Framework entities:
- Update `docs/database-schema.mmd` to reflect the current schema
- Include all tables, columns, data types, and relationships
- Keep foreign key relationships accurate

### Performance

**Optimize for:**
- Mobile devices (PWA)
- Azure Functions consumption plan
- Blob storage operations (minimize round trips)
- Excel generation (streaming for large files)

### Deployment

**Environment awareness:**
- Local development uses Azurite and SWA CLI
- Dev environment for testing
- Production environment for live usage
- CI/CD via GitHub Actions on merge to main

**Infrastructure:**
- Managed via Terraform (infrastructure/terraform/)
- Do NOT manually modify Azure resources
- Update Terraform files for infrastructure changes

### Database Administration

**Running SQL queries against Azure SQL Database:**

Install Microsoft SQL tools via Homebrew (if not already installed):
```bash
# Check if sqlcmd is installed
which sqlcmd || (brew tap microsoft/mssql-release https://github.com/Microsoft/homebrew-mssql-release && HOMEBREW_ACCEPT_EULA=Y brew install msodbcsql18 mssql-tools18)
```

Run queries using `sqlcmd`:
```bash
# Load credentials and run a query
cd infrastructure/terraform
source .credentials
sqlcmd -S $DEV_SQL_SERVER -d $DEV_SQL_DATABASE -U $SQL_ADMIN_LOGIN -P "$SQL_ADMIN_PASSWORD" -N -C -Q "SELECT * FROM Users"

# For production (use with caution!)
sqlcmd -S $PROD_SQL_SERVER -d $PROD_SQL_DATABASE -U $SQL_ADMIN_LOGIN -P "$SQL_ADMIN_PASSWORD" -N -C -Q "SELECT * FROM Users"
```

**Database connection details:**
- Dev: `sql-dev-cus-atsv2.database.windows.net` / `sqldb-dev-cus-atsv2`
- Prod: `sql-prod-cus-atsv2.database.windows.net` / `sqldb-prod-cus-atsv2`
- Credentials: `infrastructure/terraform/.credentials`

**Tables (see `docs/database-schema.mmd` for full schema):**
- `Users` - User accounts
- `Games` - Weekly games with lines
- `Picks` - User picks for weekly games
- `BowlGames` - Bowl games with lines
- `BowlPicks` - User picks for bowl games
- `TeamAliases` - Team name mappings
- `__EFMigrationsHistory` - EF Core migrations (do not modify)

### Task Acceptance Criteria

**When completing a task:**
- All unit tests pass (`dotnet test`) - 156+ tests
- All E2E tests pass (`cd tests && npm test`) - 2+ tests
- Code compiles without warnings
- Documentation is updated
- Follows established patterns
- Includes error handling
- Mobile-friendly (for UI changes)
- Security considerations addressed

**For significant changes, consider:**
- Adding new E2E test scenarios in `tests/specs/`
- Updating Page Object Models in `tests/pages/`
- Updating `tests/README.md` if test setup changes

### MVP Scope Focus

**Current phase:** Building MVP functionality
- Admin manually uploads weekly lines (no web UI needed)
- Users select 6 games and download Excel picks
- PWA installable on mobile devices
- Focus on core functionality, not enhancements

**Out of scope for MVP:**
- User authentication (trust-based)
- Pick history storage
- Automated scoring
- Leaderboards
- Push notifications

### Resources

**Reference files:**
- `reference-docs/Week 1 Lines.xlsx` - Sample input
- `reference-docs/Weekly Picks Example.xlsx` - Expected output format

**Documentation:**
- `.agents.md` - Comprehensive agent guide
- `implementation-plan.md` - Detailed implementation steps
- `TESTING.md` - Complete testing guide (unit tests)
- `CONTRIBUTING.md` - Contribution guidelines

**E2E Testing (Playwright):**
- `tests/README.md` - **Complete E2E testing guide** (START HERE)
- `tests/specs/full-flow.spec.ts` - Main E2E test scenarios
- `tests/pages/admin-page.ts` - Admin page object model
- `tests/pages/picks-page.ts` - Picks page object model
- `tests/helpers/` - Test utilities and validators
- `start-e2e.sh` / `stop-e2e.sh` - E2E environment scripts

### Common Pitfalls to Avoid

- ❌ Skipping tests
- ❌ Not running builds before committing
- ❌ Hardcoding values (use configuration)
- ❌ Ignoring mobile experience
- ❌ Breaking Excel format compatibility
- ❌ Adding unnecessary dependencies
- ❌ Creating large, unfocused PRs
- ❌ **Dismissing test failures as "flaky tests"** - This is an anti-pattern

### Handling Test Failures

**CRITICAL: Never dismiss test failures as "flaky tests" without investigation.**

When tests fail (especially in CI):
1. **Investigate the root cause** - Read the error messages, check logs, reproduce locally
2. **Fix the underlying issue** - Don't assume it's intermittent without evidence
3. **If truly flaky, fix the test** - A test that sometimes fails is a bug in the test
4. **Ask for permission** - If a test is genuinely bad and needs to be skipped/deleted, ask the user first

Common causes of "flaky" tests that are actually real bugs:
- Environment configuration differences (local vs CI)
- Race conditions in async code
- Missing test data setup
- Authentication/authorization issues
- Network timeouts that indicate real problems

Remember: Tests exist to catch bugs. A failing test is doing its job until proven otherwise.

### Success Indicators

- ✅ Tests written first (TDD)
- ✅ All tests pass
- ✅ Code compiles cleanly
- ✅ Documentation updated
- ✅ Follows existing patterns
- ✅ Mobile-friendly
- ✅ Security considered
- ✅ Small, focused commits

---

**Remember:** Build incrementally, test frequently, commit often. This project uses AI-assisted development with human code review.
