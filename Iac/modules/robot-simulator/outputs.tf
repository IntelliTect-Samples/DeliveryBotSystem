output "url" {
  description = "Public HTTPS URL of the Robot Simulator Container App."
  value       = "https://${azurerm_container_app.robot_simulator.ingress[0].fqdn}"
}
