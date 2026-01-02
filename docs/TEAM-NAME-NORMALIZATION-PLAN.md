# Plan: Team Name Normalization System

**Status:** Complete
**Started:** 2026-01-01
**Completed:** 2026-01-01

## Progress Tracker

- [x] Plan approved
- [x] Phase 1: Database schema (entity, migration)
  - [x] Create TeamAliasEntity
  - [x] Add DbSet to AtsDbContext
  - [x] Create EF migration with seed data (~150 teams)
- [x] Phase 2: TeamNameNormalizer service
  - [x] Create ITeamNameNormalizer interface
  - [x] Implement TeamNameNormalizer service with caching
- [x] Phase 3: Seed initial aliases from logo mapping
- [x] Phase 4: Integrate into CFBD sync (via GameService)
- [x] Phase 5: Integrate into Excel parsing (via GameService/BowlGameService)
- [x] Phase 6: Duplicate game detection (in sync methods)
- [x] Phase 7: Update DI registration
- [x] Phase 8: Unit tests (14 new tests, 427 total passing)
- [x] Test locally (all tests pass)
- [x] E2E tests pass (18 passed, 1 skipped)
- [ ] Deploy and verify (pending - code is ready to commit)

---

## Goal

Normalize team names at data entry points (CFBD sync, Excel uploads) to prevent issues with inconsistent naming (e.g., "Southern Florida" vs "USF", "Florida State" vs "FSU"). Store aliases in the database for future admin UI management.

## Problem Statement

- Team names come from multiple sources (CFBD API, Excel uploads) with no normalization
- Pick validation uses EXACT string matching - mismatched names cause failures
- Leaderboard calculations use EXACT string matching - affects scoring accuracy
- Same team with different names could create duplicate games

## Design Decisions (Per User Requirements)

1. **Store aliases in database** - enables future admin UI for managing aliases
2. **Pass through unknown teams with warning** - don't reject, but log for review
3. **Prevent duplicate games** - detect same game offered twice with different team names
4. **No data migration needed** - will start fresh for 2026 season

---

## Phase 1: Database Schema for Team Aliases

### 1.1 Create TeamAlias Entity

**File:** `src/AgainstTheSpread.Data/Entities/TeamAliasEntity.cs`

```csharp
public class TeamAliasEntity
{
    public int Id { get; set; }
    public string Alias { get; set; } = string.Empty;      // e.g., "USF", "South Florida"
    public string CanonicalName { get; set; } = string.Empty; // e.g., "South Florida"
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 1.2 Add DbSet to AtsDbContext

**File:** `src/AgainstTheSpread.Data/AtsDbContext.cs`

### 1.3 Create EF Migration

- Add unique index on `Alias` (case-insensitive)
- Add index on `CanonicalName`

---

## Phase 2: Team Name Normalizer Service

### 2.1 Create Interface

**File:** `src/AgainstTheSpread.Data/Interfaces/ITeamNameNormalizer.cs`

```csharp
public interface ITeamNameNormalizer
{
    Task<string> NormalizeAsync(string teamName, CancellationToken ct = default);
    Task<Dictionary<string, string>> NormalizeBatchAsync(IEnumerable<string> teamNames, CancellationToken ct = default);
    Task<bool> AreTeamsEqual(string teamName1, string teamName2, CancellationToken ct = default);
    Task RefreshCacheAsync(CancellationToken ct = default);
}
```

### 2.2 Implement Service

**File:** `src/AgainstTheSpread.Data/Services/TeamNameNormalizer.cs`

- Lazy load alias mappings from database into memory cache
- Case-insensitive lookups
- Log warnings for unknown teams (not in any mapping)
- Return original name if no mapping found

---

## Phase 3: Seed Initial Aliases from Logo Mapping

### 3.1 Create Seed Data Helper

**File:** `src/AgainstTheSpread.Data/Seeds/TeamAliasSeedData.cs`

- Parse existing `/wwwroot/team-logo-mapping.json`
- Group by ESPN logo ID to find canonical names
- Insert into TeamAliases table (~286 entries)

### 3.2 Create EF Migration with Seed Data

---

## Phase 4: Integrate into CFBD Sync

### 4.1 Update CollegeFootballDataProvider

**File:** `src/AgainstTheSpread.Core/Services/CollegeFootballDataProvider.cs`

- Inject `ITeamNameNormalizer`
- Normalize team names from API response before returning
- Log warnings for unknown teams

---

## Phase 5: Integrate into Excel Parsing

### 5.1 Update ExcelService

**File:** `src/AgainstTheSpread.Core/Services/ExcelService.cs`

- Inject `ITeamNameNormalizer`
- Normalize favorite/underdog names after parsing

### 5.2 Update BowlExcelService

**File:** `src/AgainstTheSpread.Core/Services/BowlExcelService.cs`

- Same pattern as ExcelService

---

## Phase 6: Duplicate Game Detection

### 6.1 Update GameService.SyncGamesAsync

**File:** `src/AgainstTheSpread.Data/Services/GameService.cs`

- After normalization, check for duplicate games (same teams, same week)
- Log warning and skip duplicate

### 6.2 Update BowlGameService.SyncBowlGamesAsync

**File:** `src/AgainstTheSpread.Data/Services/BowlGameService.cs`

- Same duplicate detection pattern

---

## Phase 7: Update DI Registration

**File:** `src/AgainstTheSpread.Functions/Program.cs`

```csharp
services.AddScoped<ITeamNameNormalizer, TeamNameNormalizer>();
```

---

## Phase 8: Unit Tests

### Files to Create/Update:

- `src/AgainstTheSpread.Tests/Data/Services/TeamNameNormalizerTests.cs`
- Update `GameServiceTests.cs` for duplicate detection
- Update `CollegeFootballDataProviderTests.cs` for normalization

---

## Files to Create

| File | Description |
|------|-------------|
| `src/AgainstTheSpread.Data/Entities/TeamAliasEntity.cs` | Team alias database entity |
| `src/AgainstTheSpread.Data/Interfaces/ITeamNameNormalizer.cs` | Normalizer interface |
| `src/AgainstTheSpread.Data/Services/TeamNameNormalizer.cs` | Normalizer implementation |
| `src/AgainstTheSpread.Data/Seeds/TeamAliasSeedData.cs` | Seed data from logo mapping |
| `src/AgainstTheSpread.Tests/Data/Services/TeamNameNormalizerTests.cs` | Unit tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/AgainstTheSpread.Data/AtsDbContext.cs` | Add TeamAliases DbSet |
| `src/AgainstTheSpread.Core/Services/CollegeFootballDataProvider.cs` | Add normalization |
| `src/AgainstTheSpread.Core/Services/ExcelService.cs` | Add normalization |
| `src/AgainstTheSpread.Core/Services/BowlExcelService.cs` | Add normalization |
| `src/AgainstTheSpread.Data/Services/GameService.cs` | Add duplicate detection |
| `src/AgainstTheSpread.Data/Services/BowlGameService.cs` | Add duplicate detection |
| `src/AgainstTheSpread.Functions/Program.cs` | Register normalizer service |

---

## Key Behaviors

### Unknown Team Handling

```
Input: "Some New Team Name"
→ Log: "Warning: Unknown team 'Some New Team Name' - no alias mapping found"
→ Output: "Some New Team Name" (passed through unchanged)
```

### Duplicate Game Detection

```
Game 1: Favorite="South Florida", Underdog="UCF"
Game 2: Favorite="USF", Underdog="UCF"
→ After normalization: Both become Favorite="South Florida", Underdog="UCF"
→ Log: "Warning: Duplicate game detected - South Florida vs UCF"
→ Game 2 skipped
```

### Alias Lookup (Case-Insensitive)

```
Input: "usf" → Output: "South Florida"
Input: "USF" → Output: "South Florida"
Input: "South Florida" → Output: "South Florida"
```

---

## Future Admin UI (Not in This Plan)

The database schema enables a future admin UI to:

- View all team aliases
- Add new aliases for unknown teams
- Edit canonical names
- Merge teams (if duplicate canonical names created)
