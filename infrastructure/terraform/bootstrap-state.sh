#!/bin/bash
# Bootstrap Terraform State Storage
# Run this script once to create the storage account for Terraform state
# Requires: az cli logged in with appropriate permissions

set -e

RESOURCE_GROUP="ats-tfstate-rg"
STORAGE_ACCOUNT="atstfstate"
CONTAINER_NAME="tfstate"
LOCATION="centralus"

echo "Creating Terraform state storage..."

# Create resource group
echo "Creating resource group: $RESOURCE_GROUP"
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --tags Project=against-the-spread ManagedBy=Terraform Purpose=TerraformState

# Create storage account
echo "Creating storage account: $STORAGE_ACCOUNT"
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false \
  --tags Project=against-the-spread ManagedBy=Terraform Purpose=TerraformState

# Create blob container
echo "Creating blob container: $CONTAINER_NAME"
az storage container create \
  --name "$CONTAINER_NAME" \
  --account-name "$STORAGE_ACCOUNT"

echo ""
echo "Terraform state storage created successfully!"
echo ""
echo "Resource Group: $RESOURCE_GROUP"
echo "Storage Account: $STORAGE_ACCOUNT"
echo "Container: $CONTAINER_NAME"
echo ""
echo "Next steps:"
echo "1. cd infrastructure/terraform/environments/dev"
echo "2. terraform init"
echo "3. terraform plan -var 'sql_admin_login=YOUR_LOGIN' -var 'sql_admin_password=YOUR_PASSWORD'"
echo "4. terraform apply"
