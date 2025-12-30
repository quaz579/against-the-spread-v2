# Deployment Notes - Against The Spread V2

**Last Updated**: 2025-12-29 ~9:10 PM EST

## Current Status Summary

### What's Working
- All Azure infrastructure created via Terraform (SQL, Storage, App Insights, etc.)
- GitHub secrets configured for deployment tokens
- SWA app settings configured (AZURE_STORAGE_CONNECTION_STRING, ADMIN_EMAILS)
- Both SWAs linked to GitHub repository
- App builds and publishes successfully
- **GitHub Actions deployment working** using SWA CLI directly
- **Prod site live**: https://ashy-pebble-07e5d0b10.2.azurestaticapps.net (HTTP 200)

### What's NOT Working
- `Azure/static-web-apps-deploy@v1` GitHub Action - fails with "InternalServerError" on Terraform-created SWAs
- SWA CLI `--api-language` flags don't work reliably (known bug in v2.0.6/2.0.7)

### Solution Implemented
Switched from `Azure/static-web-apps-deploy@v1` action to using SWA CLI (`swa deploy`) directly in GitHub Actions workflows. The CLI works when:
1. Projects are pre-built before deployment
2. Using `skip_app_build: true` / `skip_api_build: true` equivalent (pre-built artifacts)
3. Deployment token is passed directly

## Azure Resources

### Resource Groups
| Environment | Resource Group |
|-------------|----------------|
| Dev | `rg-dev-cus-atsv2` |
| Prod | `rg-prod-cus-atsv2` |
| Terraform State | `ats-tfstate-rg` |

### Full Resource List (per environment)
| Resource Type | Dev | Prod |
|---------------|-----|------|
| Resource Group | `rg-dev-cus-atsv2` | `rg-prod-cus-atsv2` |
| Storage Account | `stdevcusatsv2` | `stprodcusatsv2` |
| Function App (standalone) | `func-dev-cus-atsv2` | `func-prod-cus-atsv2` |
| Static Web App | `swa-dev-cus-atsv2` | `swa-prod-cus-atsv2` |
| SQL Server | `sql-dev-cus-atsv2` | `sql-prod-cus-atsv2` |
| SQL Database | `sqldb-dev-cus-atsv2` | `sqldb-prod-cus-atsv2` |
| App Insights | `ai-dev-cus-atsv2` | `ai-prod-cus-atsv2` |
| Service Plan | `asp-dev-cus-atsv2` | `asp-prod-cus-atsv2` |

### Static Web App URLs
- **Dev**: https://blue-smoke-0b4410710.2.azurestaticapps.net
- **Prod**: https://ashy-pebble-07e5d0b10.2.azurestaticapps.net

## Files Modified/Created

### Terraform Files (`infrastructure/terraform/`)
- `main.tf` - All Azure resources with naming convention `{type}-{env}-{region}-atsv2`
- `variables.tf` - Variable definitions
- `outputs.tf` - Output definitions for connection strings, tokens, URLs
- `dev.tfvars` - Dev environment values (Free/Basic tiers)
- `prod.tfvars` - Prod environment values (Free/Basic tiers)
- `.credentials` - Local secrets file (gitignored)

### GitHub Workflows (`.github/workflows/`)
- `deploy-dev.yml` - Triggers on push to `dev` branch, uses SWA CLI
- `deploy-prod.yml` - Triggers on push to `main` branch, uses SWA CLI
- DELETED: `azure-static-web-apps-agreeable-river-0e2f38010.yml`

### Config Files
- `/staticwebapp.config.json` (root) - Updated to include `platform.apiRuntime`
- `/src/AgainstTheSpread.Web/wwwroot/staticwebapp.config.json` - Full config with auth

### Custom Agents (`.claude/agents/`)
- `devops-specialist.md` - GitHub Actions and CI/CD expert agent

## Deployment Commands

### 1. Build and Publish (Local)
```bash
cd /Users/Ben.Grossman/Code/against-the-spread-v2

# Publish web app
dotnet publish src/AgainstTheSpread.Web/AgainstTheSpread.Web.csproj -c Release -o ./publish/web

# Publish API
dotnet publish src/AgainstTheSpread.Functions/AgainstTheSpread.Functions.csproj -c Release -o ./publish/api

# Copy config
cp src/AgainstTheSpread.Web/wwwroot/staticwebapp.config.json ./publish/web/wwwroot/
```

### 2. Get Deployment Token
```bash
# For Prod
az staticwebapp secrets list --name swa-prod-cus-atsv2 --resource-group rg-prod-cus-atsv2 --query "properties.apiKey" -o tsv > /tmp/deploy_token.txt

# For Dev
az staticwebapp secrets list --name swa-dev-cus-atsv2 --resource-group rg-dev-cus-atsv2 --query "properties.apiKey" -o tsv > /tmp/deploy_token.txt
```

### 3. Deploy via SWA CLI (Working Command)
```bash
swa deploy ./publish/web/wwwroot \
  --api-location ./publish/api \
  --deployment-token "$(cat /tmp/deploy_token.txt)" \
  --env production
```

### 4. Reset API Key (if needed)
```bash
az staticwebapp secrets reset-api-key --name swa-prod-cus-atsv2 --resource-group rg-prod-cus-atsv2

# Update GitHub secret
NEW_KEY=$(az staticwebapp secrets list --name swa-prod-cus-atsv2 --resource-group rg-prod-cus-atsv2 --query "properties.apiKey" -o tsv)
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN_PROD --body "$NEW_KEY"
```

## Terraform Commands

```bash
cd /Users/Ben.Grossman/Code/against-the-spread-v2/infrastructure/terraform
source .credentials

# --- DEV ENVIRONMENT ---
tofu init -reconfigure -backend-config="key=dev.terraform.tfstate"
tofu plan -var-file="dev.tfvars" \
  -var="sql_admin_login=$SQL_ADMIN_LOGIN" \
  -var="sql_admin_password=$SQL_ADMIN_PASSWORD" \
  -var="admin_emails=$ADMIN_EMAILS"
tofu apply -var-file="dev.tfvars" \
  -var="sql_admin_login=$SQL_ADMIN_LOGIN" \
  -var="sql_admin_password=$SQL_ADMIN_PASSWORD" \
  -var="admin_emails=$ADMIN_EMAILS" -auto-approve

# --- PROD ENVIRONMENT ---
tofu init -reconfigure -backend-config="key=prod.terraform.tfstate"
tofu plan -var-file="prod.tfvars" \
  -var="sql_admin_login=$SQL_ADMIN_LOGIN" \
  -var="sql_admin_password=$SQL_ADMIN_PASSWORD" \
  -var="admin_emails=$ADMIN_EMAILS"
tofu apply -var-file="prod.tfvars" \
  -var="sql_admin_login=$SQL_ADMIN_LOGIN" \
  -var="sql_admin_password=$SQL_ADMIN_PASSWORD" \
  -var="admin_emails=$ADMIN_EMAILS" -auto-approve
```

## GitHub Secrets
| Secret Name | Description |
|-------------|-------------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV` | Dev SWA deployment token |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_PROD` | Prod SWA deployment token |

## SWA App Settings (Configured via Azure CLI)
```bash
# These were already set:
az staticwebapp appsettings set --name swa-prod-cus-atsv2 --resource-group rg-prod-cus-atsv2 \
  --setting-names "AZURE_STORAGE_CONNECTION_STRING=..." "ADMIN_EMAILS=bengrossm@gmail.com"
```

## Still Needed for Full Functionality
1. **Google OAuth settings** - Need to configure in SWA app settings:
   - `GOOGLE_CLIENT_ID`
   - `GOOGLE_CLIENT_SECRET`
2. **Google OAuth redirect URIs** - Add to Google Cloud Console:
   - `https://blue-smoke-0b4410710.2.azurestaticapps.net/.auth/login/google/callback`
   - `https://ashy-pebble-07e5d0b10.2.azurestaticapps.net/.auth/login/google/callback`

## Troubleshooting Notes

### SWA CLI Known Issues (v2.0.6/2.0.7)
- **`--api-language` flags don't work**: Known bug where flags aren't passed to StaticSitesClient binary
- **Workaround**: Pre-build projects and deploy without relying on flags
- GitHub Issues: [#980](https://github.com/Azure/static-web-apps-cli/issues/980), [#699](https://github.com/Azure/static-web-apps-cli/issues/699)

### Azure/static-web-apps-deploy@v1 Action Issues
- Fails with "InternalServerError" on Terraform-created SWAs
- Even with valid token, fresh API key reset, and correct configuration
- **Solution**: Use SWA CLI directly instead of the action

### GitHub Actions Workflow Pattern (Working)
```yaml
- name: Setup Node.js
  uses: actions/setup-node@v4
  with:
    node-version: '20'

- name: Install SWA CLI
  run: npm install -g @azure/static-web-apps-cli

- name: Deploy to Azure Static Web Apps
  run: |
    swa deploy ./publish/web/wwwroot \
      --api-location ./publish/api \
      --deployment-token "${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_PROD }}" \
      --env production
  env:
    SWA_CLI_DEPLOYMENT_TOKEN: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_PROD }}
```

## Credentials Location
- Local credentials: `infrastructure/terraform/.credentials`
- Contains: SQL_ADMIN_LOGIN, SQL_ADMIN_PASSWORD, ADMIN_EMAILS
- **Do not commit this file**

## Roadmap Reference
See `/ROADMAP.md` for full project roadmap. Current status:
- Phase 1: Infrastructure Separation - **Complete** (11/11 tasks)
- Phase 1.6: Terraform CI/CD Automation - Added as future enhancement
- Phase 2-7: Not started
