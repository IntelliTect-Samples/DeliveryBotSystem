locals {
  import_sub = "207d6c46-9d83-44fc-b7d5-6e2cfcf4d001"
  import_rg  = "deliverybot-final-rg"
}

import {
  to = module.shared_infra.azurerm_eventhub_namespace.simulator
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.EventHub/namespaces/deliverybotfinalevhns"
}

import {
  to = module.shared_infra.azurerm_eventhub.robot_input
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.EventHub/namespaces/deliverybotfinalevhns/eventhubs/robot-input"
}

import {
  to = module.shared_infra.azurerm_eventhub.robot_output
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.EventHub/namespaces/deliverybotfinalevhns/eventhubs/robot-output"
}
