export default function CreateOrder() {
  return (
    <div style={styles.page}>
      <div style={styles.orderCard}>
        <h1>Create Delivery Order</h1>

        <p style={styles.subtitle}>
          Schedule a food or beverage delivery.
        </p>

        <form style={styles.form}>
          <input
            type="text"
            placeholder="Restaurant or Store"
            style={styles.input}
          />

          <input
            type="text"
            placeholder="Delivery Address"
            style={styles.input}
          />

          <input
            type="text"
            placeholder="Customer Name"
            style={styles.input}
          />

          <input
            type="text"
            placeholder="Phone Number"
            style={styles.input}
          />

          <select style={styles.input}>
            <option>Food Order</option>
            <option>Beverage Order</option>
            <option>Small Package</option>
          </select>

          <textarea
            placeholder="Delivery Notes"
            style={styles.textarea}
          />

          <button style={styles.button}>
            Submit Order
          </button>
        </form>
      </div>
    </div>
  )
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
  }
}