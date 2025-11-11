# Local Development with Authentication

## Quick Start (Recommended - Mock Auth)

The SWA CLI supports mock authentication for local testing without needing real OAuth:

```bash
# Start the local SWA emulator with mock auth
swa start http://localhost:5158 --api-location http://localhost:7071 --app-artifact-location src/AgainstTheSpread.Web/bin/Debug/net8.0/wwwroot

# In separate terminals:
# Terminal 1: Start Blazor app
cd src/AgainstTheSpread.Web && dotnet watch run

# Terminal 2: Start Functions
cd src/AgainstTheSpread.Functions && func start
```

Then access: `http://localhost:4280` (SWA CLI default port)

To login with mock auth, visit: `http://localhost:4280/.auth/login/google`

You'll see a mock login page where you can enter any email (use `bengrossm@gmail.com` to match your ADMIN_EMAILS).

## Alternative: Real Google OAuth Locally

If you want to test with real Google OAuth:

1. Add localhost redirect URI in Google Console:
   ```
   http://localhost:4280/.auth/login/google/callback
   ```

2. Create a `.env` file in the project root:
   ```
   GOOGLE_CLIENT_ID=520517828773-09fud86es46rrj48bosc2g5de1ubk46i.apps.googleusercontent.com
   GOOGLE_CLIENT_SECRET=<redacted>>
   ADMIN_EMAILS=<redacted>
   ```

3. Start SWA CLI and load environment variables:
   ```bash
   swa start --env-file .env
   ```

## Simpler Testing

The deployment to dev takes ~2-3 minutes. Since you've already set up the Google OAuth properly, waiting for the dev deployment is actually faster than setting up local OAuth testing.

You can monitor the deployment at:
https://github.com/quaz579/against-the-spread/actions
