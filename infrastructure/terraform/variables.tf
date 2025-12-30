# Root Variables - Pass via .tfvars files

variable "environment" {
  description = "Environment name (dev, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "Environment must be 'dev' or 'prod'."
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "centralus"
}

variable "static_web_app_location" {
  description = "Azure region for Static Web App (limited regions: centralus, eastus2, eastasia, westeurope, westus2)"
  type        = string
  default     = "centralus"
}

# SQL Database Configuration
variable "sql_admin_login" {
  description = "SQL Server administrator login"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "sql_sku_name" {
  description = "SQL Database SKU name (Basic, S0, S1, etc.)"
  type        = string
  default     = "Basic"
}

variable "sql_max_size_gb" {
  description = "SQL Database max size in GB"
  type        = number
  default     = 2
}

# Static Web App Configuration
variable "static_web_app_sku_tier" {
  description = "Static Web App SKU tier (Free, Standard)"
  type        = string
  default     = "Free"
}

variable "static_web_app_sku_size" {
  description = "Static Web App SKU size (Free, Standard)"
  type        = string
  default     = "Free"
}

# Admin Configuration
variable "admin_emails" {
  description = "Comma-separated list of admin email addresses"
  type        = string
  default     = ""
}

# Tags
variable "extra_tags" {
  description = "Additional tags to apply to resources"
  type        = map(string)
  default     = {}
}
