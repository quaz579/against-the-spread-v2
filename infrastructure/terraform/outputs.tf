# Outputs

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "storage_account_name" {
  description = "Name of the storage account"
  value       = azurerm_storage_account.main.name
}

output "storage_connection_string" {
  description = "Storage account connection string"
  value       = azurerm_storage_account.main.primary_connection_string
  sensitive   = true
}

# Commented out - using SWA managed functions instead of standalone function app
# output "function_app_name" {
#   description = "Name of the function app"
#   value       = azurerm_linux_function_app.main.name
# }

# output "function_app_url" {
#   description = "URL of the function app"
#   value       = "https://${azurerm_linux_function_app.main.default_hostname}"
# }

output "static_web_app_name" {
  description = "Name of the static web app"
  value       = azurerm_static_web_app.main.name
}

output "static_web_app_url" {
  description = "URL of the static web app"
  value       = "https://${azurerm_static_web_app.main.default_host_name}"
}

output "static_web_app_api_key" {
  description = "Deployment token for Static Web App"
  value       = azurerm_static_web_app.main.api_key
  sensitive   = true
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_name" {
  description = "Name of the SQL database"
  value       = azurerm_mssql_database.main.name
}

output "sql_connection_string" {
  description = "Connection string for the SQL database"
  value       = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  sensitive   = true
}

output "application_insights_instrumentation_key" {
  description = "Application Insights instrumentation key"
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}
