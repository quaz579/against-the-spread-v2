# Against The Spread - Terraform Configuration
# Usage:
#   terraform init -backend-config="key=dev.terraform.tfstate"
#   terraform plan -var-file="dev.tfvars"
#   terraform apply -var-file="dev.tfvars"

terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.98.0"  # Required for app_settings on static_web_app
    }
  }

  backend "azurerm" {
    resource_group_name  = "ats-tfstate-rg"
    storage_account_name = "atstfstate"
    container_name       = "tfstate"
    # key is passed via -backend-config="key=dev.terraform.tfstate" or "key=prod.terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}

# Locals
locals {
  # Naming convention: {resource type}-{environment}-{region abbrev}-atsv2
  # Region abbreviations: centralus = cus, eastus = eus, eastus2 = eus2
  # Storage accounts: st{env}{region}atsv2 (no dashes)
  region_abbrev = {
    "centralus" = "cus"
    "eastus"    = "eus"
    "eastus2"   = "eus2"
    "westus"    = "wus"
    "westus2"   = "wus2"
  }
  region_short = lookup(local.region_abbrev, var.location, substr(var.location, 0, 3))
  name_suffix  = "${var.environment}-${local.region_short}-atsv2"

  tags = merge({
    Project     = "against-the-spread"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }, var.extra_tags)
}

#------------------------------------------------------------------------------
# Resource Group
#------------------------------------------------------------------------------
resource "azurerm_resource_group" "main" {
  name     = "rg-${local.name_suffix}"
  location = var.location
  tags     = local.tags
}

#------------------------------------------------------------------------------
# Storage Account
#------------------------------------------------------------------------------
resource "azurerm_storage_account" "main" {
  name                     = "st${var.environment}${local.region_short}atsv2"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET", "HEAD", "POST", "PUT"]
      allowed_origins    = ["*"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }

  tags = local.tags
}

resource "azurerm_storage_container" "gamefiles" {
  name                  = "gamefiles"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

#------------------------------------------------------------------------------
# Application Insights
#------------------------------------------------------------------------------
resource "azurerm_application_insights" "main" {
  name                = "ai-${local.name_suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  tags                = local.tags

  lifecycle {
    ignore_changes = [workspace_id]
  }
}

#------------------------------------------------------------------------------
# Azure SQL Database
#------------------------------------------------------------------------------
resource "azurerm_mssql_server" "main" {
  name                         = "sql-${local.name_suffix}"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"

  tags = local.tags
}

resource "azurerm_mssql_database" "main" {
  name         = "sqldb-${local.name_suffix}"
  server_id    = azurerm_mssql_server.main.id
  collation    = "SQL_Latin1_General_CP1_CI_AS"
  license_type = "LicenseIncluded"
  max_size_gb  = var.sql_max_size_gb
  sku_name     = var.sql_sku_name

  tags = local.tags
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# NOTE: The standalone Function App and Service Plan below are NOT NEEDED when using
# Azure Static Web Apps managed functions (deployed via SWA CLI with --api-location).
# SWA creates and manages its own Function App internally.
# These resources are commented out but kept for reference. They can be deleted once
# we confirm SWA managed functions are working correctly with the app_settings above.
#
# #------------------------------------------------------------------------------
# # App Service Plan (Consumption) - NOT NEEDED for SWA managed functions
# #------------------------------------------------------------------------------
# resource "azurerm_service_plan" "main" {
#   name                = "asp-${local.name_suffix}"
#   location            = azurerm_resource_group.main.location
#   resource_group_name = azurerm_resource_group.main.name
#   os_type             = "Linux"
#   sku_name            = "Y1"
#   tags                = local.tags
# }
#
# #------------------------------------------------------------------------------
# # Function App - NOT NEEDED for SWA managed functions
# #------------------------------------------------------------------------------
# resource "azurerm_linux_function_app" "main" {
#   name                = "func-${local.name_suffix}"
#   location            = azurerm_resource_group.main.location
#   resource_group_name = azurerm_resource_group.main.name
#   service_plan_id     = azurerm_service_plan.main.id
#
#   storage_account_name       = azurerm_storage_account.main.name
#   storage_account_access_key = azurerm_storage_account.main.primary_access_key
#
#   site_config {
#     application_stack {
#       dotnet_version              = "8.0"
#       use_dotnet_isolated_runtime = true
#     }
#
#     cors {
#       allowed_origins = ["https://${azurerm_static_web_app.main.default_host_name}"]
#     }
#   }
#
#   app_settings = {
#     "FUNCTIONS_WORKER_RUNTIME"              = "dotnet-isolated"
#     "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.main.instrumentation_key
#     "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
#     "AzureWebJobsStorage"                   = azurerm_storage_account.main.primary_connection_string
#     "AZURE_STORAGE_CONNECTION_STRING"       = azurerm_storage_account.main.primary_connection_string
#     "WEBSITE_RUN_FROM_PACKAGE"              = "1"
#     "SqlConnectionString"                   = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
#     "ADMIN_EMAILS"                          = var.admin_emails
#   }
#
#   tags = local.tags
# }

#------------------------------------------------------------------------------
# Static Web App
#------------------------------------------------------------------------------
resource "azurerm_static_web_app" "main" {
  name                = "swa-${local.name_suffix}"
  location            = var.static_web_app_location
  resource_group_name = azurerm_resource_group.main.name
  sku_tier            = var.static_web_app_sku_tier
  sku_size            = var.static_web_app_sku_size
  tags                = local.tags

  # App settings for managed functions deployed via SWA CLI
  # Note: AzureWebJobsStorage is reserved/managed by SWA internally, so we don't set it here
  app_settings = merge({
    "AZURE_STORAGE_CONNECTION_STRING"       = azurerm_storage_account.main.primary_connection_string
    "SqlConnectionString"                   = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    "ADMIN_EMAILS"                          = var.admin_emails
    "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.main.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
  },
    var.enable_test_auth ? { "ENABLE_TEST_AUTH" = "true" } : {},
    var.cfbd_api_key != "" ? { "CFBD_API_KEY" = var.cfbd_api_key } : {},
    var.disable_game_locking ? { "DISABLE_GAME_LOCKING" = "true" } : {}
  )
}
