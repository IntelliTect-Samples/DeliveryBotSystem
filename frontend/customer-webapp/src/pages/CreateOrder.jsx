import { useState } from "react"

const ORDER_SERVICE_API_BASE =
  import.meta.env.VITE_ORDER_SERVICE_API_BASE || "/api/order-service"

const initialOrder = {
  restaurantOrStore: "",
  deliveryAddress: "",
  customerName: "",
  phoneNumber: "",
  orderType: "Food Order",
  deliveryNotes: ""
}

export default function CreateOrder() {
  const [order, setOrder] = useState(initialOrder)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [message, setMessage] = useState("")
  const [error, setError] = useState("")

  async function handleSubmit(event) {
    event.preventDefault()
    setIsSubmitting(true)
    setMessage("")
    setError("")

    try {
      const response = await fetch(buildApiUrl(ORDER_SERVICE_API_BASE, "/orders"), {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          orderId: createOrderId(),
          restaurantOrStore: order.restaurantOrStore,
          deliveryAddress: order.deliveryAddress,
          customerName: order.customerName,
          phoneNumber: order.phoneNumber,
          orderType: order.orderType,
          deliveryNotes: order.deliveryNotes,
          createdAtUtc: new Date().toISOString()
        })
      })

      if (!response.ok) {
        throw new Error("The order service did not accept the order.")
      }

      setOrder(initialOrder)
      setMessage("Order submitted successfully.")
    } catch (err) {
      setError(err.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  function updateOrder(field, value) {
    setOrder((currentOrder) => ({
      ...currentOrder,
      [field]: value
    }))
  }

  return (
    <div style={styles.page}>
      <div style={styles.orderCard}>
        <h1>Create Delivery Order</h1>

        <p style={styles.subtitle}>
          Schedule a food or beverage delivery.
        </p>

        <form style={styles.form} onSubmit={handleSubmit}>
          <input
            type="text"
            placeholder="Restaurant or Store"
            style={styles.input}
            value={order.restaurantOrStore}
            onChange={(event) => updateOrder("restaurantOrStore", event.target.value)}
            required
          />

          <input
            type="text"
            placeholder="Delivery Address"
            style={styles.input}
            value={order.deliveryAddress}
            onChange={(event) => updateOrder("deliveryAddress", event.target.value)}
            required
          />

          <input
            type="text"
            placeholder="Customer Name"
            style={styles.input}
            value={order.customerName}
            onChange={(event) => updateOrder("customerName", event.target.value)}
            required
          />

          <input
            type="tel"
            placeholder="Phone Number"
            style={styles.input}
            value={order.phoneNumber}
            onChange={(event) => updateOrder("phoneNumber", event.target.value)}
            required
          />

          <select
            style={styles.input}
            value={order.orderType}
            onChange={(event) => updateOrder("orderType", event.target.value)}
          >
            <option>Food Order</option>
            <option>Beverage Order</option>
            <option>Small Package</option>
          </select>

          <textarea
            placeholder="Delivery Notes"
            style={styles.textarea}
            value={order.deliveryNotes}
            onChange={(event) => updateOrder("deliveryNotes", event.target.value)}
          />

          <button style={styles.button} disabled={isSubmitting}>
            {isSubmitting ? "Submitting..." : "Submit Order"}
          </button>
        </form>

        {message && <p style={styles.successMessage}>{message}</p>}
        {error && <p style={styles.errorMessage}>{error}</p>}
      </div>
    </div>
  )
}

function buildApiUrl(baseUrl, path) {
  return `${baseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`
}

function createOrderId() {
  if (window.crypto?.randomUUID) {
    return window.crypto.randomUUID()
  }

  return `order-${Date.now()}`
}

const styles = {
  page: {
    minHeight: "100vh",
    backgroundColor: "#111827",
    display: "flex",
    justifyContent: "center",
    alignItems: "center",
    padding: "2rem",
    fontFamily: "Arial"
  },

  orderCard: {
    backgroundColor: "#1e293b",
    padding: "2.5rem",
    borderRadius: "20px",
    width: "100%",
    maxWidth: "550px",
    color: "white",
    border: "1px solid #334155"
  },

  subtitle: {
    color: "#cbd5e1",
    marginBottom: "2rem"
  },

  form: {
    display: "flex",
    flexDirection: "column",
    gap: "1rem"
  },

  input: {
    padding: "1rem",
    borderRadius: "10px",
    border: "none",
    fontSize: "1rem"
  },

  textarea: {
    padding: "1rem",
    borderRadius: "10px",
    border: "none",
    minHeight: "100px",
    fontSize: "1rem"
  },

  button: {
    backgroundColor: "#2563eb",
    color: "white",
    border: "none",
    padding: "1rem",
    borderRadius: "10px",
    fontSize: "1rem",
    fontWeight: "bold",
    cursor: "pointer"
  },

  successMessage: {
    color: "#bbf7d0",
    backgroundColor: "#14532d",
    border: "1px solid #22c55e",
    borderRadius: "8px",
    marginTop: "1rem",
    padding: "0.85rem 1rem"
  },

  errorMessage: {
    color: "#fecaca",
    backgroundColor: "#7f1d1d",
    border: "1px solid #ef4444",
    borderRadius: "8px",
    marginTop: "1rem",
    padding: "0.85rem 1rem"
  }
}
