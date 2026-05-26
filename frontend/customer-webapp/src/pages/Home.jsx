import { Link } from "react-router-dom"
import { useEffect, useMemo, useState } from "react"

const SIMULATOR_API_BASE =
  import.meta.env.VITE_SIMULATOR_API_BASE || "/api/simulator"

const demoBots = [
  {
    botId: "bot-001",
    model: "DeliveryBot-V1",
    status: "Available",
    currentLocation: {
      latitude: 33.4255,
      longitude: -111.94
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
      latitude: 33.4261,
      longitude: -111.9394
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
      latitude: 33.4248,
      longitude: -111.9408
    },
    powerLevel: 24.6,
    externalTemperature: 72,
    internalStorageTemperature: 38,
    stock: [],
    activeOrderId: null,
    queuedOrderCount: 0
  }
]

export default function Home() {
  const [bots, setBots] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState("")
  const [lastUpdated, setLastUpdated] = useState(null)

  useEffect(() => {
    let isMounted = true

    async function loadBots() {
      try {
        const response = await fetch(`${SIMULATOR_API_BASE}/bots`)

        if (!response.ok) {
          throw new Error("The simulator did not return bot data.")
        }

        const data = await response.json()

        if (isMounted) {
          setBots(Array.isArray(data) ? data : [])
          setError("")
          setLastUpdated(new Date())
        }
      } catch (err) {
        if (isMounted) {
          setError(err.message)
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadBots()
    const refreshTimer = window.setInterval(loadBots, 5000)

    return () => {
      isMounted = false
      window.clearInterval(refreshTimer)
    }
  }, [])

  const displayedBots = bots.length > 0 ? bots : demoBots
  const fleetStats = useMemo(() => getFleetStats(displayedBots), [displayedBots])
  const isDemoData = bots.length === 0

  return (
    <div style={styles.page}>
      <div style={styles.heroCard}>
        <h1 style={styles.title}>RoboEats Delivery</h1>

        <p style={styles.subtitle}>
          Fast autonomous food and beverage delivery throughout Spokane.
        </p>

        <Link to="/orders" style={styles.button}>
          Order Now
        </Link>
      </div>

      <div style={styles.cards}>
        <InfoCard
          title="Fast Delivery"
          text="Autonomous robots deliver food quickly and efficiently."
        />

        <InfoCard
          title="Live Tracking"
          text="Track your delivery robot in real time."
        />

        <InfoCard
          title="Local Service"
          text="Serving restaurants, cafes, and beverage shops across Spokane."
        />
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

        <div style={styles.botGrid}>
          {displayedBots.map((bot) => (
            <BotCard key={bot.botId} bot={bot} isDemoData={isDemoData} />
          ))}
        </div>
      </section>
    </div>
  )
}

function InfoCard({ title, text }) {
  return (
    <div style={styles.card}>
      <h2>{title}</h2>
      <p>{text}</p>
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

function BotCard({ bot, isDemoData }) {
  const statusColor = getStatusColor(bot.status)
  const location = bot.currentLocation
  const stockSummary = getStockSummary(bot.stock)

  return (
    <article style={styles.botCard}>
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
    fontFamily: "Arial",
    display: "flex",
    flexDirection: "column",
    alignItems: "center"
  },

  heroCard: {
    backgroundColor: "#1f2937",
    padding: "3rem",
    borderRadius: "8px",
    textAlign: "center",
    maxWidth: "700px",
    width: "100%",
    marginTop: "3rem",
    border: "1px solid #334155"
  },

  title: {
    fontSize: "clamp(2.5rem, 7vw, 4rem)",
    marginBottom: "1rem"
  },

  subtitle: {
    color: "#cbd5e1",
    fontSize: "1.1rem",
    marginBottom: "2rem"
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

  cards: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
    gap: "1.5rem",
    width: "100%",
    maxWidth: "1100px",
    marginTop: "3rem"
  },

  card: {
    backgroundColor: "#1f2937",
    padding: "2rem",
    borderRadius: "8px",
    border: "1px solid #334155"
  },

  fleetSection: {
    width: "100%",
    maxWidth: "1100px",
    marginTop: "3rem",
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
    marginBottom: "1rem"
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

  demoLabel: {
    position: "absolute",
    right: "1rem",
    bottom: "1rem",
    color: "#64748b",
    fontSize: "0.8rem",
    fontWeight: "bold"
  }
}
