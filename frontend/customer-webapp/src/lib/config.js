const env = typeof import.meta !== "undefined" && import.meta.env
  ? import.meta.env
  : {}
const isDev = Boolean(env.DEV)

function trimTrailingSlash(value) {
  return typeof value === "string" ? value.replace(/\/+$/, "") : ""
}

const platformApiBase = trimTrailingSlash(
  env.VITE_API_MANAGEMENT_BASE_URL || env.VITE_PLATFORM_API_BASE
)

export const appConfig = {
  simulatorApiBase: isDev
    ? "/api/simulator"
    : trimTrailingSlash(env.VITE_SIMULATOR_API_BASE) || "/api/simulator",
  mapTileUrl:
    env.VITE_MAP_TILE_URL || "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
  orderServiceUrl: isDev
    ? "/api/order-service"
    : trimTrailingSlash(env.VITE_ORDER_SERVICE_URL || env.VITE_ORDER_SERVICE_API_BASE) ||
      (platformApiBase ? `${platformApiBase}/orders` : ""),
  osrmApiUrl: trimTrailingSlash(env.VITE_OSRM_API_URL) || "https://router.project-osrm.org",
  agentApiUrl: isDev
    ? "/api/agent"
    : trimTrailingSlash(env.VITE_AGENT_API_URL) ||
      (platformApiBase ? `${platformApiBase}/agent` : "")
}
