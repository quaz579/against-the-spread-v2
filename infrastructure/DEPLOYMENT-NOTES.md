# Deployment Notes - Against The Spread V2

**Last Updated**: 2025-12-29 ~8:30 PM EST

## Current Status Summary

### What's Working
- All Azure infrastructure created via Terraform (SQL, Storage, App Insights, etc.)
- GitHub secrets configured for deployment tokens
- SWA app settings configured (AZURE_STORAGE_CONNECTION_STRING, ADMIN_EMAILS)
- Both SWAs linked to GitHub repository
- App builds and publishes successfully

### What's NOT Working
- **GitHub Actions deployment** fails with "InternalServerError" - appears to be Azure-side issue with newly created SWAs
- **SWA CLI deployment** fails with "Cannot deploy to the function app because Function language info isn't provided" even though:
  - `staticwebapp.config.json` has `"platform": { "apiRuntime": "dotnet-isolated:8.0" }`
  - Explicit `--api-language dotnet-isolated --api-version 8.0` flags are provided

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
- `deploy-dev.yml` - Triggers on push to `dev` branch
- `deploy-prod.yml` - Triggers on push to `main` branch
- DELETED: `azure-static-web-apps-agreeable-river-0e2f38010.yml`

### Config Files
- `/staticwebapp.config.json` (root) - Updated to include `platform.apiRuntime`
- `/src/AgainstTheSpread.Web/wwwroot/staticwebapp.config.json` - Full config with auth

## Deployment Commands

### 1. Build and Publish
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

### 3. Deploy via SWA CLI (CURRENT BLOCKER)
```bash
# This command is failing with "Function language info isn't provided" error
swa deploy ./publish/web/wwwroot \
  --api-location ./publish/api \
  --deployment-token "$(cat /tmp/deploy_token.txt)" \
  --env production \
  --api-language dotnet-isolated \
  --api-version 8.0
```

### 4. Reset API Key (if needed)
```bash
az staticwebapp secrets reset-api-key --name swa-prod-cus-atsv2 --resource-group rg-prod-cus-atsv2
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

To update GitHub secrets after resetting API key:
```bash
NEW_KEY=$(az staticwebapp secrets list --name swa-prod-cus-atsv2 --resource-group rg-prod-cus-atsv2 --query "properties.apiKey" -o tsv)
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN_PROD --body "$NEW_KEY"
```

## SWA App Settings (Configured via Azure CLI)
```bash
# These were already set:
az staticwebapp appsettings set --name swa-prod-cus-atsv2 --resource-group rg-prod-cus-atsv2 \
  --setting-names "AZURE_STORAGE_CONNECTION_STRING=..." "ADMIN_EMAILS=bengrossm@gmail.com"
```

## Still Needed for Full Functionality
1. **Fix SWA deployment** - Either via CLI or GitHub Actions
2. **Google OAuth settings** - Need to configure in SWA app settings:
   - `GOOGLE_CLIENT_ID`
   - `GOOGLE_CLIENT_SECRET`
3. **Google OAuth redirect URIs** - Add to Google Cloud Console:
   - `https://blue-smoke-0b4410710.2.azurestaticapps.net/.auth/login/google/callback`
   - `https://ashy-pebble-07e5d0b10.2.azurestaticapps.net/.auth/login/google/callback`

## Troubleshooting Notes

### SWA CLI "Function language info isn't provided" Error
Even with `--api-language dotnet-isolated --api-version 8.0` flags and `platform.apiRuntime` in config, the CLI fails. Possible causes:
1. SWA CLI version issue (currently 2.0.7)
2. Config file format issue
3. Azure backend not recognizing the newly created SWA

### GitHub Actions "InternalServerError" Error
The Azure/static-web-apps-deploy@v1 action fails with:
```
The content server has rejected the request with: InternalServerError
Reason: An unexpected error has occurred. Please ensure your deployment token is properly set...
```

This happens even after:
- Resetting the API key
- Linking the SWA to GitHub repo
- Waiting several minutes

## Next Steps to Try
1. Try deploying WITHOUT the api-location to see if just the frontend deploys
2. Try older version of SWA CLI
3. Try deploying through Azure Portal UI to see if that initializes the SWA
4. Create SWA through Azure Portal (instead of Terraform) to compare behavior
5. Check Azure status page for any ongoing issues

## Credentials Location
- Local credentials: `infrastructure/terraform/.credentials`
- Contains: SQL_ADMIN_LOGIN, SQL_ADMIN_PASSWORD, ADMIN_EMAILS
- **Do not commit this file**

## Roadmap Reference
See `/ROADMAP.md` for full project roadmap. Current status:
- Phase 1: Infrastructure Separation - **Complete** (11/11 tasks)
- Phase 1.6: Terraform CI/CD Automation - Added as future enhancement
- Phase 2-7: Not started
