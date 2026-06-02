resource_group_name      = "ewu-deliverybotsystem-rg"
location                 = "westus2"
container_app_env_name   = "managedEnvironment-ewudeliverybots-aa2f"
acr_name                 = "DeliverybotCR"
event_hub_namespace_name = "DeliverybotSimulator-EVHNS"
container_app_name       = "deliverybot-robot-simulator"
image_name               = "deliverybot-robot-simulator"
image_tag                = "latest"

# eventhub_connection_string is sensitive — supply via:
#   TF_VAR_eventhub_connection_string environment variable (CI)
#   or a local secrets.auto.tfvars file (never commit)
