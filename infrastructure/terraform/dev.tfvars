# Dev Environment Configuration
# Usage:
#   terraform init -backend-config="key=dev.terraform.tfstate"
#   terraform plan -var-file="dev.tfvars" -var="sql_admin_login=YOUR_LOGIN" -var="sql_admin_password=YOUR_PASSWORD"
#   terraform apply -var-file="dev.tfvars" -var="sql_admin_login=YOUR_LOGIN" -var="sql_admin_password=YOUR_PASSWORD"

environment = "dev"
location    = "centralus"

# Dev uses Free/Basic tiers
static_web_app_sku_tier = "Free"
static_web_app_sku_size = "Free"
sql_sku_name            = "Basic"
sql_max_size_gb         = 2
