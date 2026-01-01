# EF Migrations Deployment Plan

## Problem Statement

The prod database is missing the `Games` table (and likely other tables) because Entity Framework migrations are not being run as part of the deployment process. Currently, migrations must be run manually, which:
- Is error-prone
- Can be forgotten
- Creates environment drift between dev and prod

## Current State Analysis

### Findings

| Question | Answer |
|----------|--------|
| Where are EF migrations? | `src/AgainstTheSpread.Data/Migrations/` |
| What migrations exist? | `InitialCreate`, `AddBowlGamesAndPicks` |
| DbContext configured? | Yes - `AtsDbContext.cs` with `AtsDbContextFactory.cs` |
| Dev database state | Has tables (sync works) |
| Prod database state | Missing `Games` table (sync fails) |

### Current Deployment Flow (deploy.yml)
```
Push to main
    → unit-tests
    → deploy-dev
    → wait-for-dev
    → e2e-tests
    → deploy-prod
```

**Missing Step:** Database migrations are never applied.

---

## Decisions Made

| Question | Decision |
|----------|----------|
| Migration approach? | **EF Bundles** - self-contained, transaction support, no SDK needed |
| Manual fix urgency? | Not urgent - no users yet |
| Connection string secrets? | Construct from existing `TF_VAR_SQL_ADMIN_LOGIN` + `TF_VAR_SQL_ADMIN_PASSWORD` |
| Which workflow to modify? | `deploy.yml` (single unified workflow) |
| Database backup strategy? | Use Azure SQL PITR (point-in-time restore) - no explicit BACPAC exports |
| Test locally first? | Yes - test EF bundle against local database before CI integration |
| Cross-platform bundle? | Build with `--self-contained -r linux-x64` for GitHub Actions runners |

### Infrastructure Details

| Environment | SQL Server FQDN | Database Name |
|-------------|-----------------|---------------|
| Dev | `sql-dev-cus-atsv2.database.windows.net` | `sqldb-dev-cus-atsv2` |
| Prod | `sql-prod-cus-atsv2.database.windows.net` | `sqldb-prod-cus-atsv2` |

---

## Chosen Approach: EF Migration Bundles

EF Migration Bundles were introduced in **EF Core 6.0 (November 2021)** specifically for production CI/CD scenarios.

### Why Bundles Over Alternatives?

| Approach | Production Ready | Requires SDK | Transaction Support | Our Choice |
|----------|-----------------|--------------|---------------------|------------|
| `dotnet ef database update` | No | Yes | No | ❌ |
| EF Bundle | Yes | No | Yes | ✅ |
| SQL Script (`--idempotent`) | Yes | No | Depends | ❌ |
| Startup migration | No | N/A | No | ❌ |

**Why not `dotnet ef database update`?**
- Requires .NET SDK on target (Azure SWA doesn't have it)
- Requires source code access (security risk)
- No transaction support (partial migrations can occur)

**Why not SQL scripts?**
- More complex setup (need sqlcmd tooling)
- Transaction handling depends on executor
- Bundles are simpler for our use case

**Why not startup migration?**
- Race conditions with multiple instances
- Long startup times
- No rollback capability

### Bundle Benefits
1. **Self-contained:** No SDK or source code needed at runtime
2. **Transaction support:** Built-in, atomic migrations
3. **DevOps-friendly:** Single executable, easy to integrate
4. **Idempotent:** Safe to run multiple times
5. **Auditable:** Bundle version matches code version

---

## Implementation Plan

### Phase 1: Local Testing

**Goal:** Verify EF bundle builds and runs correctly before CI/CD integration.

#### Step 1.1: Install EF Tools
```bash
dotnet tool install --global dotnet-ef
# or update if already installed
dotnet tool update --global dotnet-ef
```

#### Step 1.2: Build EF Bundle for Local Architecture
```bash
cd /Users/Ben.Grossman/Code/against-the-spread-v2

dotnet ef migrations bundle \
  --project src/AgainstTheSpread.Data \
  --startup-project src/AgainstTheSpread.Functions \
  --configuration Release \
  --output ./efbundle \
  --self-contained \
  -r osx-arm64
# Use osx-x64 for Intel Mac
```

#### Step 1.3: Start Local SQL Server
```bash
# Start SQL Server in Docker
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourStrong!Passw0rd' \
  -p 1433:1433 --name sql-local -d mcr.microsoft.com/mssql/server:2022-latest

# Wait for it to be ready (about 20 seconds)
sleep 20

# Verify it's running
docker ps | grep sql-local
```

#### Step 1.4: Run Bundle Against Local Database
```bash
./efbundle --connection "Server=localhost,1433;Database=AtsLocal;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
```

**Expected output:** Migration applied successfully, showing each migration name.

#### Step 1.5: Verify Tables Created
```bash
# Using sqlcmd (if installed)
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -d AtsLocal \
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"

# Or using Docker exec
docker exec -it sql-local /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Passw0rd' -d AtsLocal -C \
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
```

**Expected tables:** `Games`, `BowlGames`, `BowlPicks`, `__EFMigrationsHistory`

#### Step 1.6: Cleanup
```bash
docker stop sql-local && docker rm sql-local
rm ./efbundle
```

---

### Phase 2: Update deploy.yml

**Goal:** Add migration jobs to the CI/CD pipeline.

#### Potential Issue: Azure SQL Firewall

The current Terraform has `AllowAzureServices` firewall rule (0.0.0.0 to 0.0.0.0), which allows Azure services to connect. However, **GitHub Actions runners may not be covered by this rule**.

**Options if firewall blocks connection:**
1. **Option A (Recommended):** Use Azure Login action + temporary firewall rule
2. **Option B:** Add GitHub Actions IP ranges to firewall (complex, IPs change)
3. **Option C:** Test first - it might work with existing rule

We'll implement Option A to be safe.

#### Changes to deploy.yml

**Add to `env` section:**
```yaml
env:
  DOTNET_VERSION: '8.0.x'
  NODE_VERSION: '20'
  DEV_URL: 'https://blue-smoke-0b4410710.2.azurestaticapps.net'
  PROD_URL: 'https://ashy-pebble-07e5d0b10.2.azurestaticapps.net'
  # NEW: SQL Server details for migrations
  DEV_SQL_SERVER: sql-dev-cus-atsv2
  DEV_SQL_DATABASE: sqldb-dev-cus-atsv2
  DEV_RESOURCE_GROUP: rg-dev-cus-atsv2
  PROD_SQL_SERVER: sql-prod-cus-atsv2
  PROD_SQL_DATABASE: sqldb-prod-cus-atsv2
  PROD_RESOURCE_GROUP: rg-prod-cus-atsv2
```

**Add to end of `unit-tests` job (after existing steps):**
```yaml
      # NEW: Build EF Migrations Bundle
      - name: Install EF Core tools
        run: dotnet tool install --global dotnet-ef

      - name: Build Migrations Bundle
        run: |
          dotnet ef migrations bundle \
            --project src/AgainstTheSpread.Data \
            --startup-project src/AgainstTheSpread.Functions \
            --configuration Release \
            --output ./efbundle \
            --self-contained \
            -r linux-x64 \
            --force

      - name: Upload Migrations Bundle
        uses: actions/upload-artifact@v4
        with:
          name: ef-migrations
          path: ./efbundle
          retention-days: 1
```

**Add NEW job `migrate-dev` after `unit-tests`:**
```yaml
  # NEW JOB: Migrate Dev Database
  migrate-dev:
    name: Migrate Dev DB
    needs: unit-tests
    runs-on: ubuntu-latest
    steps:
      - name: Download Migrations Bundle
        uses: actions/download-artifact@v4
        with:
          name: ef-migrations

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: '{"clientId":"${{ secrets.ARM_CLIENT_ID }}","clientSecret":"${{ secrets.ARM_CLIENT_SECRET }}","subscriptionId":"${{ secrets.ARM_SUBSCRIPTION_ID }}","tenantId":"${{ secrets.ARM_TENANT_ID }}"}'

      - name: Get Runner IP
        id: ip
        uses: haythem/public-ip@v1.3

      - name: Add Firewall Rule
        run: |
          az sql server firewall-rule create \
            --resource-group ${{ env.DEV_RESOURCE_GROUP }} \
            --server ${{ env.DEV_SQL_SERVER }} \
            --name GitHubActions-${{ github.run_id }} \
            --start-ip-address ${{ steps.ip.outputs.ipv4 }} \
            --end-ip-address ${{ steps.ip.outputs.ipv4 }}
          echo "Waiting 30s for firewall rule to propagate..."
          sleep 30

      - name: Apply Migrations to Dev
        run: |
          chmod +x ./efbundle
          ./efbundle --connection "Server=tcp:${{ env.DEV_SQL_SERVER }}.database.windows.net,1433;Initial Catalog=${{ env.DEV_SQL_DATABASE }};Persist Security Info=False;User ID=${{ secrets.TF_VAR_SQL_ADMIN_LOGIN }};Password=${{ secrets.TF_VAR_SQL_ADMIN_PASSWORD }};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        timeout-minutes: 5

      - name: Remove Firewall Rule
        if: always()
        run: |
          az sql server firewall-rule delete \
            --resource-group ${{ env.DEV_RESOURCE_GROUP }} \
            --server ${{ env.DEV_SQL_SERVER }} \
            --name GitHubActions-${{ github.run_id }} \
            --yes
```

**Modify `deploy-dev` job:**
```yaml
  deploy-dev:
    name: Deploy to Dev
    needs: migrate-dev  # CHANGED from: needs: unit-tests
    # ... rest unchanged ...
```

**Add NEW job `migrate-prod` after `e2e-tests`:**
```yaml
  # NEW JOB: Migrate Prod Database
  migrate-prod:
    name: Migrate Prod DB
    needs: e2e-tests
    runs-on: ubuntu-latest
    steps:
      - name: Download Migrations Bundle
        uses: actions/download-artifact@v4
        with:
          name: ef-migrations

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: '{"clientId":"${{ secrets.ARM_CLIENT_ID }}","clientSecret":"${{ secrets.ARM_CLIENT_SECRET }}","subscriptionId":"${{ secrets.ARM_SUBSCRIPTION_ID }}","tenantId":"${{ secrets.ARM_TENANT_ID }}"}'

      - name: Get Runner IP
        id: ip
        uses: haythem/public-ip@v1.3

      - name: Add Firewall Rule
        run: |
          az sql server firewall-rule create \
            --resource-group ${{ env.PROD_RESOURCE_GROUP }} \
            --server ${{ env.PROD_SQL_SERVER }} \
            --name GitHubActions-${{ github.run_id }} \
            --start-ip-address ${{ steps.ip.outputs.ipv4 }} \
            --end-ip-address ${{ steps.ip.outputs.ipv4 }}
          echo "Waiting 30s for firewall rule to propagate..."
          sleep 30

      - name: Apply Migrations to Prod
        run: |
          chmod +x ./efbundle
          ./efbundle --connection "Server=tcp:${{ env.PROD_SQL_SERVER }}.database.windows.net,1433;Initial Catalog=${{ env.PROD_SQL_DATABASE }};Persist Security Info=False;User ID=${{ secrets.TF_VAR_SQL_ADMIN_LOGIN }};Password=${{ secrets.TF_VAR_SQL_ADMIN_PASSWORD }};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        timeout-minutes: 5

      - name: Remove Firewall Rule
        if: always()
        run: |
          az sql server firewall-rule delete \
            --resource-group ${{ env.PROD_RESOURCE_GROUP }} \
            --server ${{ env.PROD_SQL_SERVER }} \
            --name GitHubActions-${{ github.run_id }} \
            --yes
```

**Modify `deploy-prod` job:**
```yaml
  deploy-prod:
    name: Deploy to Prod
    needs: migrate-prod  # CHANGED from: needs: e2e-tests
    # ... rest unchanged ...
```

---

### Updated Pipeline Flow

```
unit-tests (+ build EF bundle)
        ↓
migrate-dev ──(fail)──→ STOP
   │  └── Azure login, add firewall rule, run bundle, remove firewall rule
   ↓ (success)
deploy-dev
        ↓
wait-for-dev
        ↓
e2e-tests ──(fail)──→ STOP
        ↓ (success)
migrate-prod ──(fail)──→ STOP
   │  └── Azure login, add firewall rule, run bundle, remove firewall rule
   ↓ (success)
deploy-prod
```

### Why This Order?
- Migration BEFORE deploy ensures database schema matches code
- If migration fails, old code continues running (safe)
- If migration succeeds but deploy fails, database is forward-compatible
- E2E tests validate dev migration + deploy before touching prod
- Firewall rules are temporary and cleaned up even on failure (`if: always()`)

---

### Phase 3: Verification & Cleanup

#### Step 3.1: Test the Pipeline
1. Make a small change (e.g., update a comment in a migration file)
2. Push to trigger the workflow
3. Monitor GitHub Actions for:
   - EF bundle builds successfully
   - Firewall rule creation works
   - Migration applies to dev
   - E2E tests pass
   - Migration applies to prod
   - Deployments succeed

#### Step 3.2: Verify Database State
After successful run, verify tables exist in both databases:
```bash
# Dev
az sql query --resource-group rg-dev-cus-atsv2 \
  --name sql-dev-cus-atsv2 \
  --database sqldb-dev-cus-atsv2 \
  --query "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"

# Prod
az sql query --resource-group rg-prod-cus-atsv2 \
  --name sql-prod-cus-atsv2 \
  --database sqldb-prod-cus-atsv2 \
  --query "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
```

#### Step 3.3: Cleanup
- Confirm no stale firewall rules left behind
- Delete `efbundle` from local if still present
- Update this document status to "Implemented"

---

## Rollback Strategy

Since we're using Azure SQL PITR (point-in-time restore):

1. **If migration breaks prod:**
   - Restore database to point-in-time before migration via Azure Portal
   - Redeploy previous version of code from Git

2. **For code-level rollback:**
   - Revert the commit that added the problematic migration
   - Create a new "down" migration if needed
   - Deploy the rollback

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Firewall blocks GitHub Actions | Medium | High | Using temporary firewall rules with Azure Login |
| Migration fails mid-way | Low | High | EF bundles have transaction support; PITR for recovery |
| Bundle doesn't run on Linux | Low | Medium | Testing locally first; `--self-contained -r linux-x64` |
| Stale firewall rules accumulate | Low | Low | `if: always()` ensures cleanup; unique rule names per run |
| Connection string exposed in logs | Medium | High | Using `${{ secrets.* }}` syntax; GitHub masks secrets |

---

## Checklist

### Phase 1: Local Testing
- [ ] Step 1.1: Install EF tools (`dotnet tool install --global dotnet-ef`)
- [ ] Step 1.2: Build EF bundle for osx-arm64
- [ ] Step 1.3: Start local SQL Server in Docker
- [ ] Step 1.4: Run bundle against local database
- [ ] Step 1.5: Verify expected tables created (`Games`, `BowlGames`, `BowlPicks`, `__EFMigrationsHistory`)
- [ ] Step 1.6: Cleanup (stop Docker, delete bundle)

### Phase 2: CI/CD Integration
- [ ] Add SQL environment variables to `env` section
- [ ] Add EF bundle build steps to `unit-tests` job
- [ ] Add `migrate-dev` job with Azure Login + firewall handling
- [ ] Update `deploy-dev` to depend on `migrate-dev`
- [ ] Add `migrate-prod` job with Azure Login + firewall handling
- [ ] Update `deploy-prod` to depend on `migrate-prod`
- [ ] Commit and push changes
- [ ] Monitor first pipeline run

### Phase 3: Verification & Cleanup
- [ ] Verify dev database has all tables
- [ ] Verify prod database has all tables
- [ ] Confirm no stale firewall rules exist
- [ ] Update this document status to "Implemented"

---

*Created: January 1, 2025*
*Updated: January 1, 2025*
*Status: Ready for Implementation (EF Bundle approach confirmed)*
