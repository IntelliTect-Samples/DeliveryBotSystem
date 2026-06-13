import { Link } from "react-router-dom"
import { useEffect, useMemo, useRef, useState } from "react"
import L from "leaflet"
import "leaflet/dist/leaflet.css"
import { appConfig } from "../lib/config.js"
import { formatOrderStatus, summarizeItems } from "../lib/orders.js"
import { fetchRoute, formatDistance, formatDuration } from "../lib/osrm.js"

const SPOKANE_CENTER = [47.6588, -117.426]

const demoBots = [
  {
    botId: "bot-001",
    model: "DeliveryBot-V1",
    status: "Available",
    currentLocation: {
      latitude: 47.6588,
      longitude: -117.426
    },
    powerLevel: 99.9,
    externalTemperature: 72,
    internalStorageTemperature: 38,
    stock: [
      {
        itemId: "water",
        itemName: "Water",
        quantityAvailable: 19
      }
    ],
    activeOrderId: null,
    queuedOrderCount: 0
  },
  {
    botId: "bot-002",
    model: "DeliveryBot-V1",
    status: "OnDelivery",
    currentLocation: {
      latitude: 47.6572,
      longitude: -117.4236
    },
    powerLevel: 86.4,
    externalTemperature: 71,
    internalStorageTemperature: 39,
    stock: [],
    activeOrderId: "order-104",
    queuedOrderCount: 1
  },
  {
    botId: "bot-003",
    model: "DeliveryBot-V1",
    status: "Charging",
    currentLocation: {
      latitude: 47.6605,
      longitude: -117.4145
    },
    powerLevel: 24.6,
    externalTemperature: 72,
    internalStorageTemperature: 38,
    stock: [],
    activeOrderId: null,
    queuedOrderCount: 0
  }
]

export default function Home({ latestOrder, onRouteChange }) {
  const [bots, setBots] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState("")
  const [lastUpdated, setLastUpdated] = useState(null)
  const [route, setRoute] = useState(null)
  const [routeState, setRouteState] = useState({
    isLoading: false,
    message: "Place an order to calculate a route."
  })

  useEffect(() => {
    let isMounted = true
    let refreshTimer = null

    async function loadBots() {
      try {
        const response = await fetch(`${appConfig.simulatorApiBase}/bots`)

        if (!response.ok) {
          throw new Error("The simulator did not return bot data.")
        }

        const data = await response.json()

        if (isMounted) {
          setBots(Array.isArray(data) ? data : [])
          setError("")
          setLastUpdated(new Date())
          refreshTimer = window.setTimeout(loadBots, 5000)
        }
      } catch (loadError) {
        if (isMounted) {
          setError(loadError.message)
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadBots()

    return () => {
      isMounted = false
      if (refreshTimer) {
        window.clearTimeout(refreshTimer)
      }
    }
  }, [])

  const displayedBots = bots.length > 0 ? bots : demoBots
  const fleetStats = useMemo(() => getFleetStats(displayedBots), [displayedBots])
  const isDemoData = bots.length === 0
  const activeBot = useMemo(
    () => displayedBots.find((bot) => bot.botId === latestOrder?.assignedBotId) || null,
    [displayedBots, latestOrder]
  )

  useEffect(() => {
    let isMounted = true

    async function loadRoute() {
      if (!latestOrder?.destination) {
        setRoute(null)
        setRouteState({
          isLoading: false,
          message: latestOrder?.deliveryAddress
            ? "This local preview can only map supported demo destinations."
            : "Place an order to calculate a route."
        })
        onRouteChange?.(null)
        return
      }

      if (!activeBot?.currentLocation) {
        setRoute(null)
        setRouteState({
          isLoading: false,
          message: latestOrder.assignedBotId
            ? "Waiting for the assigned bot location."
            : "The order is waiting for a bot assignment."
        })
        onRouteChange?.(null)
        return
      }

      setRouteState({
        isLoading: true,
        message: "Calculating route."
      })

      const nextRoute = await fetchRoute(activeBot.currentLocation, latestOrder.destination)

      if (!isMounted) {
        return
      }

      setRoute(nextRoute)
      onRouteChange?.(nextRoute)
      setRouteState({
        isLoading: false,
        message: nextRoute.warning
          ? `Showing fallback route because ${nextRoute.warning}`
          : "Showing OSRM route."
      })
    }

    loadRoute()

    return () => {
      isMounted = false
    }
  }, [activeBot, latestOrder, onRouteChange])

  return (
    <div style={styles.page}>
      <div style={styles.heroCard}>
        <h1 style={styles.title}>Track the next Spokane delivery.</h1>

        <p style={styles.subtitle}>
          View the fleet, draw the OSRM route, and ask the delivery assistant
          about the latest order.
        </p>

        <div style={styles.heroActions}>
          <Link to="/orders" style={styles.button}>
            Order Now
          </Link>

          <span style={styles.heroNote}>
            {latestOrder ? `Latest order ${latestOrder.id.slice(0, 8)}` : "No active customer order yet"}
          </span>
        </div>
      </div>

      <section style={styles.fleetSection}>
        <div style={styles.sectionHeader}>
          <div>
            <p style={styles.kicker}>Simulator connection</p>
            <h2 style={styles.sectionTitle}>Live Robot Fleet</h2>
          </div>

          <div style={styles.syncStatus}>
            {isLoading
              ? "Loading simulator data"
              : error
                ? "Showing demo data"
                : `Updated ${lastUpdated?.toLocaleTimeString([], {
                    hour: "numeric",
                    minute: "2-digit",
                    second: "2-digit"
                  })}`}
          </div>
        </div>

        {error && (
          <p style={styles.notice}>
            Start the RobotSimulator API on port 5099 to replace these demo
            cards with live simulated robot data.
          </p>
        )}

        <div style={styles.statGrid}>
          <Metric label="Robots online" value={fleetStats.total} />
          <Metric label="Available" value={fleetStats.available} />
          <Metric label="On delivery" value={fleetStats.onDelivery} />
          <Metric label="Average battery" value={`${fleetStats.averageBattery}%`} />
        </div>

        <div style={styles.routeGrid}>
          <article style={styles.routeCard}>
            <h3 style={styles.routeTitle}>Latest Order</h3>
            {latestOrder ? (
              <>
                <RouteMetric label="Status" value={formatOrderStatus(latestOrder.status)} />
                <RouteMetric label="Assigned Bot" value={latestOrder.assignedBotId || "Pending"} />
                <RouteMetric label="Destination" value={latestOrder.deliveryAddress} />
                <RouteMetric label="Items" value={summarizeItems(latestOrder.items)} />
              </>
            ) : (
              <p style={styles.emptyText}>Create an order to populate the route and assistant.</p>
            )}
          </article>

          <article style={styles.routeCard}>
            <h3 style={styles.routeTitle}>OSRM Route</h3>
            {route ? (
              <>
                <RouteMetric label="Distance" value={formatDistance(route.distanceMeters)} />
                <RouteMetric label="ETA" value={formatDuration(route.durationSeconds)} />
                <RouteMetric label="Source" value={route.source === "osrm" ? "OSRM" : "Fallback"} />
                <p style={styles.routeMessage}>{routeState.message}</p>
              </>
            ) : (
              <p style={styles.emptyText}>{routeState.message}</p>
            )}
          </article>
        </div>

        <FleetMap
          bots={displayedBots}
          isDemoData={isDemoData}
          route={route}
          destination={latestOrder?.destination}
        />

        <div style={styles.botGrid}>
          {displayedBots.map((bot) => (
            <BotCard
              key={bot.botId}
              bot={bot}
              isDemoData={isDemoData}
              isAssigned={!isDemoData && bot.botId === latestOrder?.assignedBotId}
            />
          ))}
        </div>
      </section>
    </div>
  )
}

function Metric({ label, value }) {
  return (
    <div style={styles.metric}>
      <span style={styles.metricLabel}>{label}</span>
      <strong style={styles.metricValue}>{value}</strong>
    </div>
  )
}

function RouteMetric({ label, value }) {
  return (
    <div style={styles.routeMetric}>
      <span style={styles.routeMetricLabel}>{label}</span>
      <strong style={styles.routeMetricValue}>{value}</strong>
    </div>
  )
}

function FleetMap({ bots, isDemoData, route, destination }) {
  const mapElementRef = useRef(null)
  const mapRef = useRef(null)
  const markerLayerRef = useRef(null)
  const routeLayerRef = useRef(null)
  const locatedBots = useMemo(
    () =>
      bots.filter(
        (bot) =>
          Number.isFinite(bot.currentLocation?.latitude) &&
          Number.isFinite(bot.currentLocation?.longitude)
      ),
    [bots]
  )

  useEffect(() => {
    if (!mapElementRef.current || mapRef.current) {
      return undefined
    }

    const map = L.map(mapElementRef.current, {
      center: SPOKANE_CENTER,
      zoom: 15,
      minZoom: 12,
      maxZoom: 19,
      scrollWheelZoom: true
    })

    L.tileLayer(appConfig.mapTileUrl, {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      detectRetina: true,
      keepBuffer: 3,
      updateWhenIdle: true
    }).addTo(map)

    mapRef.current = map
    markerLayerRef.current = L.layerGroup().addTo(map)
    routeLayerRef.current = L.layerGroup().addTo(map)

    const stabilizeMap = () => {
      window.requestAnimationFrame(() => map.invalidateSize())
    }

    window.setTimeout(stabilizeMap, 0)
    map.on("zoomend", stabilizeMap)
    map.on("moveend", stabilizeMap)

    return () => {
      map.off("zoomend", stabilizeMap)
      map.off("moveend", stabilizeMap)
      map.remove()
      mapRef.current = null
      markerLayerRef.current = null
      routeLayerRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    const markerLayer = markerLayerRef.current
    const routeLayer = routeLayerRef.current

    if (!map || !markerLayer || !routeLayer) {
      return
    }

    markerLayer.clearLayers()
    routeLayer.clearLayers()

    if (locatedBots.length === 0) {
      map.setView(SPOKANE_CENTER, 15)
      return
    }

    locatedBots.forEach((bot) => {
      const statusColor = getStatusColor(bot.status)
      L.circleMarker(
        [bot.currentLocation.latitude, bot.currentLocation.longitude],
        {
          radius: 10,
          color: statusColor.background,
          fillColor: statusColor.text,
          fillOpacity: 1,
          opacity: 1,
          weight: 4
        }
      )
        .bindTooltip(`${bot.botId} - ${formatStatus(bot.status || "Unknown")}`, {
          direction: "top",
          offset: [0, -10],
          opacity: 0.95
        })
        .addTo(markerLayer)
    })

    if (destination) {
      L.circleMarker([destination.latitude, destination.longitude], {
        radius: 8,
        color: "#7c2d12",
        fillColor: "#fb923c",
        fillOpacity: 1,
        opacity: 1,
        weight: 3
      })
        .bindTooltip("Delivery destination", {
          direction: "top",
          offset: [0, -10],
          opacity: 0.95
        })
        .addTo(markerLayer)
    }

    if (route?.coordinates?.length > 1) {
      L.polyline(route.coordinates, {
        color: "#fff7ed",
        weight: 10,
        opacity: 0.95
      }).addTo(routeLayer)

      L.polyline(route.coordinates, {
        color: route.source === "osrm" ? "#ea580c" : "#0f766e",
        weight: 6,
        opacity: 1
      }).addTo(routeLayer)
    }

    const boundsPoints = [
      ...locatedBots.map((bot) => [
        bot.currentLocation.latitude,
        bot.currentLocation.longitude
      ])
    ]

    if (route?.coordinates?.length > 0) {
      boundsPoints.push(...route.coordinates)
    }

    const bounds = L.latLngBounds(boundsPoints)
    map.fitBounds(bounds.pad(0.35), {
      maxZoom: 16
    })
  }, [destination, locatedBots, route])

  return (
    <section style={styles.mapPanel} aria-label="Robot location map">
      <div style={styles.mapHeader}>
        <div>
          <p style={styles.kicker}>Fleet map</p>
          <h3 style={styles.mapTitle}>Robot Locations and Route</h3>
        </div>

        <span style={styles.mapCount}>
          {locatedBots.length} mapped robot{locatedBots.length === 1 ? "" : "s"}
        </span>
      </div>

      <div style={styles.mapShell}>
        <div
          ref={mapElementRef}
          style={styles.mapCanvas}
          aria-label="Interactive Spokane robot map"
        />

        <div style={styles.mapLegend}>
          {["Available", "OnDelivery", "Charging"].map((status) => {
            const statusColor = getStatusColor(status)

            return (
              <div key={status} style={styles.legendItem}>
                <span
                  style={{
                    ...styles.legendDot,
                    backgroundColor: statusColor.text
                  }}
                />
                <span>{formatStatus(status)}</span>
              </div>
            )
          })}

          <div style={styles.legendItem}>
            <span style={{ ...styles.legendDot, backgroundColor: "#ea580c" }} />
            <span>OSRM route</span>
          </div>
        </div>
      </div>

      {isDemoData && (
        <p style={styles.mapFootnote}>
          Demo coordinates are shown until simulator data is available.
        </p>
      )}
    </section>
  )
}

function BotCard({ bot, isDemoData, isAssigned }) {
  const statusColor = getStatusColor(bot.status)
  const location = bot.currentLocation
  const stockSummary = getStockSummary(bot.stock)

  return (
    <article
      style={{
        ...styles.botCard,
        ...(isAssigned ? styles.assignedBotCard : null)
      }}
    >
      <div style={styles.botHeader}>
        <div>
          <p style={styles.botId}>{bot.botId}</p>
          <p style={styles.botModel}>{bot.model || "DeliveryBot"}</p>
        </div>

        <span
          style={{
            ...styles.statusBadge,
            backgroundColor: statusColor.background,
            color: statusColor.text
          }}
        >
          {bot.status || "Unknown"}
        </span>
      </div>

      <div style={styles.batteryRow}>
        <span>Battery</span>
        <strong>{formatPercent(bot.powerLevel)}</strong>
      </div>
      <div style={styles.batteryTrack}>
        <div
          style={{
            ...styles.batteryFill,
            width: `${clamp(Number(bot.powerLevel) || 0, 0, 100)}%`
          }}
        />
      </div>

      <div style={styles.botDetails}>
        <Detail label="Active order" value={bot.activeOrderId || "None"} />
        <Detail label="Queued orders" value={bot.queuedOrderCount ?? 0} />
        <Detail
          label="Storage temp"
          value={`${Math.round(bot.internalStorageTemperature ?? 0)} F`}
        />
        <Detail label="Stock" value={stockSummary} />
      </div>

      {location && (
        <p style={styles.location}>
          {location.latitude.toFixed(4)}, {location.longitude.toFixed(4)}
        </p>
      )}

      {isAssigned && <p style={styles.assignedLabel}>Assigned to latest order</p>}
      {isDemoData && <p style={styles.demoLabel}>Demo preview</p>}
    </article>
  )
}

function Detail({ label, value }) {
  return (
    <div style={styles.detail}>
      <span style={styles.detailLabel}>{label}</span>
      <strong style={styles.detailValue}>{value}</strong>
    </div>
  )
}

function getFleetStats(botList) {
  const total = botList.length
  const available = botList.filter((bot) => bot.status === "Available").length
  const onDelivery = botList.filter((bot) => bot.status === "OnDelivery").length
  const averageBattery =
    total === 0
      ? 0
      : Math.round(
          botList.reduce((sum, bot) => sum + (Number(bot.powerLevel) || 0), 0) /
            total
        )

  return {
    total,
    available,
    onDelivery,
    averageBattery
  }
}

function getStockSummary(stock = []) {
  const availableItems = stock.filter((item) => item.quantityAvailable > 0)

  if (availableItems.length === 0) {
    return "No items"
  }

  return availableItems
    .slice(0, 2)
    .map((item) => `${item.itemName || item.itemId}: ${item.quantityAvailable}`)
    .join(", ")
}

function formatStatus(status) {
  if (status === "OnDelivery") {
    return "On delivery"
  }

  return status
}

function getStatusColor(status) {
  if (status === "Available") {
    return {
      background: "#dcfce7",
      text: "#166534"
    }
  }

  if (status === "OnDelivery") {
    return {
      background: "#dbeafe",
      text: "#1d4ed8"
    }
  }

  return {
    background: "#fef3c7",
    text: "#92400e"
  }
}

function formatPercent(value) {
  return `${Math.round(Number(value) || 0)}%`
}

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max)
}

const styles = {
  page: {
    minHeight: "100vh",
    backgroundColor: "#0f172a",
    color: "#f8fafc",
    padding: "2rem",
    display: "flex",
    flexDirection: "column",
    alignItems: "center"
  },
  heroCard: {
    backgroundColor: "#1f2937",
    padding: "3rem",
    borderRadius: "8px",
    textAlign: "left",
    maxWidth: "1100px",
    width: "100%",
    marginTop: "1rem",
    border: "1px solid #334155",
    boxSizing: "border-box"
  },
  title: {
    fontSize: "clamp(2.5rem, 6vw, 4rem)",
    marginBottom: "1rem",
    color: "#f8fafc",
    lineHeight: 1.1,
    maxWidth: "20ch"
  },
  subtitle: {
    color: "#cbd5e1",
    fontSize: "1.1rem",
    marginBottom: "1.5rem",
    maxWidth: "44rem"
  },
  heroActions: {
    display: "flex",
    flexWrap: "wrap",
    gap: "1rem",
    alignItems: "center"
  },
  button: {
    display: "inline-block",
    backgroundColor: "#2563eb",
    color: "white",
    textDecoration: "none",
    padding: "1rem 2rem",
    borderRadius: "8px",
    fontWeight: "bold"
  },
  heroNote: {
    color: "#cbd5e1",
    backgroundColor: "#111827",
    border: "1px solid #334155",
    borderRadius: "8px",
    padding: "0.65rem 0.85rem",
    fontSize: "0.9rem"
  },
  fleetSection: {
    width: "100%",
    maxWidth: "1100px",
    marginTop: "1.7rem",
    paddingBottom: "2rem"
  },
  sectionHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "flex-end",
    gap: "1rem",
    marginBottom: "1rem",
    textAlign: "left",
    flexWrap: "wrap"
  },
  kicker: {
    color: "#38bdf8",
    fontSize: "0.85rem",
    fontWeight: "bold",
    letterSpacing: "0.08em",
    marginBottom: "0.35rem",
    textTransform: "uppercase"
  },
  sectionTitle: {
    color: "#f8fafc",
    fontSize: "1.75rem",
    margin: 0
  },
  syncStatus: {
    color: "#cbd5e1",
    backgroundColor: "#111827",
    border: "1px solid #334155",
    borderRadius: "8px",
    padding: "0.65rem 0.85rem",
    fontSize: "0.9rem"
  },
  notice: {
    color: "#fde68a",
    backgroundColor: "#422006",
    border: "1px solid #854d0e",
    borderRadius: "8px",
    padding: "0.85rem 1rem",
    textAlign: "left",
    marginBottom: "1rem"
  },
  statGrid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))",
    gap: "1rem",
    marginBottom: "1rem"
  },
  metric: {
    backgroundColor: "#111827",
    border: "1px solid #334155",
    borderRadius: "8px",
    padding: "1rem",
    textAlign: "left"
  },
  metricLabel: {
    color: "#94a3b8",
    display: "block",
    fontSize: "0.85rem"
  },
  metricValue: {
    color: "#f8fafc",
    display: "block",
    fontSize: "1.7rem",
    lineHeight: 1.2,
    marginTop: "0.3rem"
  },
  routeGrid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))",
    gap: "1rem",
    marginBottom: "1rem"
  },
  routeCard: {
    backgroundColor: "#1f2937",
    border: "1px solid #334155",
    borderRadius: "8px",
    padding: "1.25rem",
    textAlign: "left"
  },
  routeTitle: {
    margin: "0 0 1rem",
    fontSize: "1.2rem"
  },
  routeMetric: {
    display: "grid",
    gap: "0.15rem",
    marginBottom: "0.85rem"
  },
  routeMetricLabel: {
    color: "#94a3b8",
    fontSize: "0.82rem",
    textTransform: "uppercase",
    letterSpacing: "0.08em"
  },
  routeMetricValue: {
    color: "#f8fafc",
    fontSize: "1rem"
  },
  emptyText: {
    color: "#cbd5e1",
    lineHeight: 1.5
  },
  routeMessage: {
    marginTop: "0.9rem",
    color: "#cbd5e1"
  },
  mapPanel: {
    backgroundColor: "#f8fafc",
    border: "1px solid #cbd5e1",
    borderRadius: "8px",
    color: "#0f172a",
    marginBottom: "1rem",
    padding: "1rem",
    textAlign: "left"
  },
  mapHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "flex-start",
    gap: "1rem",
    marginBottom: "1rem",
    flexWrap: "wrap"
  },
  mapTitle: {
    color: "#0f172a",
    fontSize: "1.35rem",
    lineHeight: 1.2,
    margin: 0
  },
  mapCount: {
    color: "#475569",
    backgroundColor: "#e2e8f0",
    border: "1px solid #cbd5e1",
    borderRadius: "999px",
    padding: "0.4rem 0.7rem",
    fontSize: "0.82rem",
    fontWeight: "bold"
  },
  mapShell: {
    display: "flex",
    flexDirection: "column",
    gap: "1rem",
    alignItems: "stretch"
  },
  mapCanvas: {
    height: "430px",
    minHeight: "360px",
    overflow: "hidden",
    borderRadius: "8px",
    border: "1px solid #bfdbfe",
    backgroundColor: "#e2e8f0"
  },
  mapLegend: {
    display: "flex",
    flexWrap: "wrap",
    gap: "0.7rem",
    backgroundColor: "#f1f5f9",
    border: "1px solid #cbd5e1",
    borderRadius: "8px",
    padding: "1rem"
  },
  legendItem: {
    display: "flex",
    alignItems: "center",
    gap: "0.55rem",
    color: "#334155",
    fontSize: "0.9rem",
    fontWeight: "bold"
  },
  legendDot: {
    width: "0.8rem",
    height: "0.8rem",
    borderRadius: "999px",
    flex: "0 0 auto"
  },
  mapFootnote: {
    color: "#64748b",
    fontSize: "0.85rem",
    marginTop: "0.85rem"
  },
  botGrid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))",
    gap: "1rem"
  },
  botCard: {
    backgroundColor: "#f8fafc",
    border: "1px solid #cbd5e1",
    borderRadius: "8px",
    color: "#0f172a",
    padding: "1rem",
    textAlign: "left",
    minHeight: "295px",
    boxSizing: "border-box",
    position: "relative"
  },
  assignedBotCard: {
    borderColor: "#fb923c"
  },
  botHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "flex-start",
    gap: "0.75rem",
    marginBottom: "1rem"
  },
  botId: {
    fontSize: "1.15rem",
    fontWeight: "bold",
    marginBottom: "0.1rem"
  },
  botModel: {
    color: "#64748b",
    fontSize: "0.9rem"
  },
  statusBadge: {
    borderRadius: "999px",
    fontSize: "0.8rem",
    fontWeight: "bold",
    padding: "0.35rem 0.65rem",
    whiteSpace: "nowrap"
  },
  batteryRow: {
    display: "flex",
    justifyContent: "space-between",
    color: "#334155",
    marginBottom: "0.45rem"
  },
  batteryTrack: {
    height: "10px",
    backgroundColor: "#e2e8f0",
    borderRadius: "999px",
    overflow: "hidden",
    marginBottom: "1rem"
  },
  batteryFill: {
    height: "100%",
    backgroundColor: "#22c55e",
    borderRadius: "999px"
  },
  botDetails: {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: "0.85rem",
    marginBottom: "1.5rem"
  },
  detail: {
    minWidth: 0
  },
  detailLabel: {
    color: "#64748b",
    display: "block",
    fontSize: "0.78rem",
    marginBottom: "0.15rem"
  },
  detailValue: {
    color: "#0f172a",
    display: "block",
    fontSize: "0.92rem",
    overflowWrap: "anywhere"
  },
  location: {
    color: "#475569",
    fontFamily: "Consolas, monospace",
    fontSize: "0.85rem"
  },
  assignedLabel: {
    display: "inline-block",
    marginTop: "0.4rem",
    color: "#ea580c",
    fontSize: "0.8rem",
    fontWeight: "bold"
  },
  demoLabel: {
    position: "absolute",
    right: "1rem",
    bottom: "1rem",
    color: "#64748b",
    fontSize: "0.8rem",
    fontWeight: "bold"
  }
}
