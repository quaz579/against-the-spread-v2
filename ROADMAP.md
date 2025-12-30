# Against The Spread - Major Enhancement Roadmap

## Progress Tracking

| Phase | Status | Progress | Description |
|-------|--------|----------|-------------|
| 1 | Complete | 19/19 | Infrastructure Separation |
| 2 | Complete | 18/18 | Database Foundation |
| 3 | In Progress | 22/27 | User Authentication & Pick Submission (E2E tests pending) |
| 4 | In Progress | 14/20 | Admin Results Entry (E2E tests pending) |
| 5 | In Progress | 16/22 | Leaderboard (E2E tests pending) |
| 6 | Not Started | 0/11 | Bowl Games (Future) |
| 7 | Not Started | 0/6 | Sports Data API (Future) |
| 8 | Not Started | 0/12 | PR Preview Environments & E2E Testing (Future) |

**Overall Progress**: 89/135 tasks completed

**Note**: Phases 3-5 have backend/UI complete but E2E test coverage pending.

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
**Status**: In Progress (Backend Complete, UI Pending)
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
- [ ] **3.9.1** Create `tests/specs/auth-picks.spec.ts` - authenticated pick submission flow
- [ ] **3.9.2** Test: User logs in, selects games, submits picks to database
- [ ] **3.9.3** Test: User can view their submitted picks after page reload
- [ ] **3.9.4** Test: Game lock indicator shows for past games
- [ ] **3.9.5** Test: Locked games cannot be selected

### Phase 3 Success Criteria
- [x] Authenticated users can submit picks via the app
- [x] Picks are persisted to database
- [x] Game locking prevents changes after kickoff (server-side rejection)
- [x] Unauthenticated users still get Excel download flow
- [x] Lock status visible in UI
- [ ] E2E tests pass for authenticated pick flow

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
- [ ] **4.6.1** Create `tests/specs/admin-results.spec.ts` - admin results entry flow
- [ ] **4.6.2** Test: Admin loads games for a week with scores inputs
- [ ] **4.6.3** Test: Admin enters scores and submits results
- [ ] **4.6.4** Test: Spread winner badges display correctly after submission
- [ ] **4.6.5** Test: Push scenario displays correctly (when adjusted scores equal)
- [ ] **4.6.6** Test: Non-admin cannot access results submission

### Phase 4 Success Criteria
- [x] Admin can enter game results via UI
- [x] Spread winner calculated correctly
- [x] Results stored in database
- [x] Single game and bulk result entry supported
- [x] Non-admins cannot submit results (admin check in API)
- [ ] E2E tests pass for admin results entry flow

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
- [ ] **5.8.1** Create `tests/specs/leaderboard.spec.ts` - leaderboard display flow
- [ ] **5.8.2** Test: Leaderboard page loads and displays season standings
- [ ] **5.8.3** Test: Weekly view shows correct week standings
- [ ] **5.8.4** Test: User can click player name to view history
- [ ] **5.8.5** Test: My Picks page requires authentication
- [ ] **5.8.6** Test: My Picks displays user's pick history with WIN/LOSS badges

### Phase 5 Success Criteria
- [x] Leaderboard page displays season standings
- [x] Weekly breakdown shows correct win/loss counts
- [x] Scoring: 1 point per win, 0.5 per push
- [x] My Picks page shows user's history (auth required)
- [x] User history page accessible from leaderboard
- [x] Navigation links work correctly
- [x] 14 unit tests for LeaderboardService all passing
- [ ] E2E tests pass for leaderboard and user history flows

---

## Phase 6: Bowl Games (Future)

**Goal**: Extend authenticated flow to bowl games
**Status**: Not Started
**Prerequisites**: Phase 5 complete

### 6.1 Database Extensions
- [ ] **6.1.1** Create `BowlPick` entity with ConfidencePoints, OutrightWinner fields
- [ ] **6.1.2** Create migration for bowl tables
- [ ] **6.1.3** Add BowlPick to DbContext

### 6.2 Bowl Services
- [ ] **6.2.1** Create `IBowlPickService` interface
- [ ] **6.2.2** Implement BowlPickService with confidence point validation
- [ ] **6.2.3** Implement bowl leaderboard (confidence-based scoring)

### 6.3 Bowl API Endpoints
- [ ] **6.3.1** Create authenticated bowl picks endpoints
- [ ] **6.3.2** Create bowl leaderboard endpoint

### 6.4 E2E Tests for Phase 6
- [ ] **6.4.1** Update `tests/specs/bowl-flow.spec.ts` for authenticated bowl picks
- [ ] **6.4.2** Test: User logs in and submits bowl picks with confidence points
- [ ] **6.4.3** Test: Bowl leaderboard displays correctly with confidence-based scoring

### Phase 6 Key Files
- New: `src/AgainstTheSpread.Data/Entities/BowlPick.cs`
- New: `src/AgainstTheSpread.Core/Interfaces/IBowlPickService.cs`
- New: `src/AgainstTheSpread.Data/Services/BowlPickService.cs`

---

## Phase 7: Sports Data API Integration (Future)

**Goal**: Auto-pull spreads and results from external API
**Status**: Not Started
**Prerequisites**: Phase 5 complete

### 7.1 Provider Interface
- [ ] **7.1.1** Create `src/AgainstTheSpread.Core/Interfaces/ISportsDataProvider.cs`
  ```csharp
  public interface ISportsDataProvider
  {
      string ProviderName { get; }
      Task<List<ExternalGame>> GetWeeklyGamesAsync(int week, int year);
      Task<ExternalGameResult?> GetGameResultAsync(string gameId);
  }
  ```

### 7.2 Provider Implementation
- [ ] **7.2.1** Research and select API provider (The Odds API, ESPN, Sportradar)
- [ ] **7.2.2** Implement concrete provider class
- [ ] **7.2.3** Add configuration for API keys

### 7.3 Admin Integration
- [ ] **7.3.1** Add "Sync from API" button to Admin page
- [ ] **7.3.2** Add auto-sync toggle for results

### Phase 7 Key Files
- New: `src/AgainstTheSpread.Core/Interfaces/ISportsDataProvider.cs`
- New: `src/AgainstTheSpread.Core/Models/ExternalGame.cs`
- New: `src/AgainstTheSpread.Core/Models/ExternalGameResult.cs`
- New: Provider implementation (TBD based on selection)

---

## Phase 8: PR Preview Environments & E2E Testing (Future)

**Goal**: Deploy preview environments for PRs and run Playwright E2E tests against them
**Status**: Not Started
**Prerequisites**: Core functionality complete (Phases 1-5)

### 8.1 PR Preview Deployments
- [ ] **8.1.1** Update `deploy-dev.yml` to deploy preview environments for PRs to `main`
- [ ] **8.1.2** Configure SWA staging environments for PR previews (auto-created by SWA)
- [ ] **8.1.3** Add dynamic URL detection (parse SWA deployment output for preview URL)

### 8.2 E2E Testing in CI
- [ ] **8.2.1** Create `.github/workflows/pr-e2e-tests.yml` - runs Playwright tests against PR preview URL
- [ ] **8.2.2** Configure Playwright to use dynamic base URL from environment
- [ ] **8.2.3** Add PR comment with preview URL and E2E test results using `actions/github-script`

### 8.3 Deployment Pipeline
- [ ] **8.3.1** Update `deploy-prod.yml` to trigger only after successful dev/preview deployment
- [ ] **8.3.2** Add deployment status checks as required for merge

**Workflow vision:**
```
PR to main → Deploy to Dev Preview → Run Playwright E2E → Report results on PR
                                                              ↓
                                              Merge to main → Deploy to Prod
```

### Phase 8 Key Files
- Modified: `.github/workflows/deploy-dev.yml`
- Modified: `.github/workflows/deploy-prod.yml`
- New: `.github/workflows/pr-e2e-tests.yml`
- Modified: `tests/playwright.config.ts`

### Phase 8 Success Criteria
- [ ] PRs automatically get preview environments
- [ ] Playwright tests run against preview URL
- [ ] Test results posted as PR comment
- [ ] Prod deployment gated on successful E2E

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
- [x] Game locking: server rejects picks after GameDate (covered in existing E2E)
- [x] Admin upload: lines upload via admin UI (covered in full-flow.spec.ts)
- [x] Dual-mode picks: authenticated vs spreadsheet download (covered in full-flow.spec.ts)
- [x] Bowl picks: complete bowl flow with confidence points (covered in bowl-flow.spec.ts)
- [ ] Spread calculation: correct winner determination (needs E2E coverage)
- [ ] Leaderboard accuracy: wins, pushes, percentages (needs E2E coverage)
- [ ] Auth flow: login → submit picks → view history (needs E2E coverage)
- [ ] Results entry: admin enters scores, winners calculated (needs E2E coverage)

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
