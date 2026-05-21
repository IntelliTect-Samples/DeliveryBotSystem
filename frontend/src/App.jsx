import {
  BrowserRouter,
  Routes,
  Route,
  Link
} from "react-router-dom"

import Home from "./pages/Home"
import CreateOrder from "./pages/CreateOrder"

function App() {
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
          <Route path="/" element={<Home />} />

          <Route
            path="/orders"
            element={<CreateOrder />}
          />
        </Routes>
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
    color: "white"
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