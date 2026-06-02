module "readable_bot_network_representation" {
  source = "../../modules/readable-bot-network-representation"

  resource_group_name = var.resource_group_name
  location            = var.location

  name_prefix = "deliverybot"
  environment = "rbnr-dev"

  eventhub_resource_group_name  = var.resource_group_name
  eventhub_namespace_name       = var.eventhub_namespace_name
  robot_output_eventhub_name    = var.robot_output_eventhub_name
  eventhub_consumer_group_name  = "readable-bot-network-dev"
  assign_eventhub_receiver_role = true

  cosmos_enable_serverless = true
  function_dotnet_version  = "8.0"

  tags = {
    project     = "DeliveryBotSystem"
    feature     = "ReadableBotNetworkRepresentation"
    environment = "rbnr-dev"
    temporary   = "true"
  }
}
