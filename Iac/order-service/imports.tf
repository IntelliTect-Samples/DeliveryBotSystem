# One-time adoption of the pre-existing Container App into Terraform state.
#
# The deliverybot-order-service app was originally created by hand, so the
# first `terraform apply` must IMPORT it instead of trying to create a
# duplicate (azurerm refuses to create over an existing resource). This import
# block lets CI's service principal do that automatically on the first apply —
# no manual out-of-band `terraform import` needed.
#
# SAFE TO DELETE after the first successful apply has run in CI (the resource
# will already be in remote state; the block then becomes a no-op).
import {
  to = module.order_service_app.azurerm_container_app.this
  id = "${data.azurerm_resource_group.rg.id}/providers/Microsoft.App/containerApps/${var.container_app_name}"
}
