const env = typeof import.meta !== "undefined" && import.meta.env
  ? import.meta.env
  : {}

function trimTrailingSlash(value) {
  return typeof value === "string" ? value.replace(/\/+$/, "") : ""
}

export const appConfig = {
  simulatorApiBase: env.VITE_SIMULATOR_API_BASE || "/api/simulator",
  mapTileUrl:
    env.VITE_MAP_TILE_URL || "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
  orderServiceUrl: trimTrailingSlash(env.VITE_ORDER_SERVICE_URL),
  osrmApiUrl: trimTrailingSlash(env.VITE_OSRM_API_URL) || "https://router.project-osrm.org",
  agentApiUrl: trimTrailingSlash(
    env.VITE_AGENT_API_URL || (env.DEV ? "/api/agent" : "")
  )
}
