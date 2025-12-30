# Prod Environment Configuration
# Usage:
#   terraform init -backend-config="key=prod.terraform.tfstate"
#   terraform plan -var-file="prod.tfvars" -var="sql_admin_login=YOUR_LOGIN" -var="sql_admin_password=YOUR_PASSWORD"
#   terraform apply -var-file="prod.tfvars" -var="sql_admin_login=YOUR_LOGIN" -var="sql_admin_password=YOUR_PASSWORD"

environment = "prod"
location    = "centralus"

# Prod uses Free/Basic tiers (upgrade later if needed)
static_web_app_sku_tier = "Free"
static_web_app_sku_size = "Free"
sql_sku_name            = "Basic"
sql_max_size_gb         = 2
