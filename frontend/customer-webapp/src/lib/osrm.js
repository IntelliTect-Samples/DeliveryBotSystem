import { appConfig } from "./config.js"

const DEFAULT_ROUTE_TIMEOUT_MS = 8000

function isFiniteCoordinate(value) {
  if (value === null || value === undefined || value === "") {
    return false
  }

  return Number.isFinite(Number(value))
}

export function hasRouteEndpoints(origin, destination) {
  return isFiniteCoordinate(origin?.latitude) &&
    isFiniteCoordinate(origin?.longitude) &&
    isFiniteCoordinate(destination?.latitude) &&
    isFiniteCoordinate(destination?.longitude)
}

export function buildRouteUrl(origin, destination) {
  const originPair = `${origin.longitude},${origin.latitude}`
  const destinationPair = `${destination.longitude},${destination.latitude}`

  return `${appConfig.osrmApiUrl}/route/v1/driving/${originPair};${destinationPair}?overview=full&geometries=geojson`
}

export function formatDistance(distanceMeters) {
  return distanceMeters >= 1000
    ? `${(distanceMeters / 1000).toFixed(1)} km`
    : `${Math.round(distanceMeters)} m`
}

export function formatDuration(durationSeconds) {
  const totalMinutes = Math.max(1, Math.round(durationSeconds / 60))

  if (totalMinutes < 60) {
    return `${totalMinutes} min`
  }

  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60

  return minutes === 0 ? `${hours} hr` : `${hours} hr ${minutes} min`
}

export function getRouteStrokeStyles(source = "osrm") {
  return {
    casing: {
      color: "#fff7ed",
      weight: 10,
      opacity: 0.95
    },
    line: {
      color: source === "osrm" ? "#ea580c" : "#0f766e",
      weight: 6,
      opacity: 1
    }
  }
}

function haversineDistance(origin, destination) {
  const earthRadiusMeters = 6371000
  const toRadians = (value) => (value * Math.PI) / 180
  const lat1 = toRadians(origin.latitude)
  const lat2 = toRadians(destination.latitude)
  const latDelta = toRadians(destination.latitude - origin.latitude)
  const lonDelta = toRadians(destination.longitude - origin.longitude)

  const a =
    Math.sin(latDelta / 2) ** 2 +
    Math.cos(lat1) * Math.cos(lat2) * Math.sin(lonDelta / 2) ** 2

  return 2 * earthRadiusMeters * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
}

export function calculateHaversineDistance(origin, destination) {
  return haversineDistance(origin, destination)
}

export function buildFallbackRoute(origin, destination, warning) {
  const distanceMeters = haversineDistance(origin, destination)
  const durationSeconds = Math.max(300, distanceMeters / 1.4)

  return {
    coordinates: [
      [origin.latitude, origin.longitude],
      [destination.latitude, destination.longitude]
    ],
    distanceMeters,
    durationSeconds,
    source: "fallback",
    warning
  }
}

export function normalizeRouteResponse(payload) {
  const route = payload?.routes?.[0]

  if (!route || !Array.isArray(route.geometry?.coordinates) || route.geometry.coordinates.length < 2) {
    throw new Error("OSRM did not return a usable route.")
  }

  return {
    coordinates: route.geometry.coordinates.map(([longitude, latitude]) => [
      latitude,
      longitude
    ]),
    distanceMeters: route.distance,
    durationSeconds: route.duration,
    source: "osrm",
    warning: ""
  }
}

async function fetchWithTimeout(fetchImpl, url, options, timeoutMs) {
  if (typeof AbortController === "undefined") {
    return fetchImpl(url, options)
  }

  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)

  try {
    return await fetchImpl(url, {
      ...options,
      signal: controller.signal
    })
  } finally {
    clearTimeout(timer)
  }
}

export async function fetchRoute(origin, destination, options = {}) {
  const fetchImpl = options.fetchImpl || fetch
  const timeoutMs = options.timeoutMs || DEFAULT_ROUTE_TIMEOUT_MS

  if (!hasRouteEndpoints(origin, destination)) {
    throw new Error("A route requires both a robot location and a destination.")
  }

  try {
    const response = await fetchWithTimeout(
      fetchImpl,
      buildRouteUrl(origin, destination),
      undefined,
      timeoutMs
    )

    if (!response.ok) {
      throw new Error(`OSRM returned HTTP ${response.status}.`)
    }

    const data = await response.json()
    return normalizeRouteResponse(data)
  } catch (error) {
    return buildFallbackRoute(
      origin,
      destination,
      error?.name === "AbortError"
        ? "the route service timed out"
        : error.message
    )
  }
}
