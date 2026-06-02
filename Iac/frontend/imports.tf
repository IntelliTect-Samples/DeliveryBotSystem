# One-time adoption of the pre-existing customer frontend App Service.
#
# WA-DeliveryBot-dev was originally created by hand. The first `terraform apply`
# will import it rather than create a duplicate.
#
# SAFE TO DELETE after the first successful apply.

locals {
  sub = "a06983f7-7384-4a09-a092-b13a3896be85"
  rg  = "ewu-deliverybotsystem-rg"
}

import {
  to = module.frontend_webapp.azurerm_linux_web_app.frontend
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.Web/sites/WA-DeliveryBot-dev"
}
