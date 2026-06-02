# One-time adoption of the pre-existing Admin Web App into Terraform state.
#
# WA-DeliveryBot-Admin-dev was originally created by hand. The first
# `terraform apply` will import it rather than create a duplicate.
#
# Address path:
#   module.admin_webapp      ← called from Iac/main.tf as "admin_webapp"
#   .module.admin_webapp     ← called from admin-webapp/main.tf as "admin_webapp"
#   .azurerm_linux_web_app.admin
#
# SAFE TO DELETE after the first successful apply.

locals {
  sub = "a06983f7-7384-4a09-a092-b13a3896be85"
  rg  = "ewu-deliverybotsystem-rg"
}

import {
  to = module.admin_webapp.azurerm_linux_web_app.admin
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.Web/sites/WA-DeliveryBot-Admin-dev"
}
