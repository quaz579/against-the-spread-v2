#!/bin/bash
set -e

# Variables
PROJECT_NAME="against-the-spread"
LOCATION="centralus"
RESOURCE_GROUP="${PROJECT_NAME}-rg"
STORAGE_ACCOUNT="stprdagnstthesprd"

echo "🚀 Deploying Against The Spread to Azure..."
echo ""

# Login check
if ! az account show &>/dev/null; then
    echo "Please login to Azure first:"
    az login
fi

# Create resource group
echo "📦 Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Create Static Web App
echo "🌐 Creating Static Web App..."
az staticwebapp create \
  --name $PROJECT_NAME \
  --resource-group $RESOURCE_GROUP \
  --source https://github.com/quaz579/against-the-spread \
  --location $LOCATION \
  --branch main \
  --app-location "src/AgainstTheSpread.Web" \
  --api-location "src/AgainstTheSpread.Functions" \
  --output-location "bin/Release/net8.0/publish/wwwroot" \
  --login-with-github \
  --output none

# Create storage account
echo "💾 Creating storage account..."
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --output none

# Get connection string
CONNECTION_STRING=$(az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query connectionString \
  --output tsv)

# Create container
echo "📁 Creating storage container..."
az storage container create \
  --name gamefiles \
  --account-name $STORAGE_ACCOUNT \
  --connection-string "$CONNECTION_STRING" \
  --output none

# Get Static Web App name
STATIC_WEB_APP_NAME=$(az staticwebapp list \
  --resource-group $RESOURCE_GROUP \
  --query "[0].name" \
  --output tsv)

# Configure app settings
echo "⚙️  Configuring application settings..."
az staticwebapp appsettings set \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --setting-names AzureWebJobsStorage="$CONNECTION_STRING" \
  --output none

# Get deployment token
DEPLOYMENT_TOKEN=$(az staticwebapp secrets list \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.apiKey \
  --output tsv)

# Add secrets to GitHub
echo "🔐 Adding secrets to GitHub..."
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$DEPLOYMENT_TOKEN"
gh secret set AZURE_STORAGE_CONNECTION_STRING --body "$CONNECTION_STRING"

# Get app URL
APP_URL=$(az staticwebapp show \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query defaultHostname \
  --output tsv)

echo ""
echo "✅ Deployment complete!"
echo ""
echo "📊 Resource Group: $RESOURCE_GROUP"
echo "💾 Storage Account: $STORAGE_ACCOUNT"
echo "🌐 App URL: https://$APP_URL"
echo ""
echo "GitHub Actions will now automatically deploy your app."
echo "Watch the deployment: gh run watch"
echo ""
