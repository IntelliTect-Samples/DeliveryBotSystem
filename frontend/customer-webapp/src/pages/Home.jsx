import { Link } from "react-router-dom"

export default function Home() {
  return (
    <div style={styles.page}>
      <div style={styles.heroCard}>
        <h1 style={styles.title}>
          RoboEats Delivery
        </h1>

        <p style={styles.subtitle}>
          Fast autonomous food and beverage delivery
          throughout Spokane.
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
          text="Serving restaurants, cafés, and beverage shops across Spokane."
        />
      </div>
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

const styles = {
  page: {
    minHeight: "100vh",
    backgroundColor: "#111827",
    color: "white",
    padding: "2rem",
    fontFamily: "Arial",
    display: "flex",
    flexDirection: "column",
    alignItems: "center"
  },

  heroCard: {
    backgroundColor: "#1e293b",
    padding: "3rem",
    borderRadius: "20px",
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
    borderRadius: "10px",
    fontWeight: "bold"
  },

  cards: {
    display: "grid",
    gridTemplateColumns:
      "repeat(auto-fit, minmax(250px, 1fr))",
    gap: "1.5rem",
    width: "100%",
    maxWidth: "1100px",
    marginTop: "3rem"
  },

  card: {
    backgroundColor: "#1e293b",
    padding: "2rem",
    borderRadius: "16px",
    border: "1px solid #334155"
  }
}