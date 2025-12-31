# Against The Spread - Major Enhancement Roadmap

## Progress Tracking

| Phase | Status | Progress | Description |
|-------|--------|----------|-------------|
| 1 | Complete | 19/19 | Infrastructure Separation |
| 2 | Complete | 18/18 | Database Foundation |
| 3 | Complete | 27/27 | User Authentication & Pick Submission |
| 4 | Complete | 20/20 | Admin Results Entry |
| 5 | Complete | 22/22 | Leaderboard |
| 6 | Complete | 11/11 | Bowl Games |
| 7 | Complete | 8/8 | Sports Data API |
| 8 | Complete | 12/12 | PR Preview Environments & E2E Testing |

**Overall Progress**: 137/137 tasks completed

**Note**: All phases complete with full unit and E2E test coverage.

---

## Overview

Transform the Against The Spread application from a spreadsheet-based pick'em tool to a full-featured platform with user authentication, database storage, leaderboards, and automatic scoring.

## Requirements Summary

1. **Azure SQL Database** with EF Core Code First
2. **Leaderboard** - Weekly wins and season standings
3. **Separate environments** - Dev and prod in separate resource groups with separate Static Web Apps
4. **Terraform** - All infrastructure with tfvars for dev/prod
5. **Dual pick submission** - Spreadsheet (anonymous) + authenticated in-app picks
6. **Game locking** - Server-side + client-side enforcement at kickoff
7. **Admin results entry** - UI and bulk upload options
8. **Future: Auto-pull spreads** from sports data API (Phase 7)

---

## Phase 1: Infrastructure Separation

**Goal**: Separate dev/prod environments with proper Terraform structure
**Status**: Complete
**Prerequisites**: None

### 1.1 Terraform State Storage Setup
- [x] **1.1.1** Create resource group for Terraform state: `az group create --name ats-tfstate-rg --location centralus`
- [x] **1.1.2** Create storage account for state: `az storage account create --name atstfstate --resource-group ats-tfstate-rg --sku Standard_LRS`
- [x] **1.1.3** Create blob container for state: `az storage container create --name tfstate --account-name atstfstate`

### 1.2 Terraform Configuration
- [x] **1.2.1** Create `infrastructure/terraform/main.tf` - All resources (Resource Group, Storage, SQL, Functions, Static Web App, App Insights)
- [x] **1.2.2** Create `infrastructure/terraform/variables.tf` - All input variables
- [x] **1.2.3** Create `infrastructure/terraform/outputs.tf` - Export connection strings, deployment tokens
- [x] **1.2.4** Create `infrastructure/terraform/dev.tfvars` - Dev-specific values (Basic SQL SKU, Free SWA)
- [x] **1.2.5** Create `infrastructure/terraform/prod.tfvars` - Prod-specific values (Standard SQL SKU, Standard SWA)

### 1.4 GitHub Actions Workflows
- [x] **1.4.1** Create `.github/workflows/deploy-dev.yml` - triggers on push to `dev` branch
- [x] **1.4.2** Create `.github/workflows/deploy-prod.yml` - triggers on push to `main` branch
- [x] **1.4.3** Delete `.github/workflows/azure-static-web-apps-agreeable-river-0e2f38010.yml` after validation

### 1.5 Resource Naming Convention
Convention: `{resource type}-{environment}-{region abbrev}-atsv2` (storage accounts: `st{env}{region}atsv2`)

| Resource | Dev | Prod |
|----------|-----|------|
| Resource Group | `rg-dev-cus-atsv2` | `rg-prod-cus-atsv2` |
| Storage Account | `stdevcusatsv2` | `stprodcusatsv2` |
| Function App | `func-dev-cus-atsv2` | `func-prod-cus-atsv2` |
| Static Web App | `swa-dev-cus-atsv2` | `swa-prod-cus-atsv2` |
| SQL Server | `sql-dev-cus-atsv2` | `sql-prod-cus-atsv2` |
| SQL Database | `sqldb-dev-cus-atsv2` | `sqldb-prod-cus-atsv2` |
| App Insights | `ai-dev-cus-atsv2` | `ai-prod-cus-atsv2` |
| Service Plan | `asp-dev-cus-atsv2` | `asp-prod-cus-atsv2` |

**Static Web App URLs:**
- Dev: https://blue-smoke-0b4410710.2.azurestaticapps.net
- Prod: https://ashy-pebble-07e5d0b10.2.azurestaticapps.net

### Phase 1 Key Files
- `infrastructure/terraform/main.tf` - All infrastructure resources
- `infrastructure/terraform/variables.tf` - Variable definitions
- `infrastructure/terraform/outputs.tf` - Output definitions
- `infrastructure/terraform/dev.tfvars` - Dev environment values
- `infrastructure/terraform/prod.tfvars` - Prod environment values
- `infrastructure/terraform/.credentials` - Local credentials (gitignored)
- `.github/workflows/deploy-dev.yml` - Dev deployment workflow
- `.github/workflows/deploy-prod.yml` - Prod deployment workflow
- `.github/workflows/terraform-plan.yml` - Terraform plan on PRs
- `.github/workflows/terraform-apply.yml` - Terraform apply on merge to main

### Phase 1 Success Criteria
- [x] Dev environment deploys successfully from `dev` branch
- [x] Prod environment deploys successfully from `main` branch
- [x] Both environments have separate resource groups
- [x] Azure SQL databases are provisioned in both environments

### 1.6 Terraform CI/CD Automation
- [x] **1.6.1** Add GitHub secrets for Terraform:
  - `ARM_CLIENT_ID`, `ARM_CLIENT_SECRET`, `ARM_SUBSCRIPTION_ID`, `ARM_TENANT_ID` (Azure Service Principal)
  - `TF_VAR_SQL_ADMIN_LOGIN`, `TF_VAR_SQL_ADMIN_PASSWORD`, `TF_VAR_ADMIN_EMAILS`
- [x] **1.6.2** Create `.github/workflows/terraform-plan.yml` - runs `tofu plan` on PRs that modify `infrastructure/terraform/**`
- [x] **1.6.3** Create `.github/workflows/terraform-apply.yml` - runs `tofu apply` on merge to `main` for infrastructure changes
- [x] **1.6.4** Add PR comment with plan output using `actions/github-script`
- [x] **1.6.5** Add manual approval gate for production applies (via GitHub environment protection)
- [x] **1.6.6** Update `deploy-dev.yml` to depend on successful Terraform apply (use `workflow_run`)
- [x] **1.6.7** Update `deploy-prod.yml` to depend on successful Terraform apply
- [x] **1.6.8** Ensure deployment workflows wait for infrastructure changes before deploying app

**Deployment Pipeline Order:**
```
Push to main → Terraform Plan/Apply → Deploy to Dev → Deploy to Prod
```

---

## Phase 2: Database Foundation

**Goal**: Add Azure SQL with EF Core Code First
**Status**: Complete
**Prerequisites**: Phase 1 (infrastructure must exist)

### 2.1 Create Data Project
- [x] **2.1.1** Create new class library: `dotnet new classlib -n AgainstTheSpread.Data -o src/AgainstTheSpread.Data`
- [x] **2.1.2** Add project to solution: `dotnet sln add src/AgainstTheSpread.Data/AgainstTheSpread.Data.csproj`
- [x] **2.1.3** Add NuGet packages: `Microsoft.EntityFrameworkCore.SqlServer` (8.0.x), `Microsoft.EntityFrameworkCore.Design` (8.0.x)
- [x] **2.1.4** Add project reference from Functions to Data project

### 2.2 Entity Classes
- [x] **2.2.1** Create `src/AgainstTheSpread.Data/Entities/User.cs`
  ```csharp
  public class User
  {
      public Guid Id { get; set; }
      public string GoogleSubjectId { get; set; } = string.Empty;
      public string Email { get; set; } = string.Empty;
      public string DisplayName { get; set; } = string.Empty;
      public DateTime CreatedAt { get; set; }
      public DateTime LastLoginAt { get; set; }
      public bool IsActive { get; set; } = true;
      public ICollection<Pick> Picks { get; set; } = new List<Pick>();
  }
  ```
- [x] **2.2.2** Create `src/AgainstTheSpread.Data/Entities/GameEntity.cs` (named GameEntity to avoid conflict with Core.Models.Game)
  ```csharp
  public class GameEntity
  {
      public int Id { get; set; }
      public int Year { get; set; }
      public int Week { get; set; }
      public string Favorite { get; set; } = string.Empty;
      public string Underdog { get; set; } = string.Empty;
      public decimal Line { get; set; }
      public DateTime GameDate { get; set; }
      // Result fields
      public int? FavoriteScore { get; set; }
      public int? UnderdogScore { get; set; }
      public string? SpreadWinner { get; set; }
      public bool? IsPush { get; set; }
      public DateTime? ResultEnteredAt { get; set; }
      public Guid? ResultEnteredBy { get; set; }
      public ICollection<Pick> Picks { get; set; } = new List<Pick>();
      // Computed (not mapped)
      public bool IsLocked => DateTime.UtcNow >= GameDate;
      public bool HasResult => SpreadWinner != null || IsPush == true;
  }
  ```
- [x] **2.2.3** Create `src/AgainstTheSpread.Data/Entities/Pick.cs`
  ```csharp
  public class Pick
  {
      public int Id { get; set; }
      public Guid UserId { get; set; }
      public int GameId { get; set; }
      public string SelectedTeam { get; set; } = string.Empty;
      public DateTime SubmittedAt { get; set; }
      public DateTime? UpdatedAt { get; set; }
      // Denormalized for efficient queries
      public int Year { get; set; }
      public int Week { get; set; }
      // Navigation
      public User? User { get; set; }
      public GameEntity? Game { get; set; }
  }
  ```

### 2.3 Entity Configurations
- [x] **2.3.1** Create `src/AgainstTheSpread.Data/Configurations/UserConfiguration.cs` - indexes on GoogleSubjectId (unique), Email
- [x] **2.3.2** Create `src/AgainstTheSpread.Data/Configurations/GameConfiguration.cs` - composite index on Year+Week, unique on Year+Week+Favorite+Underdog
- [x] **2.3.3** Create `src/AgainstTheSpread.Data/Configurations/PickConfiguration.cs` - unique constraint on UserId+GameId, index on UserId+Year+Week

### 2.4 DbContext
- [x] **2.4.1** Create `src/AgainstTheSpread.Data/AtsDbContext.cs`
  ```csharp
  public class AtsDbContext : DbContext
  {
      public AtsDbContext(DbContextOptions<AtsDbContext> options) : base(options) { }
      public DbSet<User> Users => Set<User>();
      public DbSet<GameEntity> Games => Set<GameEntity>();
      public DbSet<Pick> Picks => Set<Pick>();
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtsDbContext).Assembly);
      }
  }
  ```

### 2.5 Migrations
- [x] **2.5.1** Create initial migration: `dotnet ef migrations add InitialCreate --project src/AgainstTheSpread.Data --startup-project src/AgainstTheSpread.Functions`
- [x] **2.5.2** Review generated migration SQL
- [x] **2.5.3** Apply migration to dev database: `dotnet ef database update --project src/AgainstTheSpread.Data --startup-project src/AgainstTheSpread.Functions`

### 2.6 DI Registration
- [x] **2.6.1** Update `src/AgainstTheSpread.Functions/Program.cs` - add DbContext registration with connection string from environment variable `SqlConnectionString`
- [x] **2.6.2** Update `src/AgainstTheSpread.Functions/local.settings.json` - add `SqlConnectionString` for local development

### 2.7 Verification
- [x] **2.7.1** Build solution successfully: `dotnet build`
- [x] **2.7.2** Run existing tests to ensure no regressions: `dotnet test` (265 tests pass)

### Phase 2 Key Files
- New: `src/AgainstTheSpread.Data/AgainstTheSpread.Data.csproj`
- New: `src/AgainstTheSpread.Data/AtsDbContext.cs`
- New: `src/AgainstTheSpread.Data/Entities/*.cs`
- New: `src/AgainstTheSpread.Data/Configurations/*.cs`
- Modified: `src/AgainstTheSpread.Functions/Program.cs`
- Modified: `src/AgainstTheSpread.Functions/local.settings.json`
- Modified: `AgainstTheSpread.sln`

### Phase 2 Success Criteria
- [x] Data project builds without errors
- [x] Migration applied to dev database
- [x] DbContext can connect and query (verified with InMemory provider tests)
- [x] All existing tests pass (265 tests pass including new entity and DbContext tests)

---

## Phase 3: User Authentication & Pick Submission

**Goal**: Enable authenticated users to submit picks via the app
**Status**: Complete
**Prerequisites**: Phase 2 (database must exist)

### 3.1 Auth Helper Extraction
- [x] **3.1.1** Create `src/AgainstTheSpread.Functions/Helpers/AuthHelper.cs` - extract auth logic from `UploadLinesFunction.cs` (lines 118-213)
- [x] **3.1.2** Create `UserInfo` record: `record UserInfo(string UserId, string Email, string? DisplayName)`
- [x] **3.1.3** Create method: `(bool IsAuthenticated, UserInfo? User, string? Error) ValidateAuth(HttpRequestData req)`
- [x] **3.1.4** Create method: `bool IsAdmin(UserInfo user)` - checks against ADMIN_EMAILS
- [x] **3.1.5** Refactor `UploadLinesFunction.cs` to use new AuthHelper

### 3.2 User Service
- [x] **3.2.1** Create `src/AgainstTheSpread.Data/Interfaces/IUserService.cs` (moved to Data project to avoid circular ref)
- [x] **3.2.2** Create `src/AgainstTheSpread.Data/Services/UserService.cs` implementing IUserService
- [x] **3.2.3** Implement `GetByGoogleSubjectIdAsync(string googleSubjectId)`
- [x] **3.2.4** Implement `GetOrCreateUserAsync(string googleSubjectId, string email, string displayName)`
- [x] **3.2.5** Implement `UpdateLastLoginAsync(Guid userId)`
- [x] **3.2.6** Register UserService in Functions `Program.cs`

### 3.3 Game Service
- [x] **3.3.1** Create `src/AgainstTheSpread.Data/Interfaces/IGameService.cs` (moved to Data project)
- [x] **3.3.2** Create `src/AgainstTheSpread.Data/Services/GameService.cs` implementing IGameService
- [x] **3.3.3** Implement `SyncGamesFromLinesAsync(int year, int week, WeeklyLines lines)` - creates/updates Game records from blob data
- [x] **3.3.4** Implement `GetWeekGamesAsync(int year, int week)` - returns games with lock status
- [x] **3.3.5** Implement `IsGameLockedAsync(int gameId)` - checks GameDate vs current time
- [x] **3.3.6** Register GameService in Functions `Program.cs`

### 3.4 Pick Service
- [x] **3.4.1** Create `src/AgainstTheSpread.Data/Interfaces/IPickService.cs` (moved to Data project)
- [x] **3.4.2** Create `src/AgainstTheSpread.Data/Services/PickService.cs` implementing IPickService
- [x] **3.4.3** Implement `SubmitPicksAsync(Guid userId, int year, int week, List<PickSubmission> picks)` with game locking validation
- [x] **3.4.4** Implement `GetUserPicksAsync(Guid userId, int year, int week)`
- [x] **3.4.5** Implement `GetUserSeasonPicksAsync(Guid userId, int year)`
- [x] **3.4.6** Register PickService in Functions `Program.cs`

### 3.5 New API Endpoints
- [x] **3.5.1** Create `src/AgainstTheSpread.Functions/UserPicksFunction.cs` with:
  - `POST /api/user-picks` - submit/update picks (requires auth)
  - `GET /api/user-picks?year={year}` - get user's season picks (requires auth)
  - `GET /api/user-picks/{week}?year={year}` - get user's week picks (requires auth)
- [x] **3.5.2** Update `staticwebapp.config.json` to protect `/api/user-picks*` routes

### 3.6 Modify Lines Upload to Sync Games
- [x] **3.6.1** Update `UploadLinesFunction.cs` - after blob upload, call `IGameService.SyncGamesFromLinesAsync()`

### 3.7 Blazor Auth State
- [x] **3.7.1** Create `src/AgainstTheSpread.Web/Services/AuthStateService.cs` - manages user auth state client-side
- [x] **3.7.2** Register AuthStateService in `Program.cs`
- [x] **3.7.3** Add methods: `InitializeAsync()`, `IsAuthenticated`, `UserId`, `UserEmail`, `UserName`

### 3.8 Modify Picks.razor
- [x] **3.8.1** Add auth state detection (reuse pattern from Admin.razor)
- [x] **3.8.2** Add dual-mode rendering: authenticated (save to DB) vs unauthenticated (Excel download)
- [x] **3.8.3** Add game lock indicators (badge showing "Locked" when GameDate passed)
- [x] **3.8.4** Add "Login to save picks" prompt for unauthenticated users
- [x] **3.8.5** Modify submit button behavior based on auth state

### Phase 3 Key Files
- New: `src/AgainstTheSpread.Functions/Helpers/AuthHelper.cs` (UserInfo record + ValidateAuth + IsAdmin)
- New: `src/AgainstTheSpread.Data/Interfaces/IUserService.cs` (moved from Core to avoid circular ref)
- New: `src/AgainstTheSpread.Data/Interfaces/IGameService.cs` (moved from Core)
- New: `src/AgainstTheSpread.Data/Interfaces/IPickService.cs` (moved from Core)
- New: `src/AgainstTheSpread.Data/Services/UserService.cs`
- New: `src/AgainstTheSpread.Data/Services/GameService.cs`
- New: `src/AgainstTheSpread.Data/Services/PickService.cs`
- New: `src/AgainstTheSpread.Functions/UserPicksFunction.cs`
- New: `src/AgainstTheSpread.Tests/Helpers/AuthHelperTests.cs` (16 tests)
- New: `src/AgainstTheSpread.Tests/Data/Services/UserServiceTests.cs` (12 tests)
- New: `src/AgainstTheSpread.Tests/Data/Services/GameServiceTests.cs` (14 tests)
- New: `src/AgainstTheSpread.Tests/Data/Services/PickServiceTests.cs` (14 tests)
- New: `src/AgainstTheSpread.Web/Services/AuthStateService.cs`
- New: `src/AgainstTheSpread.Functions/GamesFunction.cs` (returns games with IDs for authenticated picks)
- Modified: `src/AgainstTheSpread.Functions/UploadLinesFunction.cs` (uses AuthHelper + syncs games)
- Modified: `src/AgainstTheSpread.Functions/Program.cs` (registers services)
- Modified: `src/AgainstTheSpread.Web/Pages/Picks.razor` (dual-mode with auth support)
- Modified: `src/AgainstTheSpread.Web/wwwroot/staticwebapp.config.json` (protected routes)

### 3.9 E2E Tests for Phase 3
- [x] **3.9.1** Create `tests/specs/auth-picks.spec.ts` - authenticated pick submission flow
- [x] **3.9.2** Test: User logs in, selects games, submits picks to database
- [x] **3.9.3** Test: User can view their submitted picks after page reload
- [x] **3.9.4** Test: Game lock indicator shows for past games
- [x] **3.9.5** Test: Locked games cannot be selected

### Phase 3 Success Criteria
- [x] Authenticated users can submit picks via the app
- [x] Picks are persisted to database
- [x] Game locking prevents changes after kickoff (server-side rejection)
- [x] Unauthenticated users still get Excel download flow
- [x] Lock status visible in UI
- [x] E2E tests pass for authenticated pick flow

---

## Phase 4: Admin Results Entry

**Goal**: Allow admins to enter game results for scoring
**Status**: Complete
**Prerequisites**: Phase 3 (games must exist in database)

### 4.1 Result Service
- [x] **4.1.1** Create `src/AgainstTheSpread.Data/Interfaces/IResultService.cs`
- [x] **4.1.2** Create `src/AgainstTheSpread.Data/Services/ResultService.cs` implementing IResultService
- [x] **4.1.3** Implement `EnterResultAsync(int gameId, int favoriteScore, int underdogScore, Guid adminUserId)` with spread calculation
- [x] **4.1.4** Implement `BulkEnterResultsAsync(int year, int week, List<GameResultInput> results, Guid adminUserId)`
- [x] **4.1.5** Implement `GetWeekResultsAsync(int year, int week)`
- [x] **4.1.6** Register ResultService in Functions `Program.cs`

### 4.2 Spread Winner Calculation
- [x] **4.2.1** Create helper method in ResultService:
  ```csharp
  (string? Winner, bool IsPush) CalculateSpreadWinner(
      string favorite, string underdog, decimal line,
      int favoriteScore, int underdogScore)
  ```
  Logic: `adjustedFavoriteScore = favoriteScore + line` (line is negative)

### 4.3 New API Endpoints
- [x] **4.3.1** Create `src/AgainstTheSpread.Functions/ResultsFunction.cs` with:
  - `POST /api/results/{week}?year={year}` - submit results (admin only)
  - `GET /api/results/{week}?year={year}` - get results (public)
  - `POST /api/results/game/{gameId}` - single game result (admin only)

### 4.4 Admin.razor Additions
- [x] **4.4.1** Add "Results Entry" section after upload sections
- [x] **4.4.2** Add week/year selector for results
- [x] **4.4.3** Display games for selected week with score inputs
- [x] **4.4.4** Show spread winner badges after results are entered
- [x] **4.4.5** Add submit button for results
- [x] **4.4.6** Results entry integrated with game load functionality

### 4.5 ApiService Extensions
- [x] **4.5.1** Add `SubmitResultsAsync(int week, int year, List<ResultInput> results)` to ApiService
- [x] **4.5.2** Add `GetResultsAsync(int week, int year)` to ApiService

### Phase 4 Key Files
- New: `src/AgainstTheSpread.Data/Interfaces/IResultService.cs`
- New: `src/AgainstTheSpread.Data/Services/ResultService.cs`
- New: `src/AgainstTheSpread.Functions/ResultsFunction.cs`
- New: `src/AgainstTheSpread.Tests/Data/Services/ResultServiceTests.cs` (17 tests)
- Modified: `src/AgainstTheSpread.Web/Pages/Admin.razor`
- Modified: `src/AgainstTheSpread.Web/Services/ApiService.cs`
- Modified: `src/AgainstTheSpread.Functions/Program.cs`

### 4.6 E2E Tests for Phase 4
- [x] **4.6.1** Create `tests/specs/admin-results.spec.ts` - admin results entry flow
- [x] **4.6.2** Test: Admin loads games for a week with scores inputs
- [x] **4.6.3** Test: Admin enters scores and submits results
- [x] **4.6.4** Test: Spread winner badges display correctly after submission
- [x] **4.6.5** Test: Push scenario displays correctly (when adjusted scores equal)
- [x] **4.6.6** Test: Non-admin cannot access results submission

### Phase 4 Success Criteria
- [x] Admin can enter game results via UI
- [x] Spread winner calculated correctly
- [x] Results stored in database
- [x] Single game and bulk result entry supported
- [x] Non-admins cannot submit results (admin check in API)
- [x] E2E tests pass for admin results entry flow

---

## Phase 5: Leaderboard

**Goal**: Display weekly and season standings
**Status**: Complete
**Prerequisites**: Phase 4 (results must be enterable)

### 5.1 Leaderboard Service
- [x] **5.1.1** Create `src/AgainstTheSpread.Data/Interfaces/ILeaderboardService.cs` (interface and DTOs in same file)
- [x] **5.1.2** Create `src/AgainstTheSpread.Data/Services/LeaderboardService.cs` implementing ILeaderboardService
- [x] **5.1.3** Implement `GetWeeklyLeaderboardAsync(int year, int week)` - returns entries sorted by wins
- [x] **5.1.4** Implement `GetSeasonLeaderboardAsync(int year)` - aggregates all weeks
- [x] **5.1.5** Implement `GetUserSeasonHistoryAsync(Guid userId, int year)` - user pick history
- [x] **5.1.6** Register LeaderboardService in Functions `Program.cs`

### 5.2 New API Endpoints
- [x] **5.2.1** Create `src/AgainstTheSpread.Functions/LeaderboardFunction.cs` with:
  - `GET /api/leaderboard/season?year={year}` - season standings
  - `GET /api/leaderboard/week/{week}?year={year}` - weekly standings
  - `GET /api/leaderboard/user/{userId}?year={year}` - user pick history
  - `GET /api/leaderboard/me?year={year}` - authenticated user's own history

### 5.3 ApiService Extensions
- [x] **5.3.1** Add `GetSeasonLeaderboardAsync(int year)` to ApiService
- [x] **5.3.2** Add `GetWeeklyLeaderboardAsync(int year, int week)` to ApiService
- [x] **5.3.3** Add `GetUserSeasonHistoryAsync(Guid userId, int year)` to ApiService
- [x] **5.3.4** Add `GetMySeasonHistoryAsync(int year)` to ApiService

### 5.4 Leaderboard.razor Page
- [x] **5.4.1** Create `src/AgainstTheSpread.Web/Pages/Leaderboard.razor`
- [x] **5.4.2** Add year and view (season/weekly) selectors
- [x] **5.4.3** Display season standings table with ranking badges
- [x] **5.4.4** Display weekly standings with PERFECT week badges
- [x] **5.4.5** Link player names to user history pages

### 5.5 User History and MyPicks Pages
- [x] **5.5.1** Create `src/AgainstTheSpread.Web/Pages/UserHistory.razor` - view any user's history
- [x] **5.5.2** Create `src/AgainstTheSpread.Web/Pages/MyPicks.razor` (requires auth)
- [x] **5.5.3** Display user's pick history by week with collapsible cards
- [x] **5.5.4** Show WIN/LOSS/PUSH badges for each pick
- [x] **5.5.5** Show perfect week indicators

### 5.6 Navigation Updates
- [x] **5.6.1** Add "Leaderboard" link to NavMenu.razor (public)
- [x] **5.6.2** Add "My Picks" link to NavMenu.razor

### 5.7 Testing
- [x] **5.7.1** Create `src/AgainstTheSpread.Tests/Data/Services/LeaderboardServiceTests.cs` (14 tests)

### Phase 5 Key Files
- New: `src/AgainstTheSpread.Data/Interfaces/ILeaderboardService.cs` (includes DTOs)
- New: `src/AgainstTheSpread.Data/Services/LeaderboardService.cs`
- New: `src/AgainstTheSpread.Functions/LeaderboardFunction.cs`
- New: `src/AgainstTheSpread.Web/Pages/Leaderboard.razor`
- New: `src/AgainstTheSpread.Web/Pages/UserHistory.razor`
- New: `src/AgainstTheSpread.Web/Pages/MyPicks.razor`
- New: `src/AgainstTheSpread.Tests/Data/Services/LeaderboardServiceTests.cs`
- Modified: `src/AgainstTheSpread.Web/Services/ApiService.cs`
- Modified: `src/AgainstTheSpread.Web/Layout/NavMenu.razor`
- Modified: `src/AgainstTheSpread.Functions/Program.cs`

### 5.8 E2E Tests for Phase 5
- [x] **5.8.1** Create `tests/specs/leaderboard.spec.ts` - leaderboard display flow
- [x] **5.8.2** Test: Leaderboard page loads and displays season standings
- [x] **5.8.3** Test: Weekly view shows correct week standings
- [x] **5.8.4** Test: User can click player name to view history
- [x] **5.8.5** Test: My Picks page requires authentication
- [x] **5.8.6** Test: My Picks displays user's pick history with WIN/LOSS badges

### Phase 5 Success Criteria
- [x] Leaderboard page displays season standings
- [x] Weekly breakdown shows correct win/loss counts
- [x] Scoring: 1 point per win, 0.5 per push
- [x] My Picks page shows user's history (auth required)
- [x] User history page accessible from leaderboard
- [x] Navigation links work correctly
- [x] 14 unit tests for LeaderboardService all passing
- [x] E2E tests pass for leaderboard and user history flows

---

## Phase 6: Bowl Games

**Goal**: Extend authenticated flow to bowl games with database storage
**Status**: Complete
**Prerequisites**: Phase 5 complete

### 6.1 Database Extensions
- [x] **6.1.1** Create `BowlGameEntity` and `BowlPickEntity` with ConfidencePoints, OutrightWinner fields
- [x] **6.1.2** Create migration `AddBowlGamesAndPicks` for bowl tables
- [x] **6.1.3** Add BowlGames and BowlPicks DbSets to AtsDbContext

### 6.2 Bowl Services
- [x] **6.2.1** Create `IBowlGameService`, `IBowlPickService`, `IBowlLeaderboardService` interfaces
- [x] **6.2.2** Implement BowlGameService with sync and result entry functionality
- [x] **6.2.3** Implement BowlPickService with confidence point uniqueness validation
- [x] **6.2.4** Implement BowlLeaderboardService (confidence-based scoring)

### 6.3 Bowl API Endpoints
- [x] **6.3.1** Create `UserBowlPicksFunction` - authenticated bowl picks submission/retrieval
- [x] **6.3.2** Create `BowlLeaderboardFunction` - bowl leaderboard and user history endpoints
- [x] **6.3.3** Create `BowlResultsFunction` - admin bowl results entry

### 6.4 Unit Tests for Phase 6
- [x] **6.4.1** Create `BowlGameServiceTests.cs` - 16 tests for bowl game service
- [x] **6.4.2** Create `BowlPickServiceTests.cs` - 16 tests for bowl pick service
- [x] **6.4.3** Create `BowlLeaderboardServiceTests.cs` - 16 tests for bowl leaderboard

### Phase 6 Key Files
- New: `src/AgainstTheSpread.Data/Entities/BowlGameEntity.cs`
- New: `src/AgainstTheSpread.Data/Entities/BowlPickEntity.cs`
- New: `src/AgainstTheSpread.Data/Configurations/BowlGameConfiguration.cs`
- New: `src/AgainstTheSpread.Data/Configurations/BowlPickConfiguration.cs`
- New: `src/AgainstTheSpread.Data/Interfaces/IBowlGameService.cs`
- New: `src/AgainstTheSpread.Data/Interfaces/IBowlPickService.cs`
- New: `src/AgainstTheSpread.Data/Interfaces/IBowlLeaderboardService.cs`
- New: `src/AgainstTheSpread.Data/Services/BowlGameService.cs`
- New: `src/AgainstTheSpread.Data/Services/BowlPickService.cs`
- New: `src/AgainstTheSpread.Data/Services/BowlLeaderboardService.cs`
- New: `src/AgainstTheSpread.Functions/UserBowlPicksFunction.cs`
- New: `src/AgainstTheSpread.Functions/BowlLeaderboardFunction.cs`
- New: `src/AgainstTheSpread.Functions/BowlResultsFunction.cs`
- New: `src/AgainstTheSpread.Tests/Data/Services/BowlGameServiceTests.cs`
- New: `src/AgainstTheSpread.Tests/Data/Services/BowlPickServiceTests.cs`
- New: `src/AgainstTheSpread.Tests/Data/Services/BowlLeaderboardServiceTests.cs`
- Modified: `src/AgainstTheSpread.Data/AtsDbContext.cs`
- Modified: `src/AgainstTheSpread.Functions/Program.cs`

### Phase 6 Success Criteria
- [x] Bowl game entities stored in database
- [x] Bowl picks with confidence points stored per user
- [x] Confidence point uniqueness validated (each point value used once per season)
- [x] Bowl leaderboard calculates confidence-based scoring correctly
- [x] Admin can enter bowl game results
- [x] 48 unit tests for bowl services all passing

---

## Phase 7: Sports Data API Integration

**Goal**: Auto-pull spreads and results from external API
**Status**: Complete
**Prerequisites**: Phase 5 complete

### 7.1 Provider Interface
- [x] **7.1.1** Create `src/AgainstTheSpread.Core/Interfaces/ISportsDataProvider.cs`
  - Defines `GetWeeklyGamesAsync`, `GetBowlGamesAsync`, `GetGameResultAsync`, `GetWeeklyResultsAsync`
  - Includes `ExternalGame`, `ExternalBowlGame`, `ExternalGameResult` DTOs

### 7.2 Provider Implementation
- [x] **7.2.1** Selected CollegeFootballData (CFBD) API - free tier with 1,000 requests/month
- [x] **7.2.2** Implemented `CollegeFootballDataProvider.cs` with spread conversion logic
- [x] **7.2.3** Added optional API key configuration via `CFBD_API_KEY` environment variable

### 7.3 Admin Sync Endpoints
- [x] **7.3.1** Created `SportsDataSyncFunction.cs` with admin-only sync endpoints
- [x] **7.3.2** POST `/api/sync/games/{week}` - Sync weekly games from CFBD
- [x] **7.3.3** POST `/api/sync/bowl-games` - Sync bowl games from CFBD
- [x] **7.3.4** GET `/api/sync/status` - Check provider configuration status

### 7.4 Unit Tests
- [x] **7.4.1** Created `CollegeFootballDataProviderTests.cs` with 13 tests for mock HTTP responses

### Phase 7 Key Files
- New: `src/AgainstTheSpread.Core/Interfaces/ISportsDataProvider.cs` (interface + DTOs)
- New: `src/AgainstTheSpread.Core/Services/CollegeFootballDataProvider.cs`
- New: `src/AgainstTheSpread.Functions/SportsDataSyncFunction.cs`
- New: `src/AgainstTheSpread.Tests/Services/CollegeFootballDataProviderTests.cs`
- Modified: `src/AgainstTheSpread.Functions/Program.cs` (registers HttpClient + provider)

### Phase 7 Success Criteria
- [x] ISportsDataProvider interface with DTOs for external data
- [x] CollegeFootballData provider implementation with spread conversion
- [x] Admin-only sync endpoints for weekly and bowl games
- [x] Provider is optional - gracefully handles missing API key
- [x] 13 unit tests for provider with mock HTTP responses

---

## Phase 8: PR Preview Environments & E2E Testing

**Goal**: Deploy preview environments for PRs and run Playwright E2E tests against them
**Status**: Complete
**Prerequisites**: Core functionality complete (Phases 1-5)

### 8.1 PR Preview Deployments
- [x] **8.1.1** Created `pr-preview.yml` workflow for PR preview deployments to `main`
- [x] **8.1.2** Configured SWA staging environments for PR previews (auto-created by SWA)
- [x] **8.1.3** Added dynamic URL construction based on PR number
- [x] **8.1.4** Preview URL posted as PR comment with deployment status

### 8.2 E2E Testing in CI
- [x] **8.2.1** Created `.github/workflows/pr-preview.yml` - deploys preview and runs E2E tests
- [x] **8.2.2** Updated `playwright.config.ts` to use `BASE_URL` environment variable
- [x] **8.2.3** Added PR comment with preview URL and E2E test results using `actions/github-script`
- [x] **8.2.4** Added increased timeouts for CI environment

### 8.3 Deployment Pipeline
- [x] **8.3.1** E2E tests run on all PRs via existing `e2e-tests.yml` (local env)
- [x] **8.3.2** PR preview E2E tests run against deployed preview URL
- [x] **8.3.3** Branch protection rules can require E2E status checks (configure in GitHub settings)
- [x] **8.3.4** Prod deployment gated via GitHub environment protection rules

**Workflow:**
```
PR to main → Deploy to Preview → Run E2E Tests → Comment Results on PR
                                                         ↓
                                         Merge (requires E2E pass) → Deploy to Prod
```

### Phase 8 Key Files
- New: `.github/workflows/pr-preview.yml` (PR preview + E2E tests)
- Modified: `tests/playwright.config.ts` (BASE_URL env var support)
- Existing: `.github/workflows/e2e-tests.yml` (local E2E tests on all PRs)
- Existing: `.github/workflows/deploy-prod.yml` (prod deployment)

### Phase 8 Success Criteria
- [x] PRs automatically get preview environments via `pr-preview.yml`
- [x] Playwright tests run against preview URL with dynamic BASE_URL
- [x] Test results posted as PR comment with status and links
- [x] Production gating available via GitHub branch protection rules

---

## Environment Variables Required

### Both Environments
| Variable | Description |
|----------|-------------|
| `SqlConnectionString` | Azure SQL connection string |
| `AZURE_STORAGE_CONNECTION_STRING` | Blob storage connection |
| `GOOGLE_CLIENT_ID` | OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | OAuth client secret |
| `ADMIN_EMAILS` | Comma-separated admin email list |

### GitHub Secrets
| Secret | Description |
|--------|-------------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV` | Dev SWA deployment token |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_PROD` | Prod SWA deployment token |
| `SQL_ADMIN_LOGIN` | SQL Server admin username |
| `SQL_ADMIN_PASSWORD` | SQL Server admin password |

---

## Testing Strategy

### IMPORTANT: Pre-Commit Testing Requirements

**Before committing or pushing any code changes:**

1. **Run all unit tests locally:** `dotnet test`
2. **Run E2E tests locally:** `cd tests && npm test`
3. **Ensure both pass** before creating commits or pushing to remote

This prevents CI failures and ensures code quality. The CI pipeline will run these same tests, but catching issues locally saves time and keeps the build green.

### Per-Phase Testing
Each phase should include:
1. **Unit tests** for new services (xUnit + Moq + FluentAssertions)
2. **Integration tests** for database operations (in-memory SQLite or real SQL)
3. **E2E tests** for critical user flows (Playwright) - **Required for all new features**

### E2E Test Requirements by Phase

| Phase | E2E Test Coverage Required |
|-------|---------------------------|
| Phase 3 | Auth flow, pick submission (authenticated + unauthenticated), game locking UI |
| Phase 4 | Admin results entry, spread winner display |
| Phase 5 | Leaderboard display, user history, season standings |
| Phase 6 | Bowl picks with confidence points, bowl leaderboard |
| Phase 7 | API sync functionality (if UI-facing) |
| Phase 8 | PR preview E2E integration |

### Key Test Scenarios
- [x] Game locking: server rejects picks after GameDate (covered in auth-picks.spec.ts)
- [x] Admin upload: lines upload via admin UI (covered in full-flow.spec.ts)
- [x] Dual-mode picks: authenticated vs spreadsheet download (covered in full-flow.spec.ts)
- [x] Bowl picks: complete bowl flow with confidence points (covered in bowl-flow.spec.ts)
- [x] Spread calculation: correct winner determination (covered in admin-results.spec.ts)
- [x] Leaderboard accuracy: wins, pushes, percentages (covered in leaderboard.spec.ts)
- [x] Auth flow: login → submit picks → view history (covered in auth-picks.spec.ts + leaderboard.spec.ts)
- [x] Results entry: admin enters scores, winners calculated (covered in admin-results.spec.ts)

---

## Quick Reference: File Locations

### Infrastructure
```
infrastructure/terraform/
├── main.tf                 # All resources
├── variables.tf            # Variable definitions
├── outputs.tf              # Output definitions
├── dev.tfvars              # Dev environment values
├── prod.tfvars             # Prod environment values
└── bootstrap-state.sh      # State storage setup script
.github/workflows/
├── deploy-dev.yml          # Dev deployment (dev branch)
└── deploy-prod.yml         # Prod deployment (main branch)

# Usage:
#   cd infrastructure/terraform
#   terraform init -backend-config="key=dev.terraform.tfstate"
#   terraform plan -var-file="dev.tfvars" -var="sql_admin_login=xxx" -var="sql_admin_password=xxx"
```

### Backend
```
src/AgainstTheSpread.Data/
├── Entities/               # EF Core entities
├── Configurations/         # Entity configurations
├── Services/               # Service implementations
└── AtsDbContext.cs         # DbContext

src/AgainstTheSpread.Core/
├── Interfaces/             # Service interfaces
└── Models/                 # DTOs, records

src/AgainstTheSpread.Functions/
├── Helpers/                # Auth helper
└── *Function.cs            # API endpoints
```

### Frontend
```
src/AgainstTheSpread.Web/
├── Pages/                  # Razor pages
├── Services/               # ApiService, AuthStateService
├── Layout/                 # NavMenu
└── wwwroot/                # Static config
```

### Running Tests (REQUIRED before commits)

```bash
# 1. Run all unit tests
dotnet test

# 2. Start E2E environment (in separate terminal)
cd tests
npm run e2e:env

# 3. Run E2E tests (in another terminal, after env is ready)
cd tests
npm test

# 4. Run specific E2E test file
npm test -- --grep "Week 11"

# 5. Run E2E tests with UI (for debugging)
npm run test:ui
```

**E2E Environment Ports:**
- SWA CLI: http://localhost:4280 (frontend + API proxy)
- Azure Functions: http://localhost:7071 (backend API)
- Azurite: http://localhost:10000 (blob storage emulator)
