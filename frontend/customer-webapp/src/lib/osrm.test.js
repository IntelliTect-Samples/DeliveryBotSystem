import test from "node:test"
import assert from "node:assert/strict"
import {
  buildFallbackRoute,
  buildRouteUrl,
  calculateHaversineDistance,
  fetchRoute,
  formatDistance,
  formatDuration,
  getRouteStrokeStyles,
  hasRouteEndpoints,
  normalizeRouteResponse
} from "./osrm.js"

const origin = {
  latitude: 47.6588,
  longitude: -117.426
}

const destination = {
  latitude: 47.667,
  longitude: -117.41
}

test("normalizeRouteResponse converts GeoJSON coordinates to leaflet order", () => {
  const route = normalizeRouteResponse({
    routes: [
      {
        distance: 2400,
        duration: 720,
        geometry: {
          coordinates: [
            [-117.426, 47.6588],
            [-117.41, 47.667]
          ]
        }
      }
    ]
  })

  assert.deepEqual(route.coordinates[0], [47.6588, -117.426])
  assert.equal(route.distanceMeters, 2400)
  assert.equal(route.durationSeconds, 720)
  assert.equal(route.source, "osrm")
})

test("buildFallbackRoute creates a direct route when OSRM is unavailable", () => {
  const route = buildFallbackRoute(origin, destination, "timeout")

  assert.equal(route.source, "fallback")
  assert.equal(route.warning, "timeout")
  assert.equal(route.coordinates.length, 2)
  assert.ok(route.distanceMeters > 0)
  assert.ok(route.durationSeconds >= 300)
})

test("distance and duration formatters return readable values", () => {
  assert.equal(formatDistance(2450), "2.5 km")
  assert.equal(formatDistance(240), "240 m")
  assert.equal(formatDuration(780), "13 min")
  assert.equal(formatDuration(5400), "1 hr 30 min")
})

test("hasRouteEndpoints rejects incomplete coordinates", () => {
  assert.equal(hasRouteEndpoints(origin, destination), true)
  assert.equal(hasRouteEndpoints(origin, { latitude: null, longitude: -117.4 }), false)
})

test("buildRouteUrl encodes origin and destination pairs in OSRM order", () => {
  const url = buildRouteUrl(origin, destination)

  assert.match(url, /-117\.426,47\.6588;-117\.41,47\.667/)
  assert.match(url, /geometries=geojson/)
})

test("normalizeRouteResponse throws when OSRM returns no usable routes", () => {
  assert.throws(
    () => normalizeRouteResponse({ routes: [] }),
    /OSRM did not return a usable route/
  )
})

test("formatDuration rounds short routes up to at least one minute", () => {
  assert.equal(formatDuration(5), "1 min")
})

test("calculateHaversineDistance matches the fallback distance basis", () => {
  const distance = calculateHaversineDistance(origin, destination)
  assert.ok(distance > 0)
  assert.ok(distance < 5000)
})

test("getRouteStrokeStyles returns a visible two-layer route style", () => {
  const strokeStyles = getRouteStrokeStyles("osrm")
  const fallbackStyles = getRouteStrokeStyles("fallback")

  assert.equal(strokeStyles.casing.weight > strokeStyles.line.weight, true)
  assert.equal(strokeStyles.line.color, "#ea580c")
  assert.equal(fallbackStyles.line.color, "#0f766e")
})

test("fetchRoute returns the normalized OSRM route when the service succeeds", async () => {
  const route = await fetchRoute(origin, destination, {
    fetchImpl: async (url) => {
      assert.match(url, /route\/v1\/driving/)

      return {
        ok: true,
        async json() {
          return {
            routes: [
              {
                distance: 1800,
                duration: 540,
                geometry: {
                  coordinates: [
                    [-117.426, 47.6588],
                    [-117.418, 47.662],
                    [-117.41, 47.667]
                  ]
                }
              }
            ]
          }
        }
      }
    }
  })

  assert.equal(route.source, "osrm")
  assert.equal(route.distanceMeters, 1800)
  assert.equal(route.durationSeconds, 540)
  assert.equal(route.coordinates.length, 3)
})

test("fetchRoute falls back when OSRM returns a bad status", async () => {
  const route = await fetchRoute(origin, destination, {
    fetchImpl: async () => ({
      ok: false,
      status: 500
    })
  })

  assert.equal(route.source, "fallback")
  assert.match(route.warning, /500/)
})

test("fetchRoute falls back when OSRM returns malformed route data", async () => {
  const route = await fetchRoute(origin, destination, {
    fetchImpl: async () => ({
      ok: true,
      async json() {
        return {
          routes: [
            {
              geometry: {
                coordinates: []
              }
            }
          ]
        }
      }
    })
  })

  assert.equal(route.source, "fallback")
  assert.match(route.warning, /usable route/i)
})

test("fetchRoute rejects when either endpoint is missing", async () => {
  await assert.rejects(
    fetchRoute(null, destination, {
      fetchImpl: async () => {
        throw new Error("fetch should not be called")
      }
    }),
    /requires both a robot location and a destination/i
  )
})
