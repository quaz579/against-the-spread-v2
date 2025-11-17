# Google Authentication Setup for Admin Access

This guide explains how to configure Google authentication for the admin panel.

## Step 1: Create Google OAuth App

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project (or select existing)
3. Enable the **Google+ API**:
   - Go to "APIs & Services" > "Library"
   - Search for "Google+ API"
   - Click "Enable"

4. Create OAuth 2.0 credentials:
   - Go to "APIs & Services" > "Credentials"
   - Click "Create Credentials" > "OAuth client ID"
   - Application type: "Web application"
   - Name: "Against The Spread Admin"
   
5. Add authorized redirect URIs:
   - For dev: `https://dev-<random-id>-<your-swa-name>.azurestaticapps.net/.auth/login/google/callback`
   - For production: `https://<your-swa-name>.azurestaticapps.net/.auth/login/google/callback`
   
   **Note:** Get your exact SWA URL from the Azure portal or GitHub Actions deployment logs

6. Click "Create" and save your:
   - **Client ID** (looks like: `123456789-abc123.apps.googleusercontent.com`)
   - **Client Secret** (looks like: `GOCSPX-abc123xyz...`)

## Step 2: Configure Azure Static Web App

Add these application settings to your SWA (both dev and production):

```bash
# Using Azure CLI
az staticwebapp appsettings set \
  --name <your-swa-name> \
  --resource-group <your-resource-group> \
  --setting-names \
    GOOGLE_CLIENT_ID="<your-client-id>" \
    GOOGLE_CLIENT_SECRET="<your-client-secret>" \
    ADMIN_EMAILS="your-email@gmail.com,other-admin@gmail.com"
```

Or via Azure Portal:
1. Go to your Static Web App
2. Click "Configuration" under Settings
3. Add Application settings:
   - `GOOGLE_CLIENT_ID`: Your Google OAuth Client ID
   - `GOOGLE_CLIENT_SECRET`: Your Google OAuth Client Secret
   - `ADMIN_EMAILS`: Comma-separated list of authorized admin emails

## Step 3: Test the Setup

1. Deploy to dev branch and wait for deployment
2. Navigate to `/admin` on your dev URL
3. Click "Sign in with Google"
4. After signing in, you should see the upload interface
5. If your email is not in `ADMIN_EMAILS`, you'll see an access denied message

## Step 4: Production Deployment

Once tested on dev:
1. Merge dev to main
2. Update Google OAuth redirect URIs to include production URL
3. Ensure production SWA has the same app settings configured

## Troubleshooting

### "Authentication required" error
- Check that `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` are set in SWA app settings
- Verify redirect URI in Google Console matches your SWA URL exactly

### "Access denied" error
- Check that your email is in the `ADMIN_EMAILS` setting
- Ensure there are no extra spaces in the email list
- Emails are case-insensitive

### Upload still fails
- Check Azure Functions logs for detailed error messages
- Verify storage connection string is still configured

## Security Notes

- Google OAuth is free and secure
- Only emails in `ADMIN_EMAILS` can upload files
- All other endpoints remain public (read-only)
- Frontend shows login UI, but backend enforces authorization
