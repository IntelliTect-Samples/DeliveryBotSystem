import { useEffect, useState } from "react"
import {
  BrowserRouter,
  Link,
  Route,
  Routes
} from "react-router-dom"
import AgentAssistant from "./components/AgentAssistant.jsx"
import { readLatestOrder, subscribeToLatestOrder, writeLatestOrder } from "./lib/orderSession.js"
import Home from "./pages/Home.jsx"
import CreateOrder from "./pages/CreateOrder.jsx"

function App() {
  const [latestOrder, setLatestOrder] = useState(() => readLatestOrder())
  const [latestRoute, setLatestRoute] = useState(null)

  useEffect(() => subscribeToLatestOrder(setLatestOrder), [])

  function handleOrderCreated(order) {
    writeLatestOrder(order)
    setLatestOrder(order)
  }

  return (
    <BrowserRouter>
      <div style={styles.page}>
        <nav style={styles.nav}>
          <h2>Robo Delivery Service</h2>

          <div style={styles.links}>
            <Link to="/" style={styles.link}>
              Home
            </Link>

            <Link to="/orders" style={styles.link}>
              Orders
            </Link>
          </div>
        </nav>

        <Routes>
          <Route
            path="/"
            element={<Home latestOrder={latestOrder} onRouteChange={setLatestRoute} />}
          />
          <Route
            path="/orders"
            element={<CreateOrder onOrderCreated={handleOrderCreated} />}
          />
        </Routes>

        <AgentAssistant latestOrder={latestOrder} route={latestRoute} />
      </div>
    </BrowserRouter>
  )
}

const styles = {
  page: {
    backgroundColor: "#111827",
    minHeight: "100vh"
  },
  nav: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "1.5rem 2rem",
    backgroundColor: "#0f172a",
    color: "white",
    gap: "1rem",
    flexWrap: "wrap"
  },
  links: {
    display: "flex",
    gap: "1rem"
  },
  link: {
    color: "white",
    textDecoration: "none"
  }
}

export default App
